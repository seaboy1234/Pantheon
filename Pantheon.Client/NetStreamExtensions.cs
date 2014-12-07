using System;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Client
{
    public static class NetStreamExtensions
    {
        static NetStreamExtensions()
        {
            NetStream.AddDataHandler(ReadPacketId, Write);
        }

        public static ClientControlCode ReadPacketId(this NetStream stream)
        {
            return (ClientControlCode)stream.ReadInt32();
        }

        public static void Write(this NetStream stream, ClientControlCode packetId)
        {
            Throw.IfEquals(packetId, ClientControlCode.Invalid, "packetId");
            stream.Write((int)packetId);
        }
    }
}