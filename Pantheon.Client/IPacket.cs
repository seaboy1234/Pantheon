using System;
using Pantheon.Common;
using Pantheon.Common.IO;

namespace Pantheon.Client
{
    public interface IPacket
    {
        ClientControlCode GetMessageType();

        void ReadFrom(NetStream stream);

        void WriteTo(INetworkManager networkManager);
    }
}