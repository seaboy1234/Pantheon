using System;
using System.Runtime.CompilerServices;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;

namespace Pantheon.Client.Extensions
{
    public class DatagramCallbackAwaiter : INotifyCompletion
    {
        private ulong _channel;
        private Datagram _reply;
        private PacketRouter _router;
        private DateTime _timeout;

        public bool IsCompleted
        {
            get { return _reply != null || DateTime.Now > _timeout; }
        }

        public DatagramCallbackAwaiter(PacketRouter router,
                                       Datagram message,
                                       TimeSpan timeout)
        {
            _router = router;
            _channel = message.Channel;
            _timeout = DateTime.Now + timeout;

            _router.RegisterRoute<Datagram>(OnReceive, ClientControlCode.Client_SendDatagram);
            message.WriteTo(router.NetworkManager);
        }

        public Datagram GetResult()
        {
            return _reply;
        }

        public void OnCompleted(Action continuation)
        {
            while (!IsCompleted) ;
            if (continuation != null)
            {
                continuation();
            }
        }

        private void OnReceive(Datagram datagram)
        {
            if (datagram.Channel == _channel)
            {
                _reply = datagram;
                _router.DestroyRoute<Datagram>(OnReceive);
            }
        }
    }
}