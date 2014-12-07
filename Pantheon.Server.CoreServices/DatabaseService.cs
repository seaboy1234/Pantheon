using System;
using System.IO;
using System.Xml.Linq;
using Pantheon.Core.MessageRouting;
using Pantheon.Database;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server.CoreServices
{
    public class DatabaseService : PantheonService, IDisposable
    {
        private DatabaseBackend _backend;
        private ulong _channel;
        private DatabaseManager _db;
        private MessageRouter _router;

        public override string Name
        {
            get { return "Database"; }
        }

        protected override string[] Dependencies
        {
            get
            {
                return new[] { "EventLogger" };
            }
        }

        public override void Start()
        {
            _router = new MessageRouter(MessageDirector);
            _db = new DatabaseManager(_router, _channel, _backend);
        }

        public override void Stop()
        {
            _db.Dispose();
        }

        protected override ConfigurationModule GenerateDefault()
        {
            return new ConfigurationModule("Service",
                                    new ConfigurationModule("Disabled", true).Attribute(),
                                    new ConfigurationModule("Channel", 3000),
                                    new ConfigurationModule("Backend", "FileSystem"));
        }

        protected override void LoadConfig()
        {
            _channel = Config.Child("Channel").UInt64(1100);
            var name = Config.Child("Backend").String("File");
            var path = Path.Combine("Database", "ObjectDb");
            _backend = DatabaseBackend.CreateInstance(name, path);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above. GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _db.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~DatabaseService() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}