using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Common.TCP
{
    internal class TcpSocket : IDisposable
    {
        private bool _disposedValue = false;
        private IPEndPoint _endpoint;
        private Socket _socket;

        public bool DataIsAvailable
        {
            get { return _socket.Available > 4; }
        }

        public string Host
        {
            get
            {
                Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
                return _endpoint?.Address.ToString() ?? string.Empty;
            }
            set
            {
                Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
                Throw.If(IsConnected, "Connected", nameof(TcpSocket));
                var address = Dns.GetHostAddresses(value).FirstOrDefault();
                if (_endpoint == null)
                {
                    _endpoint = new IPEndPoint(address, 0);
                }
                _endpoint.Address = address;
            }
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    return !(!_socket.Connected || (_socket.Poll(1000, SelectMode.SelectRead) && (_socket.Available == 0)));
                }
                catch
                {
                    return false;
                }
            }
        }

        public int Port
        {
            get
            {
                Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
                return _endpoint?.Port ?? -1;
            }
            set
            {
                Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
                Throw.If(IsConnected, "Connected", nameof(TcpSocket));
                if (_endpoint == null)
                {
                    _endpoint = new IPEndPoint(IPAddress.Any, value);
                }
                _endpoint.Port = value;
            }
        }

        public TcpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public TcpSocket(Socket socket)
        {
            Throw.IfNull(socket, nameof(socket));

            _socket = socket;
            if (IsConnected)
            {
                _endpoint = (IPEndPoint)_socket.RemoteEndPoint;
            }
        }

        ~TcpSocket()
        {
            Dispose(false);
        }

        public void Connect(string host, int port)
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
            Throw.If(IsConnected, "Connected", nameof(TcpSocket));
            Throw.IfEmpty(host, nameof(host));
            Throw.IfValueNotInRange(port, ushort.MinValue, ushort.MaxValue, nameof(port));

            _socket.Connect(host, port);
            _endpoint = (IPEndPoint)_socket.RemoteEndPoint;
        }

        public void Connect()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
            Throw.If(_endpoint == null, "No host and port", nameof(TcpSocket));
            Throw.If(IsConnected, "Connected", nameof(TcpSocket));

            _socket.Connect(_endpoint);
        }

        public void Disconnect()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
            Throw.If(!IsConnected, "Connected", nameof(TcpSocket));

            _socket.Disconnect(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public NetStream Read()
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
            Throw.If(!IsConnected, "Connected", nameof(TcpSocket));

            ushort length = ReadLength();
            byte[] data = new byte[length];

            _socket.Receive(data);

            NetStream stream = new NetStream(data);
            stream.Position = 0;
            return stream;
        }

        public bool TryWrite(NetStream stream)
        {
            try
            {
                Write(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Write(NetStream stream)
        {
            Throw.IfDisposed(_disposedValue, nameof(TcpSocket));
            Throw.If(!IsConnected, "Connected", nameof(TcpSocket));

            if (stream.Length > ushort.MaxValue)
            {
                throw new ArgumentException("cannot send packet larger than 64KB.", nameof(stream));
            }

            _socket.Send(GetBytes((ushort)stream.Length));
            _socket.Send(stream.Data);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                _socket.Close(1);

                _disposedValue = true;
            }
        }

        private byte[] GetBytes(ushort value)
        {
            var data = BitConverter.GetBytes(value);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }

        private ushort GetUInt16(byte[] data)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToUInt16(data, 0);
        }

        private ushort ReadLength()
        {
            ushort length = 0;
            byte[] data = new byte[sizeof(ushort)];

            try
            {
                _socket.Receive(data);
            }
            catch (SocketException)
            {
                return 0;
            }
            length = GetUInt16(data);
            return length;
        }
    }
}