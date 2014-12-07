using System;
using System.Collections.Generic;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.StateServer.Event;

namespace Pantheon.StateServer
{
    [Obsolete]
    public class ZoneServer
    {
        private ulong _channel;
        private ulong _clientChannel;
        private IMessageDirector _messageDirector;
        private List<ObjectOwner> _owners;
        private MessageRouter _router;
        private StateServerManager _server;
        private uint _serverId;
        private List<Zone> _zones;

        public ulong Channel
        {
            get { return _channel; }
        }

        public ulong ClientChannel
        {
            get { return _clientChannel; }
        }

        public StateServerManager Server
        {
            get { return _server; }
        }

        public uint ServerId
        {
            get { return _serverId; }
        }

        public IEnumerable<Zone> Zones
        {
            get { return _zones; }
        }

        public event EventHandler<TraverseZoneEventArgs> ObjectGenerated = delegate { };

        public ZoneServer(StateServerManager server, MessageRouter router, ulong channel, ulong clientChannel)
        {
            _server = server;
            _router = router;
            _messageDirector = router.MessageDirector;
            _channel = channel;
            _clientChannel = clientChannel;
            _serverId = server.GetNextServerId();

            _zones = new List<Zone>();
            _owners = new List<ObjectOwner>();

            _router.RegisterRoute(MessageReceived, channel);
        }

        public void AddOwner(ObjectOwner objectOwner)
        {
            _owners.Add(objectOwner);
        }

        public ObjectOwner GetOwner(ulong ownerId)
        {
            return _owners.Find(o => o.Channel == ownerId);
        }

        public Zone GetZone(uint zoneId)
        {
            return _zones.Find(z => z.ZoneId == zoneId);
        }

        public void RemoveOwner(ObjectOwner objectOwner)
        {
            _owners.Remove(objectOwner);
        }

        public void Teardown()
        {
            _router.DestroyRoute(r => r.Target == this);
            foreach (var zone in _zones)
            {
                zone.Teardown();
            }
        }

        private Zone CreateZone(uint zoneId)
        {
            Zone zone = new Zone(this, _router, zoneId);
            _zones.Add(zone);
            return zone;
        }

        private void MessageReceived(Message message)
        {
            MessageCode action = message.ReadMessageCode();
            if (action == MessageCode.StateServer_CreateZone)
            {
                uint zoneId = message.ReadUInt32();

                var zone = CreateZone(zoneId);

                Message reply = new Message();
                reply.Channels = new[] { message.From };
                reply.From = _channel;

                reply.Write(MessageCode.StateServer_CreateZoneResp);
                reply.Write(zone.ZoneId);
                reply.Write(zone.Channel);

                _messageDirector.QueueSend(reply);
            }
            else if (action == MessageCode.StateServer_RemoveZone)
            {
                uint zone = message.ReadUInt32();
                RemoveZone(zone);
            }
            //else if (action == MessageCode.StateServer_MoveObject)
            //{
            //    ulong doid = message.ReadUInt64();
            //    uint zoneId = message.ReadUInt32();

            // Zone currentZone = _zones.Find(z => z.ContainsObject(doid)); StateServerObject obj =
            // currentZone.GetObject(doid);

            // Zone newZone = _zones.Find(z => z.ZoneId == zoneId);

            //    newZone.AddObject(obj);
            //    currentZone.RemoveObject(obj);
            //}
        }

        private void RemoveZone(uint zoneId)
        {
            _zones.RemoveAll(z => z.ZoneId == zoneId);
        }
    }
}