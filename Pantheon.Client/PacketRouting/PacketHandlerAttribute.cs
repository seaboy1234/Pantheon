using System;

namespace Pantheon.Client.PacketRouting
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PacketHandlerAttribute : Attribute
    {
        private ClientControlCode _packetId;

        public ClientControlCode PacketId
        {
            get { return _packetId; }
        }

        public PacketHandlerAttribute(ClientControlCode packetId)
        {
            _packetId = packetId;
        }
    }
}