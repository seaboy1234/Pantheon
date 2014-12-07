using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;
using Pantheon.Common;
using Pantheon.Common.DistributedServices;
using Pantheon.Common.Exceptions;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Client.Services
{
    public abstract class DistributedService<TServer, TClient> : IService, IServiceServer, IServiceClient, IDisposable
        where TServer : IServiceServer
        where TClient : IServiceClient
    {
        private ulong _channel;

        private TClient _client;

        private bool _closed;

        private ServiceProxy2 _proxy;

        private PacketRouter _router;

        private Dictionary<int, MethodInfo> _routes;

        private TServer _server;

        public string Name
        {
            get { return ServiceName; }
        }

        public PacketRouter PacketRouter
        {
            get { return _router; }
        }

        public TServer Server
        {
            get { return _server; }
        }

        public abstract string ServiceName { get; }

        public DistributedService(PacketRouter router)
        {
            Throw.IfNull(router, nameof(router));
            _router = router;

            FindServer();

            _routes = new Dictionary<int, MethodInfo>();
            _proxy = new ServiceProxy2(typeof(TServer), router, _channel);

            _server = (TServer)_proxy.GetTransparentProxy();
            _client = (TClient)((IServiceClient)this);

            FindMessageRoutes();
        }

        public void FindServer()
        {
            NetStream message = new NetStream();
            message.Write(ClientControlCode.Client_DiscoverService);
            try
            {
                message.Write(ServiceName);
            }
            catch (Exception)
            {
                throw;
            }
            DateTime timeout = DateTime.Now + TimeSpan.FromMilliseconds(5000);

            _router.RegisterRoute(DiscoverServiceResp, ClientControlCode.Client_DiscoverServiceResp);
            _router.NetworkManager.WriteMessage(message);

            while (_channel == 0 && !_closed && DateTime.Now < timeout) ;

            if (_channel != 0)
            {
                _router.RegisterRoute<Datagram>(HandleDatagram, ClientControlCode.Client_SendDatagram);
            }
        }

        private void DiscoverServiceResp(NetStream stream)
        {
            string name = stream.ReadString();
            if (name == Name)
            {
                _channel = stream.ReadUInt64();
            }
            _router.DestroyRoute<NetStream>(DiscoverServiceResp);
        }

        private void FindMessageRoutes()
        {
            var type = GetType().GetInterfaces().First(g => g.Is(typeof(IServiceClient)));

            foreach (var method in type.GetMethods())
            {
                var attr = method.GetAttribute<MessageCodeAttribute>();
                if (attr != null)
                {
                    _routes.Add(attr.MessageCode, method);
                }
            }
        }

        private void HandleDatagram(Datagram message)
        {
            if (message.Channel != _channel)
            {
                return;
            }

            int action = message.ReadInt32();
            MethodInfo method;

            if (_routes.TryGetValue(action, out method))
            {
                var attr = method.GetAttribute<MessageCodeAttribute>();
                var parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = message.ReadObject(parameters[i].ParameterType);
                }

                object value = method.Invoke(this, args);

                if (value == null)
                {
                    return;
                }

                if (attr.ResponseCode == 0)
                {
                    throw new PantheonException(method.MethodSignature() + " must have a response code.");
                }

                Datagram reply = new Datagram();
                reply.Channel = _channel;
                reply.Write(attr.ResponseCode);
                reply.WriteObject(value);

                _router.NetworkManager.WriteMessage(reply);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool
            // disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            //       GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _proxy.Dispose();
                    _router.DestroyAll(this);
                    _router = null;
                    _proxy = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // To detect redundant calls
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~ServiceManager() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}