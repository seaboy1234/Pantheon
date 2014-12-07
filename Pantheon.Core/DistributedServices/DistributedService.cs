using System;
using System.Collections.Generic;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.DistributedServices;
using Pantheon.Core.Exceptions;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.DistributedServices
{
    public abstract class DistributedService<TServiceClient, TServiceServer> : IServiceClient, IServiceServer, IDisposable
        where TServiceClient : IServiceClient
        where TServiceServer : IServiceServer
    {
        private RemoteCallerInfo _caller;
        private ulong _channel;
        private TServiceClient _client;
        private Type _clientType;
        private Type _currentRole;
        private bool _disposedValue = false;
        private EventLogger _eventLog;
        private Dictionary<int, MethodInfo> _messageHandlers;
        private DistributedServiceProxy _proxy;
        private MessageRouter _router;
        private TServiceServer _server;
        private Type _serverType;

        public virtual bool AllowAnonymous
        {
            get { return false; }
        }

        public virtual bool AllowClients
        {
            get { return false; }
        }

        public RemoteCallerInfo Caller
        {
            get { return _caller; }
        }

        public TServiceClient Client
        {
            get { return _client; }
        }

        public EventLogger EventLog
        {
            get { return _eventLog; }
        }

        public IMessageDirector MessageDirector
        {
            get { return _router.MessageDirector; }
        }

        public MessageRouter MessageRouter
        {
            get { return _router; }
        }

        public virtual bool RegisterService
        {
            get { return true; }
        }

        public ulong RemoteChannel
        {
            get { return _proxy.Channel; }
            set { _proxy.Channel = value; }
        }

        public TServiceServer Server
        {
            get { return _server; }
        }

        public abstract string ServiceName { get; }

        protected DistributedService(MessageRouter router, ulong channel)
        {
            _router = router;
            _channel = channel;
            _messageHandlers = new Dictionary<int, MethodInfo>();
            _currentRole = GetType().GetInterfaces()[3];
            _clientType = typeof(TServiceClient);
            _serverType = typeof(TServiceServer);

            if (this is TServiceClient)
            {
                _proxy = new DistributedServiceProxy(typeof(TServiceServer), router, channel, Channel.GenerateCallback());
                // We cannot cast directly to TServiceClient.
                _client = (TServiceClient)((IServiceClient)this);
                _server = (TServiceServer)_proxy.GetTransparentProxy();
            }
            else if (_currentRole == typeof(TServiceServer))
            {
                _proxy = new DistributedServiceProxy(typeof(TServiceClient), router, 0, channel);

                _client = (TServiceClient)_proxy.GetTransparentProxy();
                _server = (TServiceServer)((IServiceServer)this);

                if (RegisterService)
                {
                    ServiceManager.RegisterService(new ServicePointer(ServiceName, null, channel,
                                                                      true, AllowClients, AllowAnonymous));
                }
            }
            else
            {
                const string message = "Invalid typeparam.  Did you forget to derive from TServiceClient or TServiceServer?";
                throw new PantheonServiceException(message);
            }
            _eventLog = ServiceManager.GetServiceHandle<EventLogger>(router);
            FindMessageHandlers();

            _router.RegisterRoute(HandleMessage, channel);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void FindServer()
        {
            var service = ServiceManager.QueryServiceAsync(MessageRouter, ServiceName).Result;
            if (service != null)
            {
                RemoteChannel = service.Channel;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _eventLog.Dispose();
                    _messageHandlers.Clear();
                    _router.DestroyRoute(g => g.Target == this);
                }

                _disposedValue = true;
            }
        }

        private void FindMessageHandlers()
        {
            foreach (var method in _currentRole.GetMethods())
            {
                var attr = method.GetAttribute<MessageCodeAttribute>();
                if (attr != null)
                {
                    _messageHandlers.Add(attr.MessageCode, method);
                }
            }
        }

        private void HandleMessage(Message message)
        {
            int action = message.ReadInt32();
            MethodInfo method;

            if (_messageHandlers.TryGetValue(action, out method))
            {
                var attr = method.GetAttribute<MessageCodeAttribute>();
                var parameters = method.GetParameters();

                object[] args = new object[parameters.Length];

                try
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = message.ReadObject(parameters[i].ParameterType);
                    }
                }
                catch (Exception e)
                {
                    _eventLog.LogCritical(e);
                    return;
                }

                while (_proxy.Channel != 0) ;
                object value;
                try
                {
                    _proxy.Channel = message.From;
                    _caller = new RemoteCallerInfo(message.From);
                    value = method.Invoke(this, args);
                }
                catch (Exception e)
                {
                    _eventLog.LogCritical(e);
                    return;
                }
                finally
                {
                    _proxy.Channel = 0;
                    _caller = default(RemoteCallerInfo);
                }

                if (value != null)
                {
                    Message reply = new Message();
                    reply.From = _channel;
                    reply.AddChannel(message.From);

                    if (attr.ResponseCode == (int)MessageCode.Invalid)
                    {
                        _eventLog.Log(LogLevel.Warning, "{0} does not specify return message code.", method.Name);
                        return;
                    }

                    if (Channel.IsGameChannel(message.From))
                    {
                        reply.Write(MessageCode.ClientAgent_SendDatagram);
                    }

                    reply.Write(attr.ResponseCode);
                    reply.WriteObject(value);

                    _router.MessageDirector.QueueSend(reply);
                }
            }
        }
    }
}