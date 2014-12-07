using System;
using System.Xml.Linq;
using Pantheon.Core.MessageRouting;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;
using Pantheon.StateServer;

namespace Pantheon.Server.CoreServices
{
    public class StateServerService : PantheonService, IDisposable
    {
        private ulong _baseChannel;
        private StateServerManager _stateServer;

        public override string Name
        {
            get { return "StateServer"; }
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
            _stateServer = new StateServerManager(new MessageRouter(MessageDirector), _baseChannel);
        }

        public override void Stop()
        {
        }

        protected override ConfigurationModule GenerateDefault()
        {
            return new ConfigurationModule("Service",
                                new ConfigurationModule("Channel", 2000));
        }

        protected override void LoadConfig()
        {
            _baseChannel = Config.Child("Channel").UInt64(2000);
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
                    _stateServer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~StateServerService() { // Do not change this code. Put
        // cleanup code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}