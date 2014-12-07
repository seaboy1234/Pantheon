using System;
using System.Collections.Generic;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.StateServer.Event;

namespace Pantheon.StateServer
{
    [Obsolete]
    public class Zone
    {
        private ulong _channel;
        private List<StateServerObject> _children;
        private MessageRouter _router;
        private ZoneServer _server;
        private uint _zoneId;

        public ulong Channel
        {
            get { return _channel; }
        }

        public IEnumerable<StateServerObject> Children
        {
            get { return _children; }
        }

        public uint ZoneId
        {
            get { return _zoneId; }
        }

        public event EventHandler<TraverseZoneEventArgs> ObjectEntered = delegate { };

        public event EventHandler<TraverseZoneEventArgs> ObjectGenerated = delegate { };

        public event EventHandler<TraverseZoneEventArgs> ObjectLeft = delegate { };

        public Zone(ZoneServer server, MessageRouter router, uint zoneId)
        {
            _zoneId = zoneId;
            _children = new List<StateServerObject>();
            _server = server;
            _router = router;
            _channel = (ulong)server.ServerId << 32 | zoneId;

            router.RegisterRoute(OnMessageReceived, _channel);
        }

        public void AddObject(StateServerObject obj)
        {
            ObjectEntered(this, new TraverseZoneEventArgs(this, obj));
            _children.Add(obj);
        }

        public bool ContainsObject(ulong doid)
        {
            return GetObject(doid) != null;
        }

        public StateServerObject GetObject(ulong doid)
        {
            return _children.Find(o => o.DoId == doid);
        }

        public void RemoveObject(StateServerObject obj)
        {
            _children.Remove(obj);
            ObjectLeft(this, new TraverseZoneEventArgs(this, obj));
        }

        public void Teardown()
        {
            ObjectEntered = null;
            ObjectGenerated = null;
            ObjectLeft = null;

            _router.DestroyRoute(r => r.Target == this);

            foreach (var obj in _children)
            {
                obj.Destroy();
            }
        }

        private void OnMessageReceived(Message message)
        {
            var action = message.ReadMessageCode();
            if (action == MessageCode.StateServer_Generate)
            {
                uint id = _server.Server.GetNextDoId();
                ulong owner = message.ReadUInt64();
                Type type = message.ReadType();

                var obj = new StateServerObject(_router, _server.Server, null, type, id, owner);
                obj.OnGenerate(message);
                _children.Add(obj);

                ObjectGenerated(this, new TraverseZoneEventArgs(this, obj));

                Message reply = new Message();
                reply.From = _channel;
                reply.Channels = new[] { message.From };
                reply.Write(MessageCode.StateServer_GenerateResp);
                reply.Write(id);

                _router.MessageDirector.QueueSend(reply);
                _server.Server.LogGenerated(id, type, 0);
            }
            else if (action == MessageCode.StateServer_Destroy)
            {
                ulong id = message.ReadUInt64();
                var obj = GetObject(id);
                RemoveObject(obj);
            }
        }
    }
}