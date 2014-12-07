using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;
using Pantheon.Common;
using Pantheon.Common.DistributedServices;

namespace Pantheon.Client.Services
{
    public class ServiceProxy2 : RealProxy, IDisposable
    {
        private ulong _channel;
        private PacketRouter _router;

        public ServiceProxy2(Type classToProxy, PacketRouter router, ulong channel)
            : base(classToProxy)
        {
            _channel = channel;
            _router = router;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodCall = (IMethodCallMessage)msg;
            try
            {
                MethodInfo method = (MethodInfo)methodCall.MethodBase;

                if (disposedValue)
                {
                    return new ReturnMessage(new ObjectDisposedException(GetProxiedType().Name), methodCall);
                }

                var attr = method.GetAttribute<MessageCodeAttribute>();
                Console.WriteLine("Calling {0}", method.MethodSignature());
                Datagram message = new Datagram();
                message.Channel = _channel;

                message.Write(attr.MessageCode);

                int i = 0;
                foreach (var arg in methodCall.InArgs)
                {
                    if (!message.WriteObject(arg))
                    {
                        return new ReturnMessage(new ArgumentNullException(methodCall.GetArgName(i)), methodCall);
                    }
                    i++;
                }

                if (method.ReturnType == typeof(void))
                {
                    message.WriteTo(_router.NetworkManager);
                    return new ReturnMessage(null, null, 0, methodCall.LogicalCallContext, methodCall);
                }

                var reply = AwaitReply(message, attr.ResponseCode);
                object value = null;

                if (reply == null)
                {
                    if (method.ReturnType.IsValueType)
                    {
                        value = Activator.CreateInstance(method.ReturnType);
                    }
                    return new ReturnMessage(value, null, 0, methodCall.LogicalCallContext, methodCall);
                }

                try
                {
                    value = reply.ReadObject(method.ReturnType);
                }
                catch (Exception e)
                {
                    return new ReturnMessage(e, methodCall);
                }

                return new ReturnMessage(value, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception e)
            {
                return new ReturnMessage(e, methodCall);
            }
        }

        private Datagram AwaitReply(Datagram message, int replyCode)
        {
            message.WriteTo(_router.NetworkManager);
            Datagram replyMessage = null;
            _router.RegisterRoute((stream) =>
            {
                Datagram reply = new Datagram();
                if (reply.Channel != message.Channel)
                {
                    return;
                }
                reply.ReadFrom(stream);

                if (reply.ReadInt32() != replyCode)
                {
                    reply.Position -= sizeof(int);
                    return;
                }

                replyMessage = reply;
            }, ClientControlCode.Client_SendDatagram);
            int waiting = 0;
            while (replyMessage == null && waiting < 5000)
            {
                Thread.Sleep(25);
                waiting += 25;
            }

            return replyMessage;
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
                    _router.DestroyAll(this);
                    _router = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer
                //       below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~ServiceProxy2() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}