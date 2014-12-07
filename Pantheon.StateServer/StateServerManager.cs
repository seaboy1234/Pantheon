using System;
using System.Collections.Generic;
using Pantheon.Common.DistributedObjects;
using Pantheon.Core;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.StateServer
{
    public class StateServerManager : ServiceOwner
    {
        private ulong _channel;
        private ulong _nextClientId;
        private uint _nextObjectId;
        private uint _nextServerId;
        private List<StateServerObject> _objects;

        public StateServerManager(MessageRouter router, ulong channel)
            : base(router, "state_server", false, false, channel)
        {
            _channel = channel;
            _objects = new List<StateServerObject>();
            _nextClientId = Channel.Combine(Channel.ClientPrefix, 0);
            _nextServerId = 100;
            _nextObjectId = 1;

            AddChannel(Channel.AllStateServers);

            RegisterMessageRoute(MessageCode.StateServer_Generate, GenerateRootObject);

            EventLog.SetLoggerName("SS");
            EventLog.Log(LogLevel.Info, "Started StateServer on channel {0}", channel);
        }

        public void AddObject(StateServerObject obj)
        {
            _objects.Add(obj);
        }

        public ulong GetAIChannel(uint doid)
        {
            return Channel.Combine(Channel.AIPrefix, doid);
        }

        public ulong GetClientChannel(uint doid)
        {
            return Channel.Combine(Channel.ClPrefix, doid);
        }

        public uint GetNextDoId()
        {
            if (_nextObjectId == 0)
            {
                _nextObjectId++;
            }
            return _nextObjectId++;
        }

        public uint GetNextServerId()
        {
            return _nextServerId++;
        }

        public ulong GetObjectChannel(uint doid)
        {
            return Channel.Combine(Channel.DoPrefix, doid);
        }

        public void Log(LogLevel logLevel, string p)
        {
            LogFile.Log(logLevel, p);
        }

        public void LogGenerated(uint id, Type type, uint parent)
        {
            if (parent != 0)
            {
                LogFile.Log(LogLevel.Debug, "Generated a {0} (p:{1}, doid:{2})", type.Name, parent, id);
            }
            else
            {
                LogFile.Log(LogLevel.Debug, "Generated a {1} (doid:{0})", id, type.Name);
            }
        }

        public void RemoveObject(StateServerObject obj)
        {
            _objects.Remove(obj);
        }

        [Endpoint(MessageCode.StateServer_CreateOwner, RequireReturnPath = true)]
        [ReplyWith(MessageCode.StateServer_CreateOwnerResp, Serialization = Serialization.WriteObject)]
        private ulong CreateOwner(ulong from)
        {
            var ownerId = _nextClientId++;

            return ownerId;
        }

        [Endpoint(MessageCode.StateServer_Destroy)]
        private void Destroy(uint doid)
        {
            var obj = _objects.Find(g => g.DoId == doid);
            if (obj == null)
            {
                return;
            }
            obj.DestroyWithMessage();
        }

        [Endpoint(MessageCode.StateServer_DispenseDoId)]
        private uint DispenseDoId()
        {
            return GetNextDoId();
        }

        private void GenerateRootObject(Message message)
        {
            uint id = GetNextDoId();

            ulong owner = message.ReadUInt64();
            Type type = DistributedObjectRepository.GetObjectDefinition(message.ReadInt32()).Type;

            var obj = new StateServerObject(MessageRouter, this, null, type, id, owner);
            obj.OnGenerate(message);

            Message reply = new Message();
            reply.From = _channel;
            reply.Channels = new[] { message.From };
            reply.Write(MessageCode.StateServer_GenerateResp);
            reply.Write(id);

            SendMessage(reply);
            LogGenerated(id, type, 0);
            _objects.Add(obj);
        }

        [Endpoint(MessageCode.StateServer_CloseServer)]
        private void ZoneServerExited(ulong channel)
        {
        }
    }
}