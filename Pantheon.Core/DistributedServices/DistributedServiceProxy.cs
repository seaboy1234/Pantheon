using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Pantheon.Common;
using Pantheon.Common.DistributedServices;
using Pantheon.Common.Exceptions;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.DistributedServices
{
    public class DistributedServiceProxy : RealProxy
    {
        private ulong _channel;
        private ulong _originChannel;

        private MessageRouter _router;
        private Type _type;

        public ulong Channel
        {
            get { return _channel; }
            set { _channel = value; }
        }

        public DistributedServiceProxy(Type serviceType, MessageRouter router, ulong channel, ulong originChannel)
            : base(serviceType)
        {
            _type = serviceType;
            _router = router;
            _channel = channel;
            _originChannel = originChannel;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodCall = (IMethodCallMessage)msg;
            MethodInfo method = (MethodInfo)methodCall.MethodBase;

            var attr = _type.GetMethod(method.Name).GetAttribute<MessageCodeAttribute>();

            Message message = new Message();
            message.From = _originChannel;
            message.AddChannel(_channel);

            message.Write(attr.MessageCode);

            foreach (var arg in methodCall.InArgs)
            {
                if (!message.WriteObject(arg))
                {
                    var exception = new PantheonException("No writer for " + arg.GetType().FullName);
                    return new ReturnMessage(exception, methodCall);
                }
            }
            if (method.ReturnType == typeof(void))
            {
                _router.MessageDirector.QueueSend(message);
                return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
            }

            var reply = message.AwaitReply(_router);

            if (reply == null)
            {
                return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
            }

            reply.Position += sizeof(int);

            var value = reply.ReadObject(method.ReturnType);
            return new ReturnMessage(value, null, 0, methodCall.LogicalCallContext, methodCall);
        }
    }
}