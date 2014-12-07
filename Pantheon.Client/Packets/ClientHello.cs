using System;

namespace Pantheon.Client.Packets
{
    public class ClientHello : DataPacket
    {
        public int DistributedHash { get; set; }

        public string Version { get; set; }

        protected override ClientControlCode MessageType
        {
            get { return ClientControlCode.Client_Hello; }
        }
    }
}