using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common;
using Pantheon.Common.Event;
using Pantheon.Common.IO;

namespace Pantheon.Client.PacketRouting
{
    public class PacketRouter
    {
        private INetworkManager _networkManager;
        private List<PacketHandlerInfo> _packetHandlers;

        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        public event Action<NetStream> DefaultRoute = delegate { };

        public event Action<Exception> OnException = delegate { };

        public PacketRouter(INetworkManager networkManager, bool autoRoute)
        {
            _networkManager = networkManager;
            _packetHandlers = new List<PacketHandlerInfo>();

            if (autoRoute)
            {
                _networkManager.OnDataReceived += DataReceived;
            }
        }

        public void DestroyAll(object target)
        {
            _packetHandlers.RemoveAll(p => p.Target == target);
        }

        public void DestroyRoute<T>(Action<T> method)
        {
            _packetHandlers.RemoveAll(p => p.Method == method.Method && p.Target == method.Target);
        }

        public void EnableRoutingOn(object target)
        {
            _packetHandlers.AddRange(PacketRouterFactory.GenetateHandlersFor(target));
            foreach (var handler in _packetHandlers)
            {
                Console.WriteLine(handler.HandledPacket + " : " + handler.Method.Name);
            }
        }

        public void EnableRoutingOn(Type target)
        {
            _packetHandlers.AddRange(PacketRouterFactory.GenetateHandlersFor(target));
        }

        public void RegisterRoute<T>(Action<T> handler, ClientControlCode packet) where T : IPacket
        {
            PacketHandlerAttribute attr = new PacketHandlerAttribute(packet);
            PacketHandlerInfo info = new PacketHandlerInfo(handler.Method, attr, handler.Target);
            _packetHandlers.Add(info);
        }

        public void RegisterRoute(Action<NetStream> handler, ClientControlCode packet)
        {
            PacketHandlerInfo info = new PacketHandlerInfo(handler, packet);
            _packetHandlers.Add(info);
        }

        public void RoutePacket(NetStream packet)
        {
            Route(_networkManager, packet);
        }

        public void Stop()
        {
            _networkManager.OnDataReceived -= DataReceived;
            _packetHandlers.Clear();
        }

        private void DataReceived(object sender, DataEventArgs e)
        {
            Route(e.NetworkManager, e.Data);
        }

        private void Route(INetworkManager networkManager, NetStream stream)
        {
            ClientControlCode packetId = stream.ReadPacketId();
            stream.Position = 0;
            var packet = DataPacket.CreateFrom(stream);

            var handlers = _packetHandlers.Where(p => p != null && p.HandledPacket == packetId).ToArray();

            Console.WriteLine(string.Format("{0}-{1}", packetId.ToString(), handlers.Count()));

            if (handlers.Count() == 0)
            {
                DefaultRoute(new NetStream(stream));
            }

            foreach (var handler in handlers)
            {
                stream.Position = 4;
                Exception exception;
                object data;
                if (handler.GivePacket)
                {
                    data = packet;
                }
                else
                {
                    data = new NetStream(stream);
                    ((NetStream)data).Position = 4;
                }

                if (!handler.Call(data, networkManager, out exception))
                {
                    OnException(exception);
                }
            }
        }
    }
}