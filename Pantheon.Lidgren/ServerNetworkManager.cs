using System;
using System.Collections.Generic;
using System.IO.Compression;
using Lidgren.Network;
using Pantheon.Common.Event;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common
{
    /// <summary>
    ///   Represents the server in a client-server communication model.
    /// </summary>
    public class ServerNetworkManager : INetworkManager
    {
        private List<ConnectionNetworkManager> _connections;
        private bool _enableGzip;
        private Queue<NetPacket> _incoming;
        private int _port;
        private NetServer _server;

        public bool EnableGZip
        {
            get { return _enableGzip; }
            set { _enableGzip = value; }
        }

        /// <summary>
        ///   Gets the host that this <see cref="ServerNetworkManager" /> is listening on.
        /// </summary>
        public string Host
        {
            get { return "0.0.0.0"; }
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _server.Status == NetPeerStatus.Running;
            }
        }

        /// <summary>
        ///   Gets the port that this <see cref="ServerNetworkManager" /> is bound to.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        public event EventHandler<ConnectionEventArgs> OnClientConnected = delegate { };

        public event EventHandler<ConnectionEventArgs> OnConnected = delegate { };

        public event EventHandler<ConnectionEventArgs> OnConnectionStarted = delegate { };

        public event EventHandler<DataEventArgs> OnDataReceived = delegate { };

        public event EventHandler<DataEventArgs> OnDataSent = delegate { };

        public event EventHandler<ConnectionEventArgs> OnDisconnected = delegate { };

        public event EventHandler<DataEventArgs> OnLidgrenMessage = delegate { };

        public ServerNetworkManager(int port)
        {
            Throw.IfValueNotInRange(port, 1024, ushort.MaxValue, "port");

            NetPeerConfiguration config = new NetPeerConfiguration("pantheon");
            config.Port = port;
            _port = port;

            _server = new NetServer(config);
            _connections = new List<ConnectionNetworkManager>();
            _incoming = new Queue<NetPacket>(1024);
        }

        public void Connect()
        {
            _server.RegisterReceivedCallback(OnMessageReceived, new System.Threading.SynchronizationContext());
            _server.Start();
        }

        public void Disconnect()
        {
            Disconnect("shutdown");
        }

        public void Disconnect(string reason)
        {
            Throw.IfNull(reason, "reason");
            _server.Shutdown(reason);
        }

        public NetStream ReadMessage()
        {
            if (_incoming.Count > 0)
            {
                NetPacket packet = _incoming.Dequeue();
                return packet.Stream;
            }
            return null;
        }

        public NetStream ReadMessage(out INetworkManager origin)
        {
            if (_incoming.Count > 0)
            {
                NetPacket packet = _incoming.Dequeue();
                origin = packet.Origin;
                return packet.Stream;
            }
            origin = null;
            return null;
        }

        public void WriteMessage(NetStream stream)
        {
            Throw.IfNull(stream, "stream");

            NetOutgoingMessage message = _server.CreateMessage();

            Compress(stream);

            stream.CopyTo(message);

            _server.SendMessage(message, _server.Connections, NetDeliveryMethod.ReliableOrdered, 0);
            OnDataSent(this, new DataEventArgs(this, new NetStream(stream.Data)));
        }

        internal void GiveMessage(NetStream stream, INetworkManager origin)
        {
            if (_incoming.Count >= 1000)
            {
                _incoming.Clear();
            }
            _incoming.Enqueue(new NetPacket(origin, stream));
        }

        private void Compress(NetStream stream)
        {
            if (!_enableGzip)
            {
                return;
            }

            NetStream compressed = new NetStream();
            using (GZipStream gzip = new GZipStream(compressed, CompressionMode.Compress))
            {
                gzip.Write(stream.Data, 0, stream.Data.Length);
            }
            stream.Data = compressed.Data;
        }

        private void Decompress(NetStream stream)
        {
            if (!_enableGzip)
            {
                return;
            }

            using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
            using (NetStream decompress = new NetStream())
            {
                const int count = 4096;
                int read = 1;
                while (read > 0)
                {
                    byte[] buffer = new byte[count];
                    read = gzip.Read(buffer, 0, count);
                    decompress.Write(buffer);
                }
                stream.Data = decompress.Data;
            }
        }

        private void OnMessageReceived(object state)
        {
            NetIncomingMessage message;
            while ((message = _server.ReadMessage()) != null)
            {
                var networkManager = _connections.Find(p => p.Connection == message.SenderConnection);
                if (networkManager == null)
                {
                    networkManager = new ConnectionNetworkManager(message.SenderConnection);
                    _connections.Add(networkManager);
                    OnClientConnected(this, new ConnectionEventArgs(networkManager));
                }
                networkManager.OnMessageReceived(message);
                message.Position = 0;
                if (message.MessageType == NetIncomingMessageType.Data)
                {
                    NetStream stream = new NetStream();
                    stream.Write(message);

                    Decompress(stream);

                    GiveMessage(new NetStream(stream), networkManager);
                    message.Position = 0;
                    OnDataReceived(this, new DataEventArgs(this, new NetStream(stream)));
                }
                if (message.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    if (_server.Status == NetPeerStatus.Running)
                    {
                        OnConnected(this, new ConnectionEventArgs(this));
                    }
                    else if (_server.Status == NetPeerStatus.NotRunning)
                    {
                        OnDisconnected(this, new ConnectionEventArgs(this));
                    }
                }
                if (message.MessageType == NetIncomingMessageType.ErrorMessage ||
                    message.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    NetStream stream = new NetStream();
                    stream.Write(message);

                    OnLidgrenMessage(this, new DataEventArgs(this, stream));
                }
            }
        }
    }
}