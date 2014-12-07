using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.MessageDirector.Statistics;

namespace Pantheon.MessageDirector
{
    internal class MDServer : IMessageDirector
    {
        private List<MessageConsumer> _consumers;
        private Queue<Message> _incoming;
        private DateTime _lastPump;
        private List<SystemMessage> _outgoing;

        public DateTime LastPump
        {
            get { return _lastPump; }
        }

        public INetworkManager NetworkManager { get; private set; }

        public IEnumerable<Message> QueuedMessagesRecv
        {
            get { return _incoming.ToArray(); }
        }

        public IEnumerable<Message> QueuedMessagesSend
        {
            get { return _outgoing.Select(m => m.Message); }
        }

        public event EventHandler OnMessagesAvailable;

        public event EventHandler OnPump;

        public MDServer(INetworkManager networkManager)
        {
            NetworkManager = networkManager;
            NetworkManager.EnableGZip = true;
            _outgoing = new List<SystemMessage>();
            _incoming = new Queue<Message>();
            _consumers = new List<MessageConsumer>();
        }

        internal struct SystemMessage
        {
            private Message _message;
            private INetworkManager _sender;

            public Message Message
            {
                get { return _message; }
            }

            public INetworkManager Sender
            {
                get { return _sender; }
            }

            public SystemMessage(Message message, INetworkManager sender)
            {
                _message = message;
                _sender = sender;
            }
        }

        public void AddInterest(ulong channel)
        {
        }

        public void AddInterest(ulong low, ulong high)
        {
        }

        public void Pump()
        {
            if (!NetworkManager.IsConnected)
            {
                throw new InvalidOperationException("NetworkManager is not connected!");
            }
            IEnumerable<SystemMessage> messages;
            lock (_outgoing)
            {
                NetStream incoming;
                INetworkManager origin;
                int incomingTotal = 0;
                while ((incoming = NetworkManager.ReadMessage(out origin)) != null)
                {
                    if (incoming.Position != 0)
                    {
                        incoming.Position = 0;
                    }
                    if (_consumers.Find(c => c.NetworkManager == origin) == null
                        && origin != NetworkManager)
                    {
                        origin.EnableGZip = true;
                        var consumer = new MessageConsumer(origin);
                        _consumers.Add(consumer);
                        consumer.Disconnected += disconnectMessages =>
                        {
                            lock (_consumers)
                            {
                                _consumers.Remove(consumer);
                            }
                            foreach (var message in disconnectMessages)
                            {
                                _incoming.Enqueue(message);
                                _outgoing.Add(new SystemMessage(message, origin));
                            }
                        };
                    }
                    int count = incoming.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        Message message;
                        try
                        {
                            message = new Message(incoming);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            continue;
                        }
                        if (message.Channels.Contains(Channel.MessageDirector))
                        {
                            HandleMessage(origin, message);
                            message.Position = 0;
                        }

                        _incoming.Enqueue(message);
                        _outgoing.Add(new SystemMessage(message, origin));
                    }
                    incomingTotal += count;
                }
                //if (incomingTotal > 0)
                //{
                //    Console.WriteLine("Processed {0} incoming messages.", incomingTotal);
                //}
                long size = _incoming.Select(m => m?.Length ?? 0).Sum();

                _lastPump = DateTime.Now;
                OnPump(this, new PumpingEventArgs(size));

                lock (_outgoing)
                {
                    messages = _outgoing.ToArray();
                    _outgoing.Clear();
                }

                if (messages.Count() > 0)
                {
                    foreach (var consumer in _consumers.ToArray())
                    {
                        var updates = messages.Where(consumer.ShouldReceiveMessage).ToArray();
                        if (updates.Count() > 0)
                        {
                            using (var stream = new NetStream())
                            {
                                stream.Write(updates.Count());
                                foreach (var message in updates)
                                {
                                    message.Message.WriteTo(stream);
                                }

                                consumer.NetworkManager.WriteMessage(stream);
                            }
                        }
                    }
                    //Console.WriteLine("Processed {0} outgoing messages.", messages.Count());
                }
                if (_incoming.Count > 0)
                {
                    OnMessagesAvailable(this, EventArgs.Empty);
                }
            }
        }

        public void QueueOnDisconnect(Message message)
        {
        }

        public void QueueSend(Message message)
        {
            lock (_outgoing)
            {
                _outgoing.Add(new SystemMessage(message, null));
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
            while (++current < count && (message = Read()) != null)
            {
                messages.Add(message);
            }
            return messages;
        }

        public IEnumerable<Message> ReadAll()
        {
            IEnumerable<Message> messages = _incoming.ToArray();
            _incoming.Clear();
            return messages;
        }

        public void RemoveInterest(ulong channel)
        {
        }

        public void RemoveInterest(ulong low, ulong high)
        {
        }

        private void AddInterest(INetworkManager origin, ulong[] channels)
        {
            _consumers.Find(g => g.NetworkManager == origin).Channels.AddRange(channels);
        }

        private void AddInterest(INetworkManager networkManager, ulong low, ulong high)
        {
            MessageConsumer consumer = _consumers.Find(c => c.NetworkManager == networkManager) ??
                                        new MessageConsumer(networkManager);
            for (ulong channel = low; channel <= high; channel++)
            {
                if (!consumer.Channels.Contains(channel))
                {
                    consumer.Channels.Add(channel);
                }
            }
        }

        private void HandleMessage(INetworkManager origin, Message message)
        {
            MessageCode action = message.ReadMessageCode();

            switch (action)
            {
                case MessageCode.MessageDirector_AddInterest:
                    ulong channel = message.ReadUInt64();
                    AddInterest(origin, channel, channel);
                    break;

                case MessageCode.MessageDirector_AddInterestMultiple:
                    ulong[] channels = message.ReadValues<ulong>();
                    AddInterest(origin, channels);
                    break;

                case MessageCode.MessageDirector_AddInterestRange:
                    ulong low = message.ReadUInt64();
                    ulong high = message.ReadUInt64();
                    AddInterest(origin, low, high);
                    break;

                case MessageCode.MessageDirector_QueueOnDisconnect:
                    var msg = new Message(message);
                    _consumers.Find(c => c.NetworkManager == origin).QueueOnDisconnect(msg);
                    break;

                case MessageCode.MessageDirector_RemoveInterest:
                    channel = message.ReadUInt64();
                    RemoveInterest(origin, channel, channel);
                    break;

                case MessageCode.MessageDirector_RemoveInterestMultiple:
                    channels = message.ReadValues<ulong>();
                    RemoveInterest(origin, channels);
                    break;

                case MessageCode.MessageDirector_RemoveInterestRange:
                    low = message.ReadUInt64();
                    high = message.ReadUInt64();
                    RemoveInterest(origin, low, high);
                    break;
            }
        }

        private void RemoveInterest(INetworkManager origin, ulong[] channels)
        {
            _consumers.Find(g => g.NetworkManager == origin).Channels.RemoveAll(c => channels.Contains(c));
        }

        private void RemoveInterest(INetworkManager networkManager, ulong low, ulong high)
        {
            MessageConsumer consumer = _consumers.Find(c => c.NetworkManager == networkManager) ??
                            new MessageConsumer(networkManager);
            for (ulong channel = low; channel < high; channel++)
            {
                consumer.Channels.Remove(channel);
            }
        }
    }
}