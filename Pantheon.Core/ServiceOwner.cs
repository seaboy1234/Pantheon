using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.Exceptions;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public class ServiceOwner : IPantheonService, IDisposable
    {
        private bool _allowAnonymous;
        private ulong _channel;
        private Dictionary<MessageCode, MethodInfo> _endpoints;
        private EventLogger _eventLog;
        private Dictionary<MessageCode, Action<Message>> _handlers;
        private MessageRouter _router;
        private string _serviceName;

        public bool AllowAnonymous
        {
            get { return _allowAnonymous; }
        }

        /// <summary>
        ///   Gets this <see cref="ServiceOwner" /> 's <see cref="IMessageDirector" /> .
        /// </summary>
        public IMessageDirector MessageDirector
        {
            get { return _router.MessageDirector; }
        }

        /// <summary>
        ///   Gets this <see cref="ServiceOwner" /> 's
        ///   <see cref="Pantheon.Core.MessageRouting.MessageRouter" /> .
        /// </summary>
        public MessageRouter MessageRouter
        {
            get { return _router; }
        }

        public string Name
        {
            get { return _serviceName; }
        }

        public ulong ServiceChannel
        {
            get { return _channel; }
        }

        /// <summary>
        ///   Gets a value indicating whether to register this service with the
        ///   <see cref="ServiceManager" /> .
        /// </summary>
        protected virtual bool AutoRegister
        {
            get { return true; }
        }

        /// <summary>
        ///   Gets the <see cref="EventLogger" /> for this component.
        /// </summary>
        protected EventLogger EventLog
        {
            get { return _eventLog; }
        }

        protected ServiceOwner(MessageRouter router,
                               string service,
                               bool allowAnonymous,
                               bool allowClients,
                               ulong channel)
        {
            _router = router;
            _allowAnonymous = allowAnonymous;
            _serviceName = service;
            _channel = channel;
            _handlers = new Dictionary<MessageCode, Action<Message>>();
            _endpoints = new Dictionary<MessageCode, MethodInfo>();
            _eventLog = ServiceManager.GetServiceHandle<EventLogger>(router);

            _router.RegisterRoute(RouteMessage, channel);
            MakeMessageEndpoints();

            var pointer = new ServicePointer(service, "", channel, true, allowClients, allowAnonymous);
            if (AutoRegister)
            {
                ServiceManager.RegisterService(pointer);
            }
        }

        protected void AddChannel(ulong channel)
        {
            _router.RegisterRoute(RouteMessage, channel);
        }

        protected void RegisterMessageRoute(MessageCode action, Action<Message> handler)
        {
            _handlers.Add(action, handler);
        }

        protected void RegisterMessageRoute(int action, Action<Message> handler)
        {
            RegisterMessageRoute((MessageCode)action, handler);
        }

        protected void SendMessage(Message message)
        {
            if (message.Channels.Length == 0)
            {
                message.Channels = new[] { _channel };
            }
            if (message.From == 0)
            {
                message.From = _channel;
            }

            _router.MessageDirector.QueueSend(message);
        }

        private void HandleRoutedMessage(Message message)
        {
            message.Position -= 4;
            var action = message.ReadMessageCode();
            MethodInfo method;
            if (_endpoints.TryGetValue(action, out method))
            {
                bool ret = method.GetAttribute<EndpointAttribute>().RequireReturnPath;
                var parameters = method.GetParameters().Where(p => !p.IsOut);
                object[] args = new object[parameters.Count()];
                for (int i = ret ? 1 : 0; i < args.Length; i++)
                {
                    args[i] = message.ReadObject(parameters.ElementAt(i).ParameterType);
                }
                if (ret)
                {
                    args[0] = message.From;
                }
                var value = method.Invoke(this, args);
                if (value == null)
                {
                    return;
                }

                Message reply = null;

                if (value is Message)
                {
                    reply = (Message)value;
                    if (reply.Channels.Length == 0)
                    {
                        reply.AddChannel(message.From);
                    }
                }
                else if (method.HasAttribute<ReplyWithAttribute>())
                {
                    var attribute = method.GetAttribute<ReplyWithAttribute>();
                    reply = new Message();
                    reply.AddChannel(message.From);
                    reply.Write(attribute.Code);

                    WriteReturnValue(value, attribute, reply);
                }
                else
                {
                    reply = new Message();
                    reply.AddChannel(message.From);

                    if (!reply.WriteObject(value))
                    {
                        var error = string.Format("Unable to write object value {0} returned from {1}.  \n"
                                                + "Consider adding a ReplyWithAttribute to your method.",
                                                  value, method.MethodSignature());
                        throw new PantheonException(error);
                    }
                }

                SendMessage(reply);
            }
        }

        private void MakeMessageEndpoints()
        {
            Type type = GetType();

            foreach (var method in type.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Instance |
                                                   BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.HasAttribute<EndpointAttribute>())
                {
                    var attr = method.GetAttribute<EndpointAttribute>();
                    _endpoints.Add((MessageCode)attr.CallingCode, method);
                    RegisterMessageRoute(attr.CallingCode, HandleRoutedMessage);
                }
            }
        }

        private void RouteMessage(Message message)
        {
            MessageCode action = message.ReadMessageCode();
            Action<Message> method;

            if (action == MessageCode.ALL_QueryChannel)
            {
                Message reply = new Message();
                reply.Channels = new[] { message.From };

                reply.Write(Name);

                SendMessage(reply);
                return;
            }

            if (_handlers.TryGetValue(action, out method))
            {
                method(message);
            }
            else
            {
                _eventLog.Log(LogLevel.Warning, "Message from {0} had invalid action.", message.From);
            }
        }

        private void WriteReturnValue(object value, ReplyWithAttribute attribute, Message message)
        {
            if (attribute.Serialization == Serialization.WriteObject)
            {
                message.WriteObject(value);
            }
            else if (attribute.Serialization == Serialization.WriteProperties)
            {
                message.WriteProperties(value);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _eventLog.Dispose();
                    _router.DestroyRoute(m => m.Target == this);
                    _endpoints.Clear();
                    _handlers.Clear();
                    _router = null;
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}