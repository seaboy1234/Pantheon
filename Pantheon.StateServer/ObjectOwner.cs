using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.Core.ClientAgent;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;
using Pantheon.StateServer.Event;

namespace Pantheon.StateServer
{
    [Obsolete]
    public class ObjectOwner
    {
        private ulong _channel;
        private PantheonClientAgent _client;
        private List<Zone> _interest;
        private IMessageDirector _messageDirector;
        private List<Message> _messages;
        private MessageRouter _router;
        private ZoneServer _server;
        private StateServerManager _stateServer;
        private List<ulong> _visible;

        public ulong Channel
        {
            get { return _channel; }
        }

        public IEnumerable<StateServerObject> VisibleObjects
        {
            get { return _interest.SelectMany(z => z.Children).Distinct(); }
        }

        public ObjectOwner(StateServerManager stateServer, MessageRouter router, ulong channel)
        {
            _router = router;
            _messageDirector = router.MessageDirector;
            _channel = channel;
            _stateServer = stateServer;

            _visible = new List<ulong>();
            _interest = new List<Zone>();
            _messages = new List<Message>();
            _client = PantheonClientAgent.Create(router, channel);

            _router.RegisterRoute(MessageReceived, channel);
        }

        public void AddInterest(Zone zone)
        {
            if (_interest.Contains(zone))
            {
                return;
            }

            _interest.Add(zone);
            zone.ObjectEntered += OnObjectEnteredZone;
            zone.ObjectLeft += OnObjectLeftZone;
            zone.ObjectGenerated += OnObjectGenerated;

            foreach (var obj in zone.Children)
            {
                OnObjectGenerated(this, new TraverseZoneEventArgs(zone, obj));
            }
            _stateServer.Log(LogLevel.Info, "Client added interest in zone " + _server.ServerId + ":" + zone.ZoneId);
        }

        public void Close()
        {
            _router.DestroyRoute(m => m.Target == this);

            foreach (var zone in _interest)
            {
                foreach (var obj in zone.Children.Where(z => z.Owner == _channel).ToArray())
                {
                    obj.Destroy();
                    zone.RemoveObject(obj);
                }
            }
            _interest.Clear();
            var method = new Action<Message>(MessageReceived).Method;
        }

        public void OnZoneServerChanged(ZoneServer zoneServer)
        {
            _client.PurgeInterest();

            foreach (var zone in _interest)
            {
                RemoveInterest(zone);
            }
            _interest.Clear();

            _client.SetZoneServer(zoneServer.Channel, zoneServer.ClientChannel);
            _server = zoneServer;
            if (_messages.Count > 0)
            {
                foreach (var message in _messages)
                {
                    MessageReceived(message);
                }
                _messages.Clear();
            }
        }

        public void RemoveInterest(Zone zone)
        {
            _interest.Remove(zone);
            zone.ObjectEntered -= OnObjectEnteredZone;
            zone.ObjectLeft -= OnObjectLeftZone;
            zone.ObjectGenerated -= OnObjectGenerated;

            foreach (var obj in zone.Children)
            {
                if (obj.Owner == _channel)
                {
                    continue;
                }
                _client.Destroy(obj.DoId);
            }
        }

        private void MessageReceived(Message message)
        {
            MessageCode action = message.ReadMessageCode();
            if (_server == null)
            {
                message.Position -= 4;
                _messages.Add(message);
                return;
            }
            //if (action == MessageCode.StateServer_ZoneAddInterest)
            //{
            //    uint zoneId = message.ReadUInt32();
            //    Zone zone = _server.GetZone(zoneId);
            //    if (zone == null)
            //    {
            //        // TODO: fallback?
            //        return;
            //    }

            //    AddInterest(zone);
            //}
            //else if (action == MessageCode.StateServer_ZoneAddInterestMultiple)
            //{
            //    int count = message.ReadInt32();
            //    for (int i = 0; i < count; i++)
            //    {
            //        uint zoneId = message.ReadUInt32();
            //        Zone zone = _server.GetZone(zoneId);
            //        AddInterest(zone);
            //    }
            //}
            //else if (action == MessageCode.StateServer_ZoneAddInterestRange)
            //{
            //    uint low = message.ReadUInt32();
            //    uint high = message.ReadUInt32();

            //    for (uint i = low; i < high; i++)
            //    {
            //        Zone zone = _server.GetZone(i);
            //        AddInterest(zone);
            //    }
            //}
            //else if (action == MessageCode.StateServer_ZoneRemoveInterest)
            //{
            //    uint zoneId = message.ReadUInt32();
            //    Zone zone = _server.GetZone(zoneId);

            //    RemoveInterest(zone);
            //}
            //else if (action == MessageCode.StateServer_ZoneRemoveInterestMultiple)
            //{
            //    int count = message.ReadInt32();
            //    for (int i = 0; i < count; i++)
            //    {
            //        uint zoneId = message.ReadUInt32();
            //        Zone zone = _server.GetZone(zoneId);
            //        RemoveInterest(zone);
            //    }
            //}
            //else if (action == MessageCode.StateServer_ZoneRemoveInterestRange)
            //{
            //    uint low = message.ReadUInt32();
            //    uint high = message.ReadUInt32();

            //    for (uint i = low; i < high; i++)
            //    {
            //        Zone zone = _server.GetZone(i);
            //        RemoveInterest(zone);
            //    }
            //}
            //else if (action == MessageCode.ALL_QueryChannel)
            //{
            //    Message reply = new Message();
            //    reply.Channels = new[] { message.From };
            //    reply.Write("owner");

            //    _router.MessageDirector.QueueSend(reply);
            //}
        }

        private void OnObjectEnteredZone(object sender, TraverseZoneEventArgs e)
        {
            if (!_visible.Contains(e.Target.DoId))
            {
                OnObjectGenerated(sender, e);
            }
            else
            {
                Message message = new Message();
                message.Channels = new[] { _channel };
                message.From = _channel;

                message.Write(MessageCode.ClientAgent_ObjectChangedZones);
                message.Write(e.Target.DoId);
                message.Write(e.Zone.ZoneId);

                _messageDirector.QueueSend(message);
            }
        }

        private void OnObjectGenerated(object sender, TraverseZoneEventArgs e)
        {
            var obj = e.Target;
            if (_visible.Contains(obj.DoId))
            {
                return;
            }
            _visible.Add(obj.DoId);
        }

        private void OnObjectLeftZone(object sender, TraverseZoneEventArgs e)
        {
            if (!VisibleObjects.Contains(e.Target))
            {
                if (e.Target.Owner == _channel)
                {
                    return;
                }
                _client.Destroy(e.Target.DoId);
                _visible.Remove(e.Target.DoId);
            }
        }
    }
}