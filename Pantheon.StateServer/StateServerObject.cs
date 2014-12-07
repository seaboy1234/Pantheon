using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;

namespace Pantheon.StateServer
{
    public class StateServerObject : IDisposable
    {
        private ulong _aiChannel;
        private ulong _channel;
        private List<StateServerObject> _children;
        private DistributedObjectDefinition _def;
        private ulong _clientChannel;
        private uint _doid;
        private IMessageDirector _messageDirector;
        private MessageRouter _messageRouter;
        private ulong _owner;
        private StateServerObject _parent;
        private Dictionary<PropertyInfo, object> _ram;
        private StateServerManager _stateServer;
        private Type _type;
        private ushort _zone;

        public ulong AIChannel
        {
            get { return _aiChannel; }
        }

        public uint DoId
        {
            get { return _doid; }
        }

        public ulong ObjectChannel
        {
            get { return _channel; }
        }

        public ulong Owner
        {
            get { return _owner; }
        }

        public Type Type
        {
            get { return _type; }
        }

        public ushort Zone
        {
            get { return _zone; }
        }

        public StateServerObject(MessageRouter router,
                                 StateServerManager stateServer,
                                 StateServerObject parent,
                                 Type objectType,
                                 uint doid,
                                 ulong owner)
        {
            _messageRouter = router;
            _messageDirector = router.MessageDirector;
            _type = objectType;
            _owner = owner;
            _parent = parent;
            _stateServer = stateServer;
            _doid = doid;
            _def = DistributedObjectRepository.GetObjectDefinition(objectType);

            _channel = Channel.Combine(Channel.DoPrefix, doid);
            _aiChannel = Channel.Combine(Channel.AIPrefix, doid);
            _clientChannel = Channel.Combine(Channel.ClPrefix, doid);

            _ram = new Dictionary<PropertyInfo, object>();
            _children = new List<StateServerObject>();

            _messageRouter.RegisterRoute(HandleMessage, _channel);

            foreach (var property in objectType.GetInterfaceProperties())
            {
                if (IsRam(property))
                {
                    _ram.Add(property, null);
                }
            }

            _stateServer.AddObject(this);
        }

        public void Destroy()
        {
            _stateServer.RemoveObject(this);
            _messageRouter.DestroyRoute(m => m.Target == this);
            _ram.Clear();

            foreach (var child in _children)
            {
                child.DestroyWithMessage();
            }
            _parent = null;
            _children = null;
            _stateServer = null;
        }

        public void DestroyWithMessage()
        {
            Destroy();

            Message message = new Message();
            message.From = _channel;
            message.AddChannel(_clientChannel);
            message.Write(MessageCode.ClientAgent_Destroy);
            message.Write(_channel);

            _messageDirector.QueueSend(message);
        }

        public void OnGenerate(Message message)
        {
            UpdateObject(message);
        }

        public void SerializeAll(Message message)
        {
            foreach (var item in _ram.Where(g => g.Value != null))
            {
                message.Write(_def.GetPropertyId(item.Key.Name));
                message.WriteObject(item.Value);
            }
        }

        public void SerializeRequired(Message message)
        {
            foreach (var item in _ram.Where(i => i.Key.HasAttribute<RequiredAttribute>() && i.Value != null))
            {
                message.Write(_def.GetPropertyId(item.Key.Name));
                message.WriteObject(item.Value);
            }
        }

        private void BroadcastUpdate(string name, object value, uint rpcId, params ulong[] channels)
        {
            Message reply = new Message();
            reply.Channels = channels;
            reply.From = _channel;

            reply.Write(MessageCode.DObject_BroadcastUpdate);
            reply.Write(_def.GetPropertyId(name));
            reply.WriteObject(value);
            reply.Write(rpcId);

            _messageDirector.QueueSend(reply);
        }

        private void DestroyChild(Message message)
        {
            uint doid = message.ReadUInt32();
            var child = _children.Find(o => o.DoId == doid);

            child.DestroyWithMessage();
            child._parent = null;
            _children.Remove(child);
        }

        private void GenerateChild(Message message)
        {
            uint id = _stateServer.GetNextDoId();

            ulong owner = message.ReadUInt64();
            Type type = DistributedObjectRepository.GetObjectDefinition(message.ReadInt32()).Type;

            var obj = new StateServerObject(_messageRouter, _stateServer, this, type, id, owner);
            obj.OnGenerate(message);
            _children.Add(obj);

            Message reply = new Message();
            reply.From = _channel;
            reply.Channels = new[] { message.From };
            reply.Write(MessageCode.StateServer_GenerateResp);
            reply.Write(id);

            Message broadcast = new Message();
            broadcast.From = _channel;
            broadcast.AddChannel(_channel);
            broadcast.Write(MessageCode.ClientAgent_Generate);
            broadcast.Write(id);
            broadcast.Write(owner);
            broadcast.Write(_def.TypeId);

            obj.SerializeRequired(broadcast);

            _messageDirector.QueueSend(broadcast);
            _messageDirector.QueueSend(reply);
            _stateServer.LogGenerated(id, type, _doid);
        }

