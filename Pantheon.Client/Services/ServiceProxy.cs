using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Pantheon.Client.Packets;
using Pantheon.Common;

namespace Pantheon.Client.Services
{
    public class ServiceProxy : RealProxy
    {
        private Service _instance;

        public ServiceProxy(Service instance)
            : base(instance.GetType())
        {
            _instance = instance;
        }

        public static T CreateProxy<T>(T service) where T : Service
        {
            return (T)new ServiceProxy(service).GetTransparentProxy();
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage call = (IMethodCallMessage)msg;
            MethodInfo info = (MethodInfo)call.MethodBase;

            if (info.HasAttribute<RpcAttribute>())
            {
                var attribute = info.GetAttribute<RpcAttribute>();
                int action = attribute.CallingCode;

                Datagram message = new Datagram();
                message.Channel = _instance.Channel;
                message.Write(action);
                foreach (var arg in call.InArgs)
                {
                    message.WriteObject(arg);
                }

                if (info.ReturnType == typeof(void))
                {
                    message.WriteTo(_instance.PacketRouter.NetworkManager);
                    info.Invoke(_instance, call.Args);
                    return new ReturnMessage(null, null, 0, call.LogicalCallContext, call);
                }
                Datagram replyMessage = null;
                _instance.PacketRouter.RegisterRoute((stream) =>
                {
                    Datagram reply = new Datagram();
                    if (reply.Channel != message.Channel)
                    {
                        return;
                    }
                    reply.ReadFrom(stream);

                    replyMessage = reply;
                }, ClientControlCode.Client_SendDatagram);
                int waiting = 0;
                while (replyMessage == null && waiting < 5000)
                {
                    Thread.Sleep(25);
                    waiting += 25;
                }
                object ret;

                if (replyMessage == null)
                {
                    ret = info.Invoke(_instance, call.Args);
                    return new ReturnMessage(ret, null, 0, call.LogicalCallContext, call);
                }

                var reader = _instance.GetType().GetMethod(info.Name + "Resp",
                                    BindingFlags.Instance | BindingFlags.NonPublic |
                                    BindingFlags.Public);
                if (reader != null)
                {
                    ret = reader.Invoke(_instance, new[] { replyMessage });
                }
                else
                {
                    ret = replyMessage.ReadObject(info.ReturnType);
                }
                return new ReturnMessage(ret, null, 0, call.LogicalCallContext, call);
            }
            object value = info.Invoke(_instance, call.Args);
            return new ReturnMessage(value, null, 0, call.LogicalCallContext, call);
        }
    }
}