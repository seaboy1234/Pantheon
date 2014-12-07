using System;
using Pantheon.Common.IO;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.Database
{
    public class DatabaseClient : ServiceClient
    {
        public override string Name
        {
            get { return "database"; }
        }

        protected DatabaseClient(MessageRouter router)
            : base(router, 0)
        {
        }

        public static DatabaseClient Create(MessageRouter router)
        {
            return ServiceProxy.Create(new DatabaseClient(router));
        }

        [RemoteMethod(MessageCode.Database_GetObject)]
        public SerializedObject GetObject(ulong id) { return null; }

        [RemoteMethod(MessageCode.Database_SaveObject, ReplySkipMessageCode = true)]
        public bool SetObject(ulong id, SerializedObject obj) { return false; }

        private SerializedObject GetObjectResp(Message message)
        {
            bool objectFound = message.ReadBoolean();
            if (!objectFound)
            {
                return null;
            }
            Type type = message.ReadType();
            return new SerializedObject(type, message);
        }
    }
}