using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading.Tasks;
using Pantheon.Common;
using Pantheon.Common.Exceptions;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.DistributedServices
{
    public class ServiceProxy : RealProxy
    {
        private static readonly BindingFlags _AnyMethod = BindingFlags.InvokeMethod |
                                                          BindingFlags.Instance |
                                                          BindingFlags.Public |
                                                          BindingFlags.NonPublic;

        private List<Message> _backlogMessages;
        private Func<MessageCode, Message, Message> _beforeSendMethod;
        private ServiceClient _instance;

        public bool IsSendingDisabled { get; set; }

        public ServiceProxy(ServiceClient service)
            : base(service.GetType())
        {
            _backlogMessages = new List<Message>();

            _instance = service;
            _instance.Proxy = this;

            var method = _instance.GetType().GetMethod("OnBeforeSend", _AnyMethod);

            if (method != null)
            {
                try
                {
                    _beforeSendMethod =
                        Delegate.CreateDelegate(typeof(Func<MessageCode, Message, Message>),
                        _instance, method) as Func<MessageCode, Message, Message>;
                }
                catch (Exception e)
                {
                    var message = "Invalid OnBeforeSend method format.  \n";
                    message += "Use Message OnBeforeSend(MessageCode, Message) signature.";
                    throw new PantheonException(message, e);
                }
            }
        }

        public static T Create<T>(MessageRouter router) where T : ServiceClient
        {
            var type = typeof(T);
            var cotr = type.GetConstructor(new[] { typeof(MessageRouter) });
            if (cotr == null)
            {
                const string message = "Type must implement the standard service constructor pattern.";
                throw new ArgumentException(message, "T");
            }
            T obj = (T)cotr.Invoke(new[] { router });

            return Create(obj);
        }

        public static T Create<T>(T client) where T : ServiceClient
        {
            return (T)new ServiceProxy(client).GetTransparentProxy();
        }

        public IEnumerable<Message> GetMessageBacklog()
        {
            IsSendingDisabled = false;

            var messages = _backlogMessages.ToArray();
            _backlogMessages.Clear();
            return messages;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodCall = (IMethodCallMessage)msg;
            MethodInfo method = (MethodInfo)methodCall.MethodBase;
            RemoteMethodAttribute attr = method.GetAttribute<RemoteMethodAttribute>();

            if (attr != null)
            {
                if (!_instance.IsConnected)
                {
                    _instance.DiscoverServer();
                }

                Message message = GetMessageFor(attr, methodCall.InArgs);

                if (message == null)
                {
                    var exception = new PantheonException("Method call failed.  Check the local console for details.");
                    return new ReturnMessage(exception, methodCall);
                }

                message = OnBeforeSend(attr, message);

                object value = method.Invoke(_instance, methodCall.Args);

                if (IsSendingDisabled)
                {
                    _backlogMessages.Add(message);
                    return new ReturnMessage(value, null, 0,
                         methodCall.LogicalCallContext,
                         methodCall);
                }

                if (method.ReturnType == typeof(void) || !attr.AwaitResponse)
                {
                    _instance.MessageDirector.QueueSend(message);
                    return new ReturnMessage(value,
                                             null,
                                             0,
                                             methodCall.LogicalCallContext,
                                             methodCall);
                }

                Message reply = Task.Run<Message>(() => message.AwaitReply(_instance.MessageRouter)).Result;

                if (reply == null)
                {
                    return new ReturnMessage(value, null, 0,
                                             methodCall.LogicalCallContext,
                                             methodCall);
                }

                if (attr.ReplySkipMessageCode)
                {
                    reply.Position += sizeof(int);
                }

                var readerMethod = _instance.GetType().GetMethod(method.Name + "Resp", _AnyMethod);
                object returnObj;

                if (method.ReturnType == typeof(Message))
                {
                    returnObj = reply;
                }
                else if (readerMethod == null)
                {
                    returnObj = reply.ReadObject(method.ReturnType);
                }
                else
                {
                    returnObj = readerMethod.Invoke(_instance, new[] { reply });
                }
                return new ReturnMessage(returnObj, null, 0, methodCall.LogicalCallContext, methodCall);
            }

            var result = method.Invoke(_instance, methodCall.InArgs);
            return new ReturnMessage(result,
                                     null,
                                     0,
                                     methodCall.LogicalCallContext,
                                     methodCall);
        }

        private Message GetMessageFor(RemoteMethodAttribute attr, params object[] args)
        {
            Message message = new Message();
            message.From = Channel.GenerateCallback();
            message.Channels = new[] { _instance.ServiceChannel };
            message.Write(attr.CallingCode);
            try
            {
                foreach (var arg in args)
                {
                    message.WriteObject(arg);
                }
            }
            catch (TargetInvocationException e)
            {
                Console.WriteLine(e.InnerException);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return message;
        }

        private Message OnBeforeSend(RemoteMethodAttribute attr, Message message)
        {
            if (_beforeSendMethod != null)
            {
                var newMessage = _beforeSendMethod(attr.CallingCode, message);
                if (newMessage == null)
                {
                    const string err = "OnBeforeSend method must not return null!";
                    throw new ArgumentNullException(err, "return");
                }
                message = newMessage;
            }
            return message;
        }
    }
}