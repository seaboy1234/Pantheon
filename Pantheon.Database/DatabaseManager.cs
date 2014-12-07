using System;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Database
{
    public class DatabaseManager : ServiceOwner
    {
        private DatabaseBackend _database;
        private MessageRouter _router;

        public DatabaseBackend Database
        {
            get { return _database; }
        }

        public DatabaseManager(MessageRouter router, ulong channel, DatabaseBackend backend)
            : base(router, "database", false, false, channel)
        {
            _router = router;

            RegisterMessageRoute(MessageCode.Database_GetObject, GetObject);
            RegisterMessageRoute(MessageCode.Database_SaveObject, SaveObject);

            _database = backend;

            EventLog.SetLoggerName("DB");
            EventLog.Log(LogLevel.Info, "Started Database on channel {0} using {1}",
                                        channel,
                                        backend.Name);
        }

        private void GetObject(Message message)
        {
            int id = message.ReadInt32();

            Message reply = new Message();
            reply.Channels = new[] { message.From };

            reply.Write(MessageCode.Database_GetObjectResp);
            reply.Write(id);
            var obj = _database.GetObject(id);

            reply.Write(obj != null);
            if (obj != null)
            {
                reply.Write(obj.Type);
                obj.WriteTo(reply);
            }

            SendMessage(reply);
        }

        private void SaveObject(Message message)
        {
            int id = message.ReadInt32();
            Type type = message.ReadType();
            SerializedObject obj = new SerializedObject(type);
            obj.ReadFrom(message);

            _database.SetObject(id, obj);
        }
    }
}