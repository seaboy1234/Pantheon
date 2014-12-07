using System;
using System.Threading.Tasks;
using Pantheon.Client.Packets;
using Pantheon.Client.Services;

namespace Pantheon.Client.Extensions
{
    public static class ServiceExtensions
    {
        public static async Task<Datagram> SendDatagramAsync(this Service service, Datagram message)
        {
            if (service.IsConnected)
            {
                message.Channel = service.Channel;
                return await message.SendAsync(service.PacketRouter, 4000);
            }
            return null;
        }
    }
}