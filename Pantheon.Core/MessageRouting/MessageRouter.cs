using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common;
using Pantheon.Common.Utility;

namespace Pantheon.Core.MessageRouting
{
    public class MessageRouter
    {
        private Action<Message> _defaultRoute;
        private Dictionary<ulong, int> _interestCounts;
        private IMessageDirector _messageDirector;
        private List<MessageHandlerInfo> _messageHandlers;

        public IMessageDirector MessageDirector
        {
            get { return _messageDirector; }
        }

        public event Action<MessageRouter, Exception> OnException = delegate { };

        public MessageRouter(IMessageDirector messageDirector)
        {
            Throw.IfNull(messageDirector, nameof(messageDirector));

            _messageHandlers = new List<MessageHandlerInfo>();
            messageDirector.OnMessagesAvailable += RouteMessages;
            _messageDirector = messageDirector;
            _interestCounts = new Dictionary<ulong, int>();
        }

        public MessageRouter(INetworkManager networkManager)
            : this(new MessageDirectorClient(networkManager))
        {
        }

        public MessageRouter(string host, int port)
            : this(host, port, false)
        {
        }

        public MessageRouter(string host, int port, bool useTcp)
        {
            Throw.IfEmpty(host, nameof(host));
            Throw.IfValueNotInRange(port, ushort.MinValue, ushort.MaxValue, nameof(port));

            INetworkManager networkManager;
            NetworkManagerFeatures features = NetworkManagerFeatures.Client |
                                    (useTcp ? NetworkManagerFeatures.Tcp : NetworkManagerFeatures.Udp);
            networkManager = NetworkManagerFactory.CreateNetworkManager(features, host, port);
            networkManager.Connect();

            _messageDirector = new MessageDirectorClient(networkManager);
            _messageHandlers = new List<MessageHandlerInfo>();
            _messageDirector.OnMessagesAvailable += RouteMessages;
        }

        public void DestroyRoute(Predicate<MessageHandlerInfo> predicate)
        {
            lock (_messageHandlers)
            {
                foreach (var info in _messageHandlers.ToArray().Where(r => r != null))
                {
                    if (info != null && predicate(info))
                    {
                        _messageHandlers.Remove(info);
                        foreach (var channel in info.Channels)
                        {
                            _interestCounts[channel]--;
                            if (_interestCounts[channel] <= 0)
                            {
                                _messageDirector.RemoveInterest(channel);
                            }
                        }
                    }
                }
            }
        }

        public void DestroyRoute(object target)
        {
            DestroyRoute(g => g.Target == target);
        }

        public void EnableRoutingOn(object target)
        {
            var routers = MessageRouterFactory.GenerateMessageHandlers(target);
            AddInterest(routers.SelectMany(r => r.Channels));
            _messageHandlers.AddRange(routers);
        }

        public void EnableRoutingOn(Type type)
        {
            var routers = MessageRouterFactory.GenerateMessageHandlers(type);
            AddInterest(routers.SelectMany(r => r.Channels));
            _messageHandlers.AddRange(routers);
        }

        public void RegisterRoute(Action<Message> handler, params ulong[] channels)
        {
            AddInterest(channels);
            _messageHandlers.Add(new MessageHandlerInfo(handler, channels));
        }

        public void SetDefaultRoute(Action<Message> handler)
        {
            _defaultRoute = handler;
        }

        private void AddInterest(IEnumerable<ulong> channels)
        {
            foreach (ulong channel in channels)
            {
                if (_interestCounts.ContainsKey(channel))
                {
                    _interestCounts[channel]++;
                }
                else
                {
                    _interestCounts.Add(channel, 1);
                }
                _messageDirector.AddInterest(channel);
            }
        }

        private void RouteMessages(object sender, EventArgs e)
        {
            var messageDirector = (IMessageDirector)sender;

            IEnumerable<Message> messages = messageDirector.ReadAll();

            foreach (var message in messages.Where(m => m != null))
            {
                var messageHandlers = _messageHandlers.ToArray()
                    .Where(m => m != null && m.Channels.Any(c => message.Channels.Contains(c)))
                    .ToArray();
                if (messageHandlers.Count() == 0)
                {
                    if (_defaultRoute != null)
                    {
                        _defaultRoute(message);
                    }
                    else
                    {
                        foreach (var channel in message.Channels.Where(c => c > 0))
                        {
                            _messageDirector.RemoveInterest(channel);
                        }
                        continue;
                    }
                }
                foreach (var messageHandler in messageHandlers.ToArray())
                {
                    message.Position = 0;
                    Exception ex = messageHandler.CallMethod(message);
                    if (ex != null)
                    {
                        OnException(this, ex);
                    }
                }
            }
        }
    }
}