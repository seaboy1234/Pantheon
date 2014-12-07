using System;
using System.Collections.Generic;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public abstract class ServiceClient : MarshalByRefObject, IPantheonService, IDisposable
    {
        private ulong _channel;
        private ServiceProxy _proxy;
        private MessageRouter _router;

        public bool HasProxy
        {
            get { return _proxy != null; }
        }

        public bool IsConnected
        {
            get { return _channel > 0; }
        }

        public IMessageDirector MessageDirector
        {
            get { return _router.MessageDirector; }
        }

        public MessageRouter MessageRouter
        {
            get { return _router; }
        }

        public abstract string Name { get; }

        public ServiceProxy Proxy
        {
            get
            {
                return _proxy;
            }
            internal set
            {
                if (HasProxy)
                {
                    throw new InvalidOperationException("Proxy is already set!");
                }
                _proxy = value;
            }
        }

        public ulong ServiceChannel
        {
            get { return _channel; }
            protected set { _channel = value; }
        }

        protected ServiceClient(MessageRouter router, ulong channel)
        {
            _router = router;
            _channel = channel;
        }

        public virtual bool DiscoverServer()
        {
            ulong channel;
            if (ServiceManager.ServiceExists(_router, Name, out channel))
            {
                _channel = channel;
                return true;
            }
            return false;
        }

        public IEnumerable<Message> OnDisconnect<T>(Action<T> action)
            where T : ServiceClient
        {
            Proxy.IsSendingDisabled = true;
            action((T)this);
            return Proxy.GetMessageBacklog();
        }

        protected void SendMessage(Message message)
        {
            if (message.Channels.Length == 0)
            {
                message.Channels = new[] { _channel };
            }
            _router.MessageDirector.QueueSend(message);
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
                    _router.DestroyRoute(g => g.Target == this);
                    _router = null;
                    _channel = 0;
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}