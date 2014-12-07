using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.StateServer
{
    [Obsolete]
    public class ObjectOwnerClient : ServiceClient
    {
        public override string Name
        {
            get { return "object_owner"; }
        }

        protected ObjectOwnerClient(MessageRouter router, ulong channel)
            : base(router, channel)
        {
        }

        public static ObjectOwnerClient Create(MessageRouter router, ulong channel)
        {
            return ServiceProxy.Create(new ObjectOwnerClient(router, channel));
        }

        //[RemoteMethod(MessageCode.StateServer_ZoneAddInterest, AwaitResponse = false)]
        public void AddInterest(uint zoneId)
        {
        }

        public void AddInterestMultiple(IEnumerable<uint> zones)
        {
            Message message = new Message();

            //message.Write(MessageCode.StateServer_ZoneAddInterestMultiple);
            message.Write(zones.Count());
            foreach (var zone in zones)
            {
                message.Write(zone);
            }

            //SendMessage(message);
        }

        //[RemoteMethod(MessageCode.StateServer_ZoneAddInterestRange)]
        public void AddInterestRange(uint zoneIdLow, uint zoneIdHigh)
        {
        }

        [RemoteMethod(MessageCode.StateServer_RemoveOwner)]
        public void Close() { }

        //[RemoteMethod(MessageCode.StateServer_ZoneRemoveInterest)]
        public void RemoveInterest(uint zoneId)
        {
        }

        public void RemoveInterestMultiple(IEnumerable<uint> zones)
        {
            Message message = new Message();

            //message.Write(MessageCode.StateServer_ZoneRemoveInterestMultiple);
            message.Write(zones.Count());
            foreach (var zone in zones)
            {
                message.Write(zone);
            }

            //SendMessage(message);
        }

        //[RemoteMethod(MessageCode.StateServer_ZoneRemoveInterestRange)]
        public void RemoveInterestRange(uint zoneIdLow, uint zoneIdHigh)
        {
        }
    }
}