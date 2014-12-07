using System;

using Pantheon.Common.IO;

namespace Pantheon.Common.Event
{
    public class DataEventArgs : ConnectionEventArgs
    {
        private readonly NetStream _data;

        public NetStream Data { get { return _data; } }

        public DataEventArgs(INetworkManager networkManager, NetStream stream)
            : base(networkManager)
        {
            _data = stream;
        }
    }
}