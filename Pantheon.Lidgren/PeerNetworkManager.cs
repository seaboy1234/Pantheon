using System;
using System.Net;
using Lidgren.Network;
using Pantheon.Common.Event;
using Pantheon.Common.IO;

namespace Pantheon.Common
{
    internal class PeerNetworkManager : INetworkManager
    {
        private NetPeer _peer;

        public bool EnableGZip
        {
            get;
            set;
        }

        public string Host
        {
            get { return _peer.Configuration.LocalAddress.ToString(); }
        }

        public bool IsConnected
        {
            get { return false; }
        }

        public int Port
        {
            get { return _peer.Port; }
        }

        public event EventHandler<ConnectionEventArgs> OnConnected;

        public event EventHandler<ConnectionEventArgs> OnConnectionStarted;

        public event EventHandler<DataEventArgs> OnDataReceived;

        public event EventHandler<DataEventArgs> OnDataSent;

        public event EventHandler<ConnectionEventArgs> OnDisconnected;

        public event EventHandler<DataEventArgs> OnLidgrenMessage;

        public PeerNetworkManager()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("pantheon");

            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            config.LocalAddress = IPAddress.Loopback;

            _peer = new NetPeer(config);
        }

        public void Connect()
        {
            _peer.Start();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect(string reason)
        {
            throw new NotImplementedException();
        }

        public NetStream ReadMessage()
        {
            throw new NotImplementedException();
        }

        public NetStream ReadMessage(out INetworkManager origin)
        {
            throw new NotImplementedException();
        }

        public void WriteMessage(NetStream stream)
        {
            throw new NotImplementedException();
        }
    }
}