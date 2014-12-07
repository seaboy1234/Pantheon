using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;
using Pantheon.Common;
using Pantheon.Common.IO;

namespace Pantheon.Client.Services
{
    public abstract class Service : MarshalByRefObject, IService
    {
        private ulong _channel;
        private bool _closed;
        private Dictionary<int, MethodInfo> _endpoints;
        private INetworkManager _networkManager;
        private Dictionary<int, Action<Datagram>> _packetHandlers;
        private PacketRouter _packetRouter;

        public ulong Channel
        {
            get { return _channel; }
        }

        public bool IsConnected
        {
            get { return !_closed && _channel > 0; }
        }

        public abstract string Name { get; }

        public PacketRouter PacketRouter
        {
            get { return _packetRouter; }
        }

        protected Service(PacketRouter packetRouter)
        {
            _networkManager = packetRouter.NetworkManager;
            _packetRouter = packetRouter;
            _packetHandlers = new Dictionary<int, Action<Datagram>>();
            _endpoints = new Dictionary<int, MethodInfo>();

            RegisterEndpoints();
        }

        public void CloseServiceChannel()
        {
            if (_channel > 0)
            {
                NetStream stream = new NetStream();
                stream.Write(ClientControlCode.Client_CloseService);
                stream.Write(_channel);
                _networkManager.WriteMessage(stream);

                _packetRouter.DestroyAll(this);
            }
            _closed = true;
        }

        public bool Discover()
        {
            NetStream message = new NetStream();
            message.Write(ClientControlCode.Client_DiscoverService);
            message.Write(Name);
            DateTime timeout = DateTime.Now + TimeSpan.FromMilliseconds(5000);

            _packetRouter.RegisterRoute(DiscoverServiceResp, ClientControlCode.Client_DiscoverServiceResp);
            _networkManager.WriteMessage(message);

            while (_channel == 0 && !_closed && DateTime.Now < timeout) ;

            if (_channel != 0)
            {
                _packetRouter.RegisterRoute(HandleDatagram, ClientControlCode.Client_SendDatagram);
            }

            return _channel == 0;
        }

        protected void AddDatagramHandler(int action, Action<Datagram> handler)
        {
            _packetHandlers.Add(action, handler);
        }

        protected void SendDatagram(Datagram message)
        {
            if (IsConnected)
            {
                message.Channel = _channel;
                message.WriteTo(_networkManager);
            }
        }

        private void DiscoverServiceResp(NetStream stream)
        {
            string name = stream.ReadString();
            if (name == Name)
            {
                _channel = stream.ReadUInt64();
            }
            _packetRouter.DestroyRoute<NetStream>(DiscoverServiceResp);
        }

        private void HandleDatagram(NetStream stream)
        {
            Datagram datagram = new Datagram();
            datagram.ReadFrom(stream);

            int action = datagram.ReadInt32();
            Action<Datagram> handler;

            if (_packetHandlers.TryGetValue(action, out handler))
            {
                handler(datagram);
            }
        }

        private void RegisterEndpoints()
        {
            var type = GetType();
            foreach (var method in type.GetMethods().Where(m => m.HasAttribute<EndpointAttribute>()))
            {
                var endpoint = method.GetAttribute<EndpointAttribute>();
                AddDatagramHandler(endpoint.ControlCode, RouteMessage);
                _endpoints.Add(endpoint.ControlCode, method);
            }
        }

        private void RouteMessage(Datagram message)
        {
            message.Position -= 4;
            int action = message.ReadInt32();
            MethodInfo method;
            if (!_endpoints.TryGetValue(action, out method))
            {
                throw new ArgumentException("invalid action " + action, "action");
            }
            var parameters = method.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = message.ReadObject(parameters[i].ParameterType);
            }
            method.Invoke(this, args);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~Service() { // Do not change this code. Put cleanup code in
        // Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}