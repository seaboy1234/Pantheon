using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pantheon.Common.DistributedServices;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;
using Pantheon.Core.Messages;

namespace Pantheon.Core
{
    public static class ServiceManager
    {
        private static MessageRouter _Router;
        private static string _ServerName;
        private static List<ServicePointer> _Services;

        public static bool AcceptsServiceQueries
        {
            get { return _Router != null; }
        }

        static ServiceManager()
        {
            _Services = new List<ServicePointer>();
            _ServerName = "PantheonServer";
        }

        private enum RequestState
        {
            Success,
            Pending,
        }

        private class ServiceQuery
        {
            public ulong Callback { get; set; }

            public List<Message> Replies { get; set; }

            public string Service { get; set; }

            public RequestState State { get; set; }

            public ServiceQuery(string service, ulong callback)
            {
                State = RequestState.Pending;
                Callback = callback;
                Service = service;
                Replies = new List<Message>();
            }

            public void OnReply(Message message)
            {
                if (message == null)
                {
                    return;
                }
                int state = message.ReadInt32();
                if (message.ReadString() == Service)
                {
                    Replies.Add(message);
                }
                State = (RequestState)state;
            }

            public void Send(MessageRouter router, ulong channel)
            {
                Message message = new DiscoverServiceMessage(Service, Callback);
                message.Channels = new[] { channel };
                OnReply(message.AwaitReply(router));
            }
        }

        /// <summary>
        ///   Instructs this <see cref="ServiceManager" /> to begin handling Service Directory
        ///   requests.
        /// </summary>
        /// <param name="router">The <see cref="MessageRouter" /> to use.</param>
        public static void EnableServiceDirectory(MessageRouter router)
        {
            _Router = router;
            router.RegisterRoute(ServiceRequestHandler, Channel.DiscoverService);
            router.RegisterRoute(ServiceQueryRequestHandler, Channel.QueryService);
        }

        public static T GetService<T>(MessageRouter router)
                    where T : IServiceServer, IServiceClient
        {
            Type type = typeof(T);
            T client = (T)type.GetConstructor(new[] { typeof(MessageRouter) })
                                                      .Invoke(new object[] { router });
            client.FindServer();
            return client;
        }

        /// <summary>
        ///   Gets a <see cref="ServiceClient" /> for a given type and readies the client for use.
        /// </summary>
        /// <typeparam name="T">The type of Service Client to create.</typeparam>
        /// <param name="router">The <see cref="MessageRouter" /> to assign.</param>
        /// <returns>
        ///   A new <see cref="ServiceClient" /> that is setup on the correct channel.
        /// </returns>
        public static T GetServiceHandle<T>(MessageRouter router) where T : ServiceClient
        {
            Type type = typeof(T);
            ServiceClient client = (ServiceClient)type.GetConstructor(new[] { typeof(MessageRouter) })
                                                      .Invoke(new[] { router });
            client.DiscoverServer();
            return (T)new ServiceProxy(client).GetTransparentProxy();
        }

        /// <summary>
        ///   Queries for a <see cref="ServicePointer" /> with the specified name.
        /// </summary>
        /// <param name="router">The <see cref="MessageRouter" /> to use for callbacks.</param>
        /// <param name="name">The name of the service to search for.</param>
        /// <param name="maxAttempts">The maximum number of pings to be sent.</param>
        /// <returns>
        ///   The <see cref="ServicePointer" /> associated with the given service; if one does not
        ///   exist on the Pantheon network, null.
        /// </returns>
        public static async Task<ServicePointer> QueryServiceAsync(MessageRouter router,
                                                              string name,
                                                              int maxAttempts = 2)
        {
            ServicePointer service;
            if (TryGetServicePointer(name, out service))
            {
                if (service.Server != null)
                {
                    return service;
                }
            }
            int attempts = 0;
            var message = new Message();
            message.From = Channel.GenerateCallback();
            message.Channels = new[] { Channel.QueryService };
            message.Write(name);
            retry:
            var reply = await message.SendAsync(router, 3000);

            if (reply == null)
            {
                if (attempts < maxAttempts)
                {
                    attempts++;
                    goto retry;
                }
                return null;
            }

            string serviceName = reply.ReadString();
            string server = reply.ReadString();
            ulong channel = reply.ReadUInt64();
            bool allowClients = reply.ReadBoolean();
            bool allowAnonymous = reply.ReadBoolean();

            if (service == null)
            {
                service = new ServicePointer(serviceName,
                                             server,
                                             channel,
                                             false,
                                             allowClients,
                                             allowAnonymous);
            }
            else
            {
                service.Name = serviceName;
                service.Server = server;
                service.Channel = channel;
                service.IsClientService = allowClients;
                service.AllowAnonymous = allowAnonymous;
            }
            return service;
        }

