using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Client.Packets;
using Pantheon.Common;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;

namespace Pantheon.Client
{
    public abstract class DataPacket : IPacket
    {
        private static Dictionary<ClientControlCode, Type> _Packets;

        protected abstract ClientControlCode MessageType { get; }

        static DataPacket()
        {
            _Packets = new Dictionary<ClientControlCode, Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types.Where(t => t.IsSubclassOf(typeof(DataPacket))))
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }
                    if (type.IsGenericType ||
                        !type.IsPublic)
                    {
                        const string message = "Packets must not be generic or internal: ";
                        Console.WriteLine(message + type.Name);
                    }
                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        const string message = "Packet must define a parameterless constructor: ";
                        Console.WriteLine(message + type.Name);
                    }

                    ClientControlCode packetId = ((DataPacket)type.GetConstructor(Type.EmptyTypes)
                                                         .Invoke(null)).MessageType;
                    _Packets.Add(packetId, type);
                }
            }
        }

        public static IPacket CreateFrom(NetStream stream)
        {
            ClientControlCode packetId = (ClientControlCode)stream.ReadInt32();

            if (packetId == ClientControlCode.Client_SendDatagram)
            {
                var datagram = new Datagram();
                datagram.ReadFrom(stream);
                return datagram;
            }

            var packetType = _Packets.Where(p => p.Key == packetId).FirstOrDefault().Value;
            if (packetType == null)
            {
                return null;
            }

            var packet = (DataPacket)packetType.GetConstructor(Type.EmptyTypes).Invoke(null);
            packet.ReadFrom(stream);
            return packet;
        }

        public virtual NetStream GetData()
        {
            var stream = new NetStream();
            WriteTo(stream);
            return stream;
        }

        public ClientControlCode GetMessageType()
        {
            return MessageType;
        }

        public virtual void ReadFrom(NetStream stream)
        {
            stream.ReadProperties(this);
        }

        public void WriteTo(INetworkManager networkManager)
        {
            Throw.IfNull(networkManager, "networkManager");

            NetStream stream = new NetStream();
            stream.Write((int)MessageType);
            WriteTo(stream);

            networkManager.WriteMessage(stream);
        }

        protected virtual void WriteTo(NetStream stream)
        {
            stream.WriteProperties(this);
        }
    }
}