using System;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;

namespace Pantheon.Client.Extensions
{
    internal class DatagramCallback
    {
        private Datagram _datagram;
        private PacketRouter _router;
        private TimeSpan _timeout;

        public DatagramCallback(PacketRouter router, Datagram datagram, int timeout)
        {
            _router = router;
            _datagram = datagram;
            _timeout = new TimeSpan(0, 0, 0, 0, timeout);
        }

        public DatagramCallbackAwaiter GetAwaiter()
        {
            return new DatagramCallbackAwaiter(_router, _datagram, _timeout);
        }
    }
}