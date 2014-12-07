using System;
using System.Collections.Generic;
using Pantheon.Client.Packets;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Client.PacketRouting
{
    public class DatagramRouter
    {
        private PacketRouter _router;
        private Dictionary<int, Action<Datagram>> _routes;

        public DatagramRouter(PacketRouter router)
        {
            _router = router;
            _routes = new Dictionary<int, Action<Datagram>>();

            _router.RegisterRoute(OnIncomingDatagram, ClientControlCode.Client_SendDatagram);
        }

        public void DestroyRoute(int code)
        {
            _routes.Remove(code);
        }

        public void RegisterRoute(int code, Action<Datagram> handler)
        {
            const int InvalidPacketId = 0;

            Throw.IfValueNotGreaterThan(code, InvalidPacketId, nameof(code));
            Throw.IfNull(handler, nameof(handler));
            _routes.Add(code, handler);
        }

        public void RouteDatagram(Datagram message)
        {
            int code = message.ReadInt32();

            Action<Datagram> handler;
            if (_routes.TryGetValue(code, out handler))
            {
                handler(message);
            }
        }

        private void OnIncomingDatagram(NetStream stream)
        {
            Datagram message = new Datagram();
            message.ReadFrom(stream);

            RouteDatagram(message);
        }
    }
}