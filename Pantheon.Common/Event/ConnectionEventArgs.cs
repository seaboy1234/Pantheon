using System;

namespace Pantheon.Common.Event
{
    public class ConnectionEventArgs : EventArgs
    {
        private readonly INetworkManager _networkManager;

        public INetworkManager NetworkManager { get { return _networkManager; } }

        public ConnectionEventArgs(INetworkManager networkManager)
        {
            _networkManager = networkManager;
        }
    }
}