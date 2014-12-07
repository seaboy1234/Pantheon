using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common;
using Pantheon.Common.IO;

namespace Pantheon.Core
{
    public class MessageDirectorClient : IMessageDirector
    {
        private Queue<Message> _incoming;
        private DateTime _lastPump;
        private List<Message> _outgoing;

        public DateTime LastPump
        {
            get { return _lastPump; }
        }

        public INetworkManager NetworkManager { get; private set; }

        public IEnumerable<Message> QueuedMessagesRecv
        {
            get { return _incoming.AsEnumerable(); }
        }

        public IEnumerable<Message> QueuedMessagesSend
        {
            get { return _outgoing.AsEnumerable(); }
        }

        public event EventHandler OnMessagesAvailable = delegate { };

        public event EventHandler OnPump = delegate { };

        public MessageDirectorClient(INetworkManager networkManager)
        {
            _outgoing = new List<Message>();
            _incoming = new Queue<Message>();
            NetworkManager = networkManager;
            NetworkManager.EnableGZip = true;
        }

        public void AddInterest(ulong channel)
        {
            Message message = new Message();

            message.From = 0;
            message.Channels = new[] { Channel.MessageDirector };
            message.Write(MessageCode.MessageDirector_AddInterest);
            message.Write(channel);

            QueueSend(message);
        }

        public void AddInterest(ulong low, ulong high)
        {
            Message message = new Message();
            message.From = 0;
            message.Channels = new[] { Channel.MessageDirector };

            message.Write(MessageCode.MessageDirector_AddInterestRange);
            message.Write(low);
            message.Write(high);

            QueueSend(message);
        }

        public void Pump()
        {
            IEnumerable<Message> messages;

            lock (_incoming)
            {
                NetStream stream;
                while (NetworkManager.IsConnected && (stream = NetworkManager.ReadMessage()) != null)
                {
                    int count = stream.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var message = new Message(stream);
                        _incoming.Enqueue(message);
                    }
                }
            }

            lock (_outgoing)
            {
                messages = _outgoing.ToArray().Where(m => m != null);
            }
            _outgoing.Clear();

            if (messages.Count() > 0)
            {
                using (var stream = new NetStream())
                {
                    stream.Write(messages.Count());
                    foreach (var message in messages)
                    {
                        message.WriteTo(stream);
                    }

                    NetworkManager.WriteMessage(stream);
                }
            }
            if (_incoming.Count > 0)
            {
                OnMessagesAvailable(this, EventArgs.Empty);
            }
            _lastPump = DateTime.Now;

            OnPump(this, EventArgs.Empty);

            //if (messages.Count() > 0)
            //{
            //    Console.WriteLine("Processed {0} messages.", messages.Count());
            //}
        }

        public void QueueOnDisconnect(Message message)
        {
            var msg = new Message();
            msg.From = 0;
            msg.Channels = new[] { Channel.MessageDirector };
            msg.Write(MessageCode.MessageDirector_QueueOnDisconnect);
            message.WriteTo(msg);
            QueueSend(msg);
        }

        public void QueueSend(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            lock (_outgoing)
            {
#if DEBUG
                if (message.Channels.Length == 1 && message.Channels[0] == 0)
                {
                    Console.WriteLine("...");
                }
#endif
                _outgoing.Add(message);
            }
        }

        public void QueueSend(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                QueueSend(message);
            }
        }

        public Message Read()
        {
            if (_incoming.Count > 0)
            {
                return _incoming.Dequeue();
            }
            return null;
        }

        public IEnumerable<Message> Read(int count)
        {
            int current = 0;
            Message message;
            List<Message> messages = new List<Message>();
            while (current++ < count && (message = Read()) != null)
            {
                messages.Add(message);
            }
            return messages;
        }

        public IEnumerable<Message> ReadAll()
        {
            IEnumerable<Message> messages = Read(_incoming.Count);
            return messages;
        }

        public void RemoveInterest(ulong channel)
        {
            Message message = new Message();

            message.From = 0;
            message.Channels = new[] { Channel.MessageDirector };
            message.Write(MessageCode.MessageDirector_RemoveInterest);
            message.Write(channel);

            QueueSend(message);
        }

        public void RemoveInterest(ulong low, ulong high)
        {
            Message message = new Message();
            message.From = 0;
            message.Channels = new[] { Channel.MessageDirector };

            message.Write(MessageCode.MessageDirector_RemoveInterestRange);
            message.Write(low);
            message.Write(high);

            QueueSend(message);
        }
    }
}