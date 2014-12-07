using System;

using Pantheon.Common.Event;
using Pantheon.Common.IO;

namespace Pantheon.Common
{
    public interface INetworkManager
    {
        bool EnableGZip { get; set; }

        string Host { get; }

        bool IsConnected { get; }

        int Port { get; }

        event EventHandler<ConnectionEventArgs> OnConnected;

        event EventHandler<ConnectionEventArgs> OnConnectionStarted;

        event EventHandler<DataEventArgs> OnDataReceived;

        event EventHandler<DataEventArgs> OnDataSent;

        event EventHandler<ConnectionEventArgs> OnDisconnected;

        event EventHandler<DataEventArgs> OnLidgrenMessage;

        void Connect();

        void Disconnect();

        void Disconnect(string reason);

        NetStream ReadMessage();

        NetStream ReadMessage(out INetworkManager origin);

        void WriteMessage(NetStream stream);
    }
}