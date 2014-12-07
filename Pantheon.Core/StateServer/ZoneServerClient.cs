using System;
using System.Collections.Generic;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.StateServer
{
    [Obsolete]
    public class ZoneServerClient : ServiceClient
    {
        private ulong _clientChannel;
        private List<ZoneClient> _zones;

        public ulong ClientChannel
        {
            get { return _clientChannel; }
        }

        public override string Name
        {
            get { return "zone_server"; }
        }

        protected ZoneServerClient(MessageRouter router, ulong channel, ulong clientChannel)
            : base(router, channel)
        {
            _zones = new List<ZoneClient>();
            _clientChannel = clientChannel;
        }

        public static ZoneServerClient Create(MessageRouter router, ulong channel, ulong clientChannel)
        {
            var client = new ZoneServerClient(router, channel, clientChannel);
            var proxy = new ServiceProxy(client);

            return (ZoneServerClient)proxy.GetTransparentProxy();
        }

        [RemoteMethod(MessageCode.StateServer_CreateZone, ReplySkipMessageCode = true)]
        public ZoneClient CreateZone(uint zoneId) { return null; }

        public override bool DiscoverServer()
        {
            return ServiceChannel != 0;
        }

        public ZoneClient GetZone(uint zoneId)
        {
            var zone = _zones.Find(z => z.ZoneId == zoneId);
            return zone;
        }

        [RemoteMethod(MessageCode.StateServer_RemoveZone)]
        public void RemoveZone(uint zoneId)
        {
            _zones.RemoveAll(z => z.ZoneId == zoneId);
        }

        private ZoneClient CreateZoneResp(Message message)
        {
            uint zoneId = message.ReadUInt32();
            ulong channel = message.ReadUInt64();

            var zone = ZoneClient.Create(MessageRouter, channel, zoneId);
            _zones.Add(zone);

            return zone;
        }
    }
}