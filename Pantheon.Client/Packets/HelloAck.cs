using System;

namespace Pantheon.Client.Packets
{
    public class HelloAck : DataPacket
    {
        public bool EnableGZip { get; set; }

        protected override ClientControlCode MessageType
        {
            get { return ClientControlCode.Client_HelloResp; }
        }
    }
}