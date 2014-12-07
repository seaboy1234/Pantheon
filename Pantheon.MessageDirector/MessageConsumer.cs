using System;
using System.Collections.Generic;
using Pantheon.Common;
using Pantheon.Core;

namespace Pantheon.MessageDirector
{
    internal class MessageConsumer
    {
        private readonly INetworkManager _networkManager;
        private List<Message> _onDisconnect;

        public List<ulong> Channels { get; private set; }

        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        public event Action<IEnumerable<Message>> Disconnected = delegate { };

        public MessageConsumer(INetworkManager networkManager)
        {
            _onDisconnect = new List<Message>();
            _networkManager = networkManager;
            _networkManager.OnDisconnected += (sender, e) => Disconnected(_onDisconnect);
            Channels = new List<ulong>();
        }

        public IEnumerable<Message> GetDisconnectMessages()
        {
            var messages = _onDisconnect.ToArray();
            _onDisconnect.Clear();
            return messages;
        }

        public bool ShouldReceiveMessage(MDServer.SystemMessage message)
        {
            if (message.Sender == _networkManager)
            {
                return false;
            }
            foreach (ulong channel in message.Message.Channels)
            {
                if (Channels.Contains(channel))
                {
                    return true;
                }
            }
            return false;
        }

        internal void QueueOnDisconnect(Message msg)
        {
            _onDisconnect.Add(msg);
        }
    }
}