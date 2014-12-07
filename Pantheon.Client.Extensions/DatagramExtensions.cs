using System;
using System.Threading.Tasks;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Packets;

namespace Pantheon.Client.Extensions
{
    public static class DatagramExtensions
    {
        public static async Task<Datagram> SendAsync(this Datagram datagram, PacketRouter router)
        {
            return await SendAsync(datagram, router, 2000);
        }

        public static async Task<Datagram> SendAsync(this Datagram datagram, PacketRouter router, int timeout)
        {
            return await new DatagramCallback(router, datagram, timeout);
        }
    }
}