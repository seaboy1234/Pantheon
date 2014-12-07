using System;
using System.Collections.Generic;
using System.IO.Compression;
using Lidgren.Network;
using Pantheon.Common.Event;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common
{
    public class ConnectionNetworkManager : INetworkManager
    {
        private NetConnection _client;
        private bool _enableGzip;

        private Queue<NetPacket> _incoming;

        public bool EnableGZip
        {
            get { return _enableGzip; }
            set { _enableGzip = value; }
        }

        public string Host
        {
            get
            {
                if (_client == null)
                {
                    return null;
                }
                return _client.RemoteEndPoint.Address.ToString();
            }
        }

        public bool IsConnected
        {
            get
            {
                return _client.Status == NetConnectionStatus.Connected;
            }
        }

        public int Port
        {
            get { return _client.RemoteEndPoint.Port; }
        }

        internal NetConnection Connection
        {
            get { return _client; }
        }

        public event EventHandler<ConnectionEventArgs> OnConnected = delegate { };

        public event EventHandler<ConnectionEventArgs> OnConnectionStarted = delegate { };

        public event EventHandler<DataEventArgs> OnDataReceived = delegate { };

        public event EventHandler<DataEventArgs> OnDataSent = delegate { };

        public event EventHandler<ConnectionEventArgs> OnDisconnected = delegate { };

        public event EventHandler<DataEventArgs> OnLidgrenMessage = delegate { };

        internal ConnectionNetworkManager(NetConnection connection)
        {
            _client = connection;
            _incoming = new Queue<NetPacket>(128);
        }

        private struct NetPacket
        {
            public INetworkManager Origin { get; set; }

            public NetStream Stream { get; set; }
        }

        public void Connect()
        {
            throw new NotSupportedException();
        }

        public void Disconnect()
        {
            Disconnect("reset by peer");
        }

        public void Disconnect(string reason)
        {
            _client.Disconnect(reason);
            OnDisconnected(this, new ConnectionEventArgs(this));
        }

        public NetStream ReadMessage()
        {
            if (_incoming.Count > 0)
            {
                return _incoming.Dequeue().Stream;
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

            NetOutgoingMessage message = _client.Peer.CreateMessage();

            Compress(stream);

            stream.CopyTo(message);

            _client.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
            OnDataSent(this, new DataEventArgs(this, new NetStream(stream.Data)));
        }

        internal void OnMessageReceived(NetIncomingMessage message)
        {
            if (message.MessageType == NetIncomingMessageType.Data)
            {
                NetStream stream = new NetStream();
                stream.Write(message);

                Decompress(stream);

                NetPacket packet = new NetPacket();
                packet.Origin = this;
                packet.Stream = new NetStream(stream);

                if (_incoming.Count >= 100)
                {
                    _incoming.Clear();
                }

                _incoming.Enqueue(packet);

                OnDataReceived(this, new DataEventArgs(this, stream));
            }
            if (message.MessageType == NetIncomingMessageType.StatusChanged)
            {
                if (_client.Status == NetConnectionStatus.Connected)
                {
                    OnConnected(this, new ConnectionEventArgs(this));
                }
                else if (_client.Status == NetConnectionStatus.Disconnected)
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
    }
}