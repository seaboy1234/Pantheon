using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Pantheon.Core
{
    public class NodeMessageDirector : IMessageDirector
    {
        private List<NodeMessageDirector> _childern;
        private List<int> _hashCodes;
        private ConcurrentQueue<Message> _incoming;
        private DateTime _lastPump;
        private List<ulong> _myInterest;
        private List<Message> _outgoing;
        private IMessageDirector _parent;

        public IEnumerable<ulong> Interest
        {
            get
            {
                try
                {
                    return _myInterest.ToArray().Concat(_childern.SelectMany(g => g.Interest)).Distinct().ToArray();
                }
                catch (Exception)
                {
                    return _myInterest.AsReadOnly();
                }
            }
        }

        public DateTime LastPump
        {
            get { return _lastPump; }
        }

        public IEnumerable<Message> QueuedMessagesRecv
        {
            get { return _incoming.ToArray(); }
        }

        public IEnumerable<Message> QueuedMessagesSend
        {
            get { return _outgoing.AsReadOnly(); }
        }

        public event EventHandler OnMessagesAvailable = delegate { };

        public event EventHandler OnPump = delegate { };

        public NodeMessageDirector(IMessageDirector parent)
        {
            _parent = parent;
            _parent.OnPump += (sender, e) => Pump();
            _childern = new List<NodeMessageDirector>();
            _myInterest = new List<ulong>();
            _incoming = new ConcurrentQueue<Message>();
            _outgoing = new List<Message>();
            _hashCodes = new List<int>();
            if (parent is NodeMessageDirector)
            {
                NodeMessageDirector node = (NodeMessageDirector)parent;
                node.AddMessageDirector(this);
            }
        }

        public void AddInterest(ulong channel)
        {
            lock (_myInterest)
            {
                if (!_myInterest.Contains(channel))
                {
                    _myInterest.Add(channel);
                }
            }
            _parent.AddInterest(channel);
        }

        public void AddInterest(ulong low, ulong high)
        {
            List<ulong> range = new List<ulong>();
            for (ulong i = low; i < high; i++)
            {
                range.Add(i);
            }
            lock (_myInterest)
            {
                foreach (ulong channel in range)
                {
                    if (!_myInterest.Contains(channel))
                    {
                        _myInterest.Add(channel);
                    }
                }
            }
            _parent.AddInterest(low, high);
        }

        public void AddMessageDirector(NodeMessageDirector messageDirector)
        {
            if (!_childern.Contains(messageDirector))
            {
                _childern.Add(messageDirector);
            }
        }

        public NodeMessageDirector AddMessageDirector()
        {
            return new NodeMessageDirector(this);
        }

        public void Pump()
        {
            IEnumerable<Message> outgoing;
            IEnumerable<Message> incoming;

            if (!(_parent is NodeMessageDirector))
            {
                incoming = ((IMessageDirector)_parent).ReadAll();

                if (incoming.Count() > 0)
                {
                    foreach (var message in incoming.Where(m => !_incoming.Contains(m)))
                    {
                        if (Interest.Intersect(message.Channels).Count() > 0)
                        {
                            _hashCodes.Add(message.GetHashCode());
                            _incoming.Enqueue(message);
                            Propagate(this, message);
                        }
                    }
                }
            }

            lock (_outgoing)
            {
                outgoing = _outgoing.ToArray();
                _outgoing.Clear();
            }
            foreach (var message in outgoing)
            {
                _parent.QueueSend(message);
            }

            if (_incoming.Count > 0)
            {
                OnMessagesAvailable(this, EventArgs.Empty);
                _incoming = new ConcurrentQueue<Message>();
            }

            _lastPump = DateTime.Now;

            if ((DateTime.Now - _parent.LastPump).TotalMilliseconds > 100)
            {
                _parent.Pump();
            }

            OnPump(this, EventArgs.Empty);
        }

        public void QueueOnDisconnect(Message message)
        {
            _parent.QueueOnDisconnect(message);
        }

        public void QueueSend(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            Propagate(this, message);
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
            Message message;
            _incoming.TryDequeue(out message);
            return message;
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
            _incoming = new ConcurrentQueue<Message>();
            return messages;
        }

        public void RemoveInterest(ulong channel)
        {
            lock (_myInterest)
            {
                _myInterest.Remove(channel);
                if (!Interest.Contains(channel))
                {
                    _parent.RemoveInterest(channel);
                }
            }
        }

        public void RemoveInterest(ulong low, ulong high)
        {
            List<ulong> range = new List<ulong>();
            for (ulong i = low; i < high; i++)
            {
                range.Add(i);
            }
            lock (_myInterest)
            {
                _myInterest.RemoveAll(c => range.Contains(c));
                if (Interest.Intersect(range).Count() == 0)
                {
                    _parent.RemoveInterest(low, high);
                }
            }
        }

        private void Propagate(NodeMessageDirector sender, Message message)
        {
            if (sender != this)
            {
                if (_incoming.Count >= 256)
                {
                    _incoming = new ConcurrentQueue<Message>();
                }
                _incoming.Enqueue(message);
            }

            if (_parent != sender)
            {
                if (_parent is NodeMessageDirector)
                {
                    ((NodeMessageDirector)_parent).Propagate(this, message);
                }
                else if (sender != this)
                {
                    _parent.QueueSend(message);
                }
            }
            foreach (var child in _childern)
            {
                if (child != sender)
                {
                    child.Propagate(this, message);
                }
            }
        }
    }
}