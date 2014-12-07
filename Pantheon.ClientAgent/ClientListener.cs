using System;
using Pantheon.Common;
using Pantheon.Common.Event;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.ClientAgent
{
    public class ClientListener
    {
        private ulong _channel;
        private EventLogger _logger;
        private IMessageDirector _messageDirector;
        private INetworkManager _networkManager;
        private MessageRouter _router;

        public event EventHandler<ConnectionEventArgs> OnClientConnected = delegate { };

        public ClientListener(MessageRouter router, int port, ulong commChannel, bool tcp)
        {
            var features = NetworkManagerFeatures.Server | (tcp ? NetworkManagerFeatures.Tcp : NetworkManagerFeatures.Udp);
            _networkManager = NetworkManagerFactory.CreateNetworkManager(NetworkManagerFeatures.Server, port);
            _messageDirector = router.MessageDirector;
            _channel = commChannel;
            _router = router;

            _logger = ServiceManager.GetServiceHandle<EventLogger>(router);
            _logger.SetLoggerName("CA");
        }

        public void Start()
        {
            _logger.Log(LogLevel.Info, "Starting ClientAgent on port {0}", _networkManager.Port);
            _networkManager.Connect();
            _networkManager.OnConnected += ClientConnected;
        }

        public void Stop()
        {
            _logger.Log(LogLevel.Info, "Stopping ClientAgent on port {0}", _networkManager.Port);
            _networkManager.Disconnect("ClientAgent stopped.");
        }

        private void ClientConnected(object sender, ConnectionEventArgs e)
        {
            OnClientConnected(this, e);
        }
    }
}