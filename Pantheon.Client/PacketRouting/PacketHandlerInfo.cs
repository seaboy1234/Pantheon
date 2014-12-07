using System;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.IO;

namespace Pantheon.Client.PacketRouting
{
    internal class PacketHandlerInfo
    {
        private bool _giveNetworkManager;
        private bool _givePacket;
        private MethodInfo _method;
        private ClientControlCode _packetId;
        private object _target;

        public bool GiveNetworkManager
        {
            get { return _giveNetworkManager; }
        }

        public bool GivePacket
        {
            get { return _givePacket; }
        }

        public ClientControlCode HandledPacket
        {
            get { return _packetId; }
        }

        public MethodInfo Method
        {
            get { return _method; }
        }

        public object Target
        {
            get { return _target; }
        }

        public PacketHandlerInfo(MethodInfo method, PacketHandlerAttribute attribute, object target)
        {
            _target = target;
            _method = method;
            _packetId = attribute.PacketId;

            if (method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(INetworkManager)) ||
                method.GetParameters()[0].ParameterType == typeof(INetworkManager))
            {
                _giveNetworkManager = true;
                _givePacket = method.GetParameters()[1].ParameterType != typeof(NetStream);
            }
            else
            {
                _givePacket = method.GetParameters()[0].ParameterType != typeof(NetStream);
            }
        }

        public PacketHandlerInfo(Action<IPacket> handler, ClientControlCode packet)
        {
            _target = handler.Target;
            _method = handler.Method;
            _packetId = packet;

            _givePacket = true;
        }

        public PacketHandlerInfo(Action<NetStream> handler, ClientControlCode packet)
        {
            _target = handler.Target;
            _method = handler.Method;
            _packetId = packet;

            _givePacket = false;
        }

        public bool Call(object packet, INetworkManager networkManager, out Exception exception)
        {
            exception = null;
            // We cannot use the 'is' operator here because Datagram is an IPacket and a NetStream.
            if (packet.GetType() == typeof(NetStream) && _givePacket)
            {
                throw new InvalidOperationException("Method accepts IPacket");
            }
            try
            {
                if (GiveNetworkManager)
                {
                    _method.Invoke(_target, new[] { networkManager, packet });
                }
                else
                {
                    _method.Invoke(_target, new[] { packet });
                }
                return true;
            }
            catch (TargetInvocationException e)
            {
                exception = e.InnerException;
                return false;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }
    }
}