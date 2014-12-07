using System;
using Pantheon.Common.IO;

namespace Pantheon.Client.Packets
{
    public class Datagram : NetStream, IPacket
    {
        public ulong Channel { get; set; }

        protected ClientControlCode MessageType
        {
            get { return ClientControlCode.Client_SendDatagram; }
        }

        public ClientControlCode GetMessageType()
        {
            return MessageType;
        }

        public void ReadFrom(NetStream stream)
        {
            Channel = stream.ReadUInt64();
            long length = stream.ReadInt64();
            Data = stream.Read((int)length);

            Read();
        }

        public void WriteTo(Common.INetworkManager networkManager)
        {
            Write();

            NetStream stream = new NetStream();
            stream.Write(MessageType);
            stream.Write(Channel);
            stream.Write(Length);
            stream.Write(Data);

            networkManager.WriteMessage(stream);
        }

        protected virtual void Read()
        {
        }

        protected virtual void Write()
        {
        }
    }
}