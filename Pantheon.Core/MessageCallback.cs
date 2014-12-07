using System;
using System.Runtime.CompilerServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core
{
    public class MessageCallback
    {
        private ulong _callback;
        private MessageCode _expected;
        private Message _request;
        private Message _response;
        private MessageRouter _router;
        private DateTime _timeout;

        public Message Response
        {
            get { return _response; }
            set { _response = value; }
        }

        internal MessageCode Expected
        {
            get { return _expected; }
        }

        public MessageCallback(MessageRouter router, Message message, int timeout)
        {
            _timeout = DateTime.Now + new TimeSpan(0, 0, 0, 0, timeout);
            _callback = message.From;
            _request = message;
            _router = router;
        }

        public class MessageCallbackAwaiter : INotifyCompletion
        {
            private ulong _channel;
            private MessageCallback _message;
            private Message _response;
            private MessageRouter _router;

            public bool IsCompleted
            {
                get { return _response != null || DateTime.Now > _message._timeout; }
            }

            public MessageCallbackAwaiter(MessageRouter router, MessageCallback callback, ulong channel)
            {
                _router = router;
                _message = callback;
                _channel = channel;
                _response = null;

                router.RegisterRoute(OnReceive, channel);
                router.MessageDirector.QueueSend(callback._request);
            }

            public MessageCallbackAwaiter GetAwaiter()
            {
                return this;
            }

            public Message GetResult()
            {
                return _response;
            }

            public void OnCompleted(Action continuation)
            {
                while (!IsCompleted) ;
                _router.DestroyRoute(m => m.Target == this);
                if (continuation != null)
                {
                    continuation();
                }
            }

            private void OnReceive(Message message)
            {
                if (_message._expected > 0)
                {
                    var code = message.ReadMessageCode();
                    message.Position -= 4;
                    if (code != _message._expected)
                    {
                        return;
                    }
                }
                _message.Response = message;
                _response = message;
            }
        }

        public MessageCallback Expect(MessageCode expectedHeader)
        {
            _expected = expectedHeader;
            return this;
        }

        public MessageCallbackAwaiter GetAwaiter()
        {
            var awaiter = new MessageCallbackAwaiter(_router, this, _callback);

            return awaiter;
        }
    }
}