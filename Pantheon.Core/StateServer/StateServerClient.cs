using System;
using System.Collections.Generic;
using Pantheon.Common.IO;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.StateServer
{
    public class StateServerClient : ServiceClient, IObjectGenerator
    {
        public override string Name
        {
            get { return "state_server"; }
        }

        public StateServerClient(MessageRouter router)
            : base(router, 0)
        {
            DiscoverServer();
        }

        [RemoteMethod(MessageCode.StateServer_CreateOwner, ReplySkipMessageCode = true)]
        public ulong CreateObjectOwner() { return 0; }

        [RemoteMethod(MessageCode.StateServer_Destroy)]
        public void DestroyObject(uint doid) { }

        [RemoteMethod(MessageCode.StateServer_DispenseDoId)]
        public ulong DispenseObjectId() { return 0; }

        public uint GenerateObject(ulong owner, int type, IDictionary<int, object> obj)
        {
            Message message = new Message();
            message.AddChannel(ServiceChannel);

            message.Write(MessageCode.StateServer_Generate);
            message.Write(owner);
            message.Write(type);
            message.WriteDictionary(obj);

            var reply = message.AwaitReply(MessageRouter);
            reply.Position += sizeof(int);
            return reply.ReadUInt32();
        }

        [RemoteMethod(MessageCode.StateServer_MoveOwner, ReplySkipMessageCode = true)]
        public bool OwnerSetServer(ulong owner, ulong zoneServer) { return false; }

        [RemoteMethod(MessageCode.StateServer_RemoveOwner)]
        public void RemoveObjectOwner(ulong owner) { }

        [RemoteMethod(MessageCode.StateServer_CloseServer)]
        public void ZoneServerExited(ulong channel) { }
    }
}