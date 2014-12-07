using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Pantheon.Common.Event;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common.TCP
{
    public class TcpNetworkManagerServer : INetworkManager, IDisposable
    {
        private List<TcpNetworkManager> _connections;

        private bool _disposedValue = false;
        private bool _gzip;
        private Queue<NetPacket> _incoming;
        private int _port;
        private bool _running;
        private TcpListener _server;
        private Thread _serverThread;

        public bool EnableGZip
        {
            get { return _gzip; }
            set { _gzip = value; }
        }

        public string Host
        {
            get { return "0.0.0.0"; }
        }

        public bool IsConnected
        {
            get { return _running; }
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

        public TcpNetworkManagerServer(int port)
        {
            Throw.IfValueNotInRange(port, ushort.MinValue, ushort.MaxValue, nameof(port));
            _port = port;
            _server = new TcpListener(IPAddress.Any, port);
            _serverThread = new Thread(Run)
            {
                IsBackground = true
            };

            _incoming = new Queue<NetPacket>();

            _connections = new List<TcpNetworkManager>();
        }

        ~TcpNetworkManagerServer()
        {
            Dispose(false);
        }

        public void Connect()
        {
            _running = true;
            _server.Start();
            _serverThread.Start();

            OnConnected?.Invoke(this, new ConnectionEventArgs(this));
        }

        public void Disconnect(string reason)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            _connections.ForEach(g => g.Disconnect());
            OnDisconnected?.Invoke(this, new ConnectionEventArgs(this));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public NetStream ReadMessage()
        {
            return Dequeue().Stream;
        }

        public NetStream ReadMessage(out INetworkManager origin)
        {
            var packet = Dequeue();
            origin = null;

            if (packet.Stream != null)
            {
                origin = packet.Origin;
                return packet.Stream;
            }

            return null;
        }

        public void WriteMessage(NetStream stream)
        {
            lock (_connections)
            {
                foreach (var client in _connections)
                {
                    client.WriteMessage(stream);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var connection in _connections)
                    {
                        connection.Disconnect();
                    }
                }
                _serverThread.Join(1000);
                _disposedValue = true;
            }
        }

        private void AcceptClient()
        {
            Socket client = _server.AcceptSocket();

            TcpNetworkManager connection = new TcpNetworkManager(client)
            {
                EnableGZip = EnableGZip
            };

            connection.OnDataReceived += DataReceived;
            connection.OnDisconnected += ClientDisconnected;
            connection.OnDataSent += DataSent;
            _connections.Add(connection);

            OnConnected(this, new ConnectionEventArgs(connection));
        }

        private void ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            _connections.Remove((TcpNetworkManager)e.NetworkManager);
            OnDisconnected?.Invoke(sender, e);

            ((IDisposable)e.NetworkManager).Dispose();
        }

        private void DataReceived(object sender, DataEventArgs e)
        {
            if (e.Data.Position != 0)
            {
                e.Data.Position = 0;
            }
            if (_incoming.Count > 64)
            {
                _incoming.Clear();
            }
            _incoming.Enqueue(new NetPacket(e.NetworkManager, new NetStream(e.Data)));
            OnDataReceived?.Invoke(sender, e);
        }

        private void DataSent(object sender, DataEventArgs e)
        {
            OnDataSent?.Invoke(sender, e);
        }

        private NetPacket Dequeue()
        {
            if (_incoming.Count > 0)
            {
                return _incoming.Dequeue();
            }

            return default(NetPacket);
        }

        private void Run()
        {
            while (_running)
            {
                Thread.Sleep(4);
                while (_server.Pending())
                {
                    AcceptClient();
                }
                foreach (var client in _connections.ToArray())
                {
                    try
                    {
                        if (client.Run().Ticks > TimeSpan.TicksPerSecond)
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        var stream = new NetStream();
                        stream.Write(e.ToString());
                        stream.Position = 0;
                        OnLidgrenMessage?.Invoke(this, new DataEventArgs(this, stream));
                        continue;
                    }
                }
            }
        }
    }
}