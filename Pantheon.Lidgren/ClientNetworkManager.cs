using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading;
using Lidgren.Network;
using Pantheon.Common.Event;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common
{
    public class ClientNetworkManager : INetworkManager
    {
        private NetClient _client;
        private bool _enableGzip;
        private string _host;
        private Queue<NetPacket> _incoming;
        private int _port;

        public bool EnableGZip
        {
            get { return _enableGzip; }
            set { _enableGzip = value; }
        }

        public string Host
        {
            get { return _host; }
        }

        public bool IsConnected
        {
            get
            {
                return _client.ConnectionStatus == NetConnectionStatus.Connected;
            }
        }

        public int Port
        {
            get { return _port; }
        }

        public event EventHandler<ConnectionEventArgs> OnConnected = delegate { };

        public event EventHandler<ConnectionEventArgs> OnConnectionStarted = delegate { };

        public event EventHandler<DataEventArgs> OnDataReceived = delegate { };

        public event EventHandler<DataEventArgs> OnDataSent = delegate { };

        public event EventHandler<ConnectionEventArgs> OnDisconnected = delegate { };

        public event EventHandler<DataEventArgs> OnLidgrenMessage = delegate { };

        public ClientNetworkManager(string host, int port)
        {
            Throw.IfEmpty(host, "host");
            Throw.IfValueNotInRange(port, 1024, ushort.MaxValue, "port");

            _host = host;
            _port = port;

            _incoming = new Queue<NetPacket>(64);

            var config = new NetPeerConfiguration("pantheon");
            _client = new NetClient(config);
        }

        public void Connect()
        {
            var syncContext = SynchronizationContext.Current;
            if (syncContext == null)
            {
                syncContext = new SynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncContext);
            }

            _client.RegisterReceivedCallback(OnMessageReceived, syncContext);
            _client.Start();
            _client.Connect(_host, _port);
            OnConnectionStarted(this, new ConnectionEventArgs(this));
        }

        public void Disconnect()
        {
            Disconnect("reset by peer");
        }

        public void Disconnect(string reason)
        {
            Throw.IfNull(reason, "reason");
            _client.Disconnect(reason);
            OnDisconnected(this, new ConnectionEventArgs(this));
        }

        public IO.NetStream ReadMessage()
        {
            if (_incoming.Count > 0)
            {
                return _incoming.Dequeue().Stream;
            }
            return null;
        }

        public IO.NetStream ReadMessage(out INetworkManager origin)
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

        public void WriteMessage(IO.NetStream stream)
        {
            Throw.IfNull(stream, "stream");

            NetOutgoingMessage message = _client.CreateMessage();

            Compress(stream);

            stream.CopyTo(message);

            _client.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            OnDataSent(this, new DataEventArgs(this, new NetStream(stream.Data)));
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
            while ((message = _client.ReadMessage()) != null)
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
                    OnDataReceived(this, new DataEventArgs(this, new NetStream(stream)));
                }
                if (message.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    if (_client.ConnectionStatus == NetConnectionStatus.Connected)
                    {
                        OnConnected(this, new ConnectionEventArgs(this));
                    }
                    else if (_client.ConnectionStatus == NetConnectionStatus.Disconnected)
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