        private void GetField(Message message)
        {
            PropertyInfo info = _def.GetProperty(message.ReadInt32());

            object value;
            if (_ram.TryGetValue(info, out value))
            {
                Message reply = new Message();
                reply.Channels = new[] { message.From };
                reply.From = _channel;

                reply.Write(MessageCode.StateServer_ObjectGetFieldResp);
                reply.Write(_def.GetPropertyId(info.Name));
                reply.WriteObject(value);

                _messageDirector.QueueSend(reply);
            }
        }

        private void HandleMessage(Message obj)
        {
            switch (obj.ReadMessageCode())
            {
                case MessageCode.ALL_QueryChannel:
                    SendQueryResponse(obj);
                    break;

                case MessageCode.StateServer_Generate:
                    GenerateChild(obj);
                    break;

                case MessageCode.StateServer_Destroy:
                    DestroyChild(obj);
                    break;

                case MessageCode.StateServer_ObjectGetField:
                    GetField(obj);
                    break;

                case MessageCode.StateServer_ObjectSetField:
                    SetField(obj);
                    break;

                case MessageCode.StateServer_ObjectSetFields:
                    HandleMultiUpdate(obj);
                    break;

                case MessageCode.StateServer_ObjectGetRequired:
                    SendRequired(obj);
                    break;

                case MessageCode.StateServer_ObjectGetAll:
                    SendAll(obj);
                    break;

                case MessageCode.StateServer_DiscoverChildren:
                    SendChildren(obj);
                    break;
            }
        }

        private void HandleMultiUpdate(Message message)
        {
            int count = message.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SetField(message);
            }
        }

        private bool IsRam(PropertyInfo info)
        {
            return info.HasAttribute<RamAttribute>() || info.HasAttribute<RequiredAttribute>();
        }

        private void SendAll(Message message)
        {
            Message reply = new Message();
            reply.From = _channel;
            reply.AddChannel(message.From);
            reply.Write(MessageCode.StateServer_ObjectGetAllResp);
            reply.Write(_parent.DoId);
            reply.Write(_owner);
            reply.Write(_type);

            SerializeAll(reply);
            _messageDirector.QueueSend(reply);
        }

        private void SendChildren(Message message)
        {
            ushort zone = message.ReadUInt16();
            var objs = _children.Where(g => g.Zone == zone).Select(g => g.DoId);

            Message response = new Message();
            response.AddChannel(message.From);
            response.From = _channel;

            response.Write(MessageCode.StateServer_DiscoverChildrenResp);
            response.WriteValues(objs.ToArray());

            _messageDirector.QueueSend(response);
        }

        private void SendQueryResponse(Message message)
        {
            Message reply = new Message();
            reply.From = _channel;
            reply.AddChannel(message.From);

            reply.Write("distributed_object");

            _messageDirector.QueueSend(reply);
        }

        private void SendRequired(Message message)
        {
            Message reply = new Message();
            reply.From = _channel;
            reply.AddChannel(message.From);
            reply.Write(MessageCode.StateServer_ObjectGetRequiredResp);
            reply.Write(_parent?.DoId ?? 0u);
            reply.Write(_owner);
            reply.Write(_def.TypeId);

            SerializeRequired(reply);
            _messageDirector.QueueSend(reply);
        }

        private void SetField(Message message)
        {
            PropertyInfo info = _def.GetProperty(message.ReadInt32());

            object value = message.ReadObject(info.PropertyType);
            if (message.Position == message.Length)
            {
                message.Write(0);
                message.Position -= sizeof(uint);
            }
            uint rpcId = message.ReadUInt32();

            if (IsRam(info))
            {
                _ram[info] = value;
            }

            List<ulong> channels = new List<ulong>();

            if (info.HasAttribute<BroadcastAttribute>())
            {
                channels.Add(_channel);
            }
            else
            {
                if (info.HasAttribute<OwnerReceiveAttribute>() && Channel.IsGameChannel(_owner))
                {
                    channels.Add(_owner);
                }
                if (info.HasAttribute<AIReceiveAttribute>())
                {
                    channels.Add(_aiChannel);
                }
                if (info.HasAttribute<ClientReceiveAttribute>())
                {
                    channels.Add(_clientChannel);
                }
            }

            if (channels.Count > 0)
            {
                BroadcastUpdate(info.Name, value, rpcId, channels.ToArray());
            }
        }

        private void UpdateObject(Message message)
        {
            int count = message.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var property = _def.GetProperty(message.ReadInt32());
                var value = message.ReadObject(property.PropertyType);

                _ram[property] = value;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Destroy();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~StateServerObject() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}