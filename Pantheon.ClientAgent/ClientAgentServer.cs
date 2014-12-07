using System;
using Pantheon.Common.Event;

namespace Pantheon.ClientAgent
{
    public class ClientAgentServer
    {
        private ClientListener _clientListener;
        private ClientManager _clientManager;

        public ClientAgentServer(ClientAgentConfiguration config)
        {
            config.Lock();
            _clientListener = new ClientListener(config.MessageRouter, config.Port, config.Channel, config.Tcp);
            _clientManager = new ClientManager(config.MessageRouter);
            _clientManager.OpenChannels(config.DefaultChannels);

            _clientManager.SetVersion(config.Version);

            _clientListener.OnClientConnected += ClientJoin;
            _clientManager.GetNewId();
        }

        public void Start()
        {
            _clientListener.Start();
        }

        public void Stop()
        {
            _clientListener.Stop();
        }

        private void ClientJoin(object sender, ConnectionEventArgs e)
        {
            _clientManager.AddClient(e.NetworkManager);
        }
    }
}