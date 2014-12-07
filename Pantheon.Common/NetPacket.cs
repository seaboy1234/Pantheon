using System;

using Pantheon.Common.IO;

namespace Pantheon.Common
{
    public struct NetPacket
    {
        private INetworkManager _origin;
        private NetStream _stream;

        public INetworkManager Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        public NetStream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

        public NetPacket(INetworkManager origin, NetStream stream)
        {
            _origin = origin;
            _stream = stream;
        }
    }
}