using System;
using System.Collections.Generic;
using Pantheon.Common.IO;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.StateServer
{
    [Obsolete]
    public class ZoneClient : ServiceClient, IObjectGenerator
    {
        private uint _zoneId;

        public override string Name
        {
            get { return "zone"; }
        }

        public uint ZoneId
        {
            get { return _zoneId; }
        }

        protected ZoneClient(MessageRouter router, ulong channel, uint zoneId)
            : base(router, channel)
        {
            _zoneId = zoneId;
        }

        public static ZoneClient Create(MessageRouter router, ulong channel, uint zoneId)
        {
            var zone = new ZoneClient(router, channel, zoneId);
            return (ZoneClient)new ServiceProxy(zone).GetTransparentProxy();
        }

        [RemoteMethod(MessageCode.StateServer_Destroy)]
        public void DestroyObject(ulong doid) { }

        public override bool DiscoverServer()
        {
            return ServiceChannel != 0;
        }

        [RemoteMethod(MessageCode.StateServer_Generate, ReplySkipMessageCode = true)]
        public uint GenerateObject(ulong owner, int type, IDictionary<int, object> obj) { return 0; }
    }
}