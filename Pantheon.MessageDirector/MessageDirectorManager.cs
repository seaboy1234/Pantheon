using System;
using System.Threading;
using Pantheon.Common;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.MessageDirector.Statistics;

namespace Pantheon.MessageDirector
{
    public class MessageDirectorManager : IDisposable
    {
        private readonly INetworkManager _networkManager;
        private readonly MDServer _server;
        private bool _continuePumping;
        private MessageRouter _router;
        private StatisticManager _stats;

        public IMessageDirector MessageDirector
        {
            get { return _server; }
        }

        public MessageRouter MessageRouter
        {
            get
            {
                if (_router == null)
                {
                    _router = new MessageRouter(_server);
                }
                return _router;
            }
        }

        public event EventHandler OnHeartbeat = delegate { };

        public event EventHandler OnHeartbeatStarted = delegate { };

        public event EventHandler OnHeartbeatStopped = delegate { };

        public MessageDirectorManager(INetworkManager networkManager)
        {
            _server = new MDServer(networkManager);
            _server.OnPump += OnHeartbeat;
            _networkManager = networkManager;

            _stats = new StatisticManager(_server);
        }

        public void DoMessageLoop()
        {
            OnHeartbeatStarted(this, EventArgs.Empty);
            _continuePumping = true;
            while (_continuePumping && _networkManager.IsConnected)
            {
                _server.Pump();
                Thread.Sleep(1);
            }

            OnHeartbeatStopped(this, EventArgs.Empty);
        }

        public void StopMessageLoop()
        {
            _continuePumping = false;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _networkManager.Disconnect();
                    _stats.Dispose();
                    _continuePumping = false;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~MessageDirectorManager() { // Do not change this code. Put
        // cleanup code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}