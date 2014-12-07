using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Pantheon.Common.Event;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common.TCP
{
    public class TcpNetworkManager : INetworkManager, IDisposable
    {
        private bool _disconnected;
        private bool _disposedValue = false;
        private bool _gzip;
        private Queue<NetStream> _incoming;
        private Queue<NetStream> _outgoing;
        private bool _queues;
        private TcpSocket _socket;
        private SynchronizationContext _sync;
        private Thread _thread;

        public bool EnableGZip
        {
            get { return _gzip; }
            set { _gzip = value; }
        }

        public string Host
        {
            get { return _socket.Host; }
        }

        public bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public int Port
        {
            get { return _socket.Port; }
        }

        public bool UseQueues
        {
            get { return _queues; }
            set { _queues = value; }
        }

        public event EventHandler<ConnectionEventArgs> OnConnected = delegate { };

        public event EventHandler<ConnectionEventArgs> OnConnectionStarted = delegate { };

        public event EventHandler<DataEventArgs> OnDataReceived = delegate { };

        public event EventHandler<DataEventArgs> OnDataSent = delegate { };

        public event EventHandler<ConnectionEventArgs> OnDisconnected = delegate { };

        public event EventHandler<DataEventArgs> OnLidgrenMessage = delegate { };

        public TcpNetworkManager(string host, int port)
            : this()
        {
            _socket = new TcpSocket();
            _socket.Host = host;
            _socket.Port = port;
        }

        public TcpNetworkManager(Socket socket)
            : this()
        {
            _socket = new TcpSocket(socket);
        }

        private TcpNetworkManager()
        {
            _incoming = new Queue<NetStream>();
            _outgoing = new Queue<NetStream>();

            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            }

            _sync = SynchronizationContext.Current;
            _queues = true;

            OnDisconnected += (s, g) =>
            {
                _disconnected = true;
                OnDisconnected = null;
            };
        }

        ~TcpNetworkManager()
        {
            Dispose(false);
        }

        public void Connect()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            Throw.If(_disconnected, "Disconnected", nameof(TcpNetworkManager));

            OnConnectionStarted?.Invoke(this, new ConnectionEventArgs(this));
            try
            {
                _socket.Connect();
            }
            catch (Exception e)
            {
                NetStream stream = new NetStream();
                stream.Write(e.ToString());

                stream.Position = 0;

                OnLidgrenMessage(this, new DataEventArgs(this, stream));
            }
            if (!IsConnected)
            {
                return;
            }

            OnConnected?.Invoke(this, new ConnectionEventArgs(this));

            _thread = new Thread(MessageLoop);
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Disconnect(string reason)
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            Disconnect();
        }

        public void Disconnect()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            _thread?.Join();
            _socket.Disconnect();

            OnDisconnected?.Invoke(this, new ConnectionEventArgs(this));
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public NetStream ReadMessage()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            Throw.If(_disconnected, "Disconnected", nameof(TcpNetworkManager));

            lock (_incoming)
            {
                if (_incoming.Count > 0)
                {
                    return _incoming.Dequeue();
                }
                return null;
            }
        }

        public NetStream ReadMessage(out INetworkManager origin)
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            Throw.If(_disconnected, "Disconnected", nameof(TcpNetworkManager));

            origin = this;
            return ReadMessage();
        }

        public void WriteMessage(NetStream stream)
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpNetworkManager));
            Throw.If(_disconnected, "Disconnected", nameof(TcpNetworkManager));

            lock (_outgoing)
            {
                _outgoing.Enqueue(stream);
            }
        }

        internal TimeSpan Run()
        {
            if (!IsConnected)
            {
                Post(() => OnDisconnected?.Invoke(this, new ConnectionEventArgs(this)));
                return default(TimeSpan);
            }

            NetStream stream = null;
            DateTime started = DateTime.Now;

            lock (_outgoing)
            {
                while (_outgoing.Count > 0)
                {
                    stream = _outgoing.Dequeue();
                    if (_gzip)
                    {
                        //GZip.Compress(stream);
                    }
                    _socket.TryWrite(stream);
                    Post(() => OnDataSent?.Invoke(this, new DataEventArgs(this, new NetStream(stream.Data) { Position = 0 })));
                }
            }

            while (_socket.DataIsAvailable)
            {
                stream = _socket.Read();

                if (_gzip)
                {
                    //GZip.Decompress(stream);
                }

                _incoming.Enqueue(stream);

                Post(() => OnDataReceived?.Invoke(this, new DataEventArgs(this, new NetStream(stream.Data) { Position = 0 })));
            }

            return DateTime.Now - started;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _socket.Dispose();
                }

                _incoming = null;
                _outgoing = null;

                OnConnected = null;
                OnConnectionStarted = null;
                OnDataReceived = null;
                OnDataSent = null;
                OnDisconnected = null;
                OnLidgrenMessage = null;

                _disposedValue = true;
            }
        }

        private void MessageLoop()
        {
            while (IsConnected)
            {
                Run();
            }
        }

        private void Post(Action action)
        {
            if (action != null)
            {
                _sync.Post(g => action(), this);
            }
        }
    }
}