        [Obsolete("Use RegisterService(ServicePointer) instead.")]
        public static void RegisterAnonymousService(string service, ulong controlChannel)
        {
            _Services.Add(new ServicePointer(service, _ServerName, controlChannel, true, true, true));
        }

        [Obsolete("Use RegisterService(ServicePointer) instead.")]
        public static void RegisterService(string service, ulong controlChannel)
        {
            _Services.Add(new ServicePointer(service, _ServerName, controlChannel, true, false, false));
        }

        /// <summary>
        ///   Registers a <see cref="ServicePointer" /> on this <see cref="ServiceManager" /> .
        /// </summary>
        /// <param name="pointer">the <see cref="ServicePointer" /> to register.</param>
        public static void RegisterService(ServicePointer pointer)
        {
            if (pointer.IsLocal)
            {
                pointer.Server = _ServerName;
            }
            _Services.Add(pointer);
        }

        public static bool ServiceExists(MessageRouter router, string service)
        {
            ulong channel;

            return ServiceExists(router, service, out channel);
        }

        public static bool ServiceExists(MessageRouter router, string service, out ulong channel)
        {
            if (TryGetService(service, out channel))
            {
                return true;
            }

            ulong callback = Channel.GenerateCallback();

            ServiceQuery query = new ServiceQuery(service, callback);

            query.Send(router, Channel.DiscoverService);

            router.DestroyRoute(info => info.Channels == new[] { callback });

            if (query.State == RequestState.Pending)
            {
                channel = 0;
                return false;
            }
            else if (query.State == RequestState.Success)
            {
                channel = query.Replies[0].ReadUInt64();
                ServicePointer pointer = new ServicePointer(service, null, channel, false, false, false);
                RegisterService(pointer);
                return true;
            }
            channel = 0;
            return false;
        }

        public static void SetServerName(string name)
        {
            _ServerName = name;
        }

        private static IEnumerable<ulong> GetAll(string name)
        {
            return _Services.Where(s => s.Name == name).Select(s => s.Channel);
        }

        private static void ServicePoolRequestHandler(Message message)
        {
            string service = message.ReadString();
            IEnumerable<ulong> channels = GetAll(service);
            if (channels.Count() > 0)
            {
                Message reply = new Message();
                reply.Channels = new[] { message.From };

                reply.Write((int)RequestState.Success);
                reply.Write(service);
                reply.Write(channels.Count());

                foreach (var channel in channels)
                {
                    reply.Write(channel);
                }

                _Router.MessageDirector.QueueSend(reply);
            }
        }

        private static void ServiceQueryRequestHandler(Message message)
        {
            string service = message.ReadString();
            ulong callback = message.From;
            ServicePointer pointer;

            if (TryGetServicePointer(service, out pointer))
            {
                if (!pointer.IsLocal)
                {
                    return;
                }
                Message reply = pointer.ToMessage();
                reply.Channels = new[] { callback };
                reply.From = Channel.QueryService;

                _Router.MessageDirector.QueueSend(reply);
            }
        }

        private static void ServiceRequestHandler(Message message)
        {
            string service = message.ReadString();
            if (message.Position >= message.Length)
            {
                message.Position = 0;
            }
            ulong callback = message.From;
            ulong serviceChannel;

            if (TryGetLocalService(service, out serviceChannel))
            {
                Message reply = new Message();

                reply.Channels = new[] { callback };
                reply.From = Channel.DiscoverService;

                reply.Write((int)RequestState.Success);
                reply.Write(service);
                reply.Write(serviceChannel);

                _Router.MessageDirector.QueueSend(reply);
            }
        }

        private static bool TryGetLocalService(string name, out ulong channel)
        {
            var item = _Services.Find(s => s.Name == name && s.IsLocal);
            if (item != null)
            {
                channel = item.Channel;
                return true;
            }
            channel = default(ulong);
            return false;
        }

        private static bool TryGetService(string name, out ulong channel)
        {
            var item = _Services.Find(s => s.Name == name);
            if (item != null)
            {
                channel = item.Channel;
                return true;
            }
            channel = default(ulong);
            return false;
        }

        private static bool TryGetServicePointer(string name, out ServicePointer service)
        {
            service = _Services.Find(s => s.Name == name);
            if (service != null)
            {
                return true;
            }
            return false;
        }
    }
}