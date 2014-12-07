using System;
using Pantheon.Common;
using Pantheon.Common.IO;
using Pantheon.Core.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.ClientAgent
{
    public class PantheonClientAgent : ServiceClient
    {
        public override string Name
        {
            get { return "client_agent"; }
        }

        protected PantheonClientAgent(MessageRouter router, ulong channel)
            : base(router, channel)
        {
        }

        public static PantheonClientAgent Create(MessageRouter router, ulong channel)
        {
            var client = new PantheonClientAgent(router, channel);
            return ServiceProxy.Create(client);
        }

        [RemoteMethod(MessageCode.ClientAgent_AddInterest)]
        public void AddInterest(uint zone) { }

        public void AddInterestMultiple(params uint[] zones)
        {
            Message message = new Message();
            message.Write(MessageCode.ClientAgent_AddInterestMultiple);
            message.Write(zones.Length);
            foreach (var zone in zones)
            {
                message.Write(zone);
            }

            SendMessage(message);
        }

        [RemoteMethod(MessageCode.ClientAgent_AddInterestRange)]
        public void AddInterestRange(uint zoneLow, uint zoneHigh) { }

        [RemoteMethod(MessageCode.ClientAgent_CloseChannel)]
        public void CloseChannel(ulong channel) { }

        [RemoteMethod(MessageCode.ClientAgent_Destroy)]
        public void Destroy(ulong doId) { }

        [RemoteMethod(MessageCode.ClientAgent_Eject)]
        public void Eject(DisconnectCode code, string reason) { }

        [RemoteMethod(MessageCode.ClientAgent_Eject)]
        public void Eject(int disconnectCode, string reason) { }

        public void Generate(uint doId, ulong owner, Type type, SerializedObject obj)
        {
            Message message = new Message();
            message.Write(MessageCode.ClientAgent_Generate);
            message.Write(doId);
            message.Write(owner);
            message.Write(type);

            obj.WriteTo(message);

            SendMessage(message);
        }

        [RemoteMethod(MessageCode.ClientAgent_AddInterest)]
        public void ObjectEnteredZone(uint doid) { }

        [RemoteMethod(MessageCode.ClientAgent_RemoveInterest)]
        public void ObjectLeftZone(uint doid) { }

        [RemoteMethod(MessageCode.ClientAgent_OpenChannel)]
        public void OpenChannel(ulong channel) { }

        [RemoteMethod(MessageCode.ClientAgent_PurgeInterest)]
        public void PurgeInterest() { }

        [RemoteMethod(MessageCode.ClientAgent_RemoveInterest)]
        public void RemoveInterest(uint zone) { }

        public void RemoveInterestMultiple(params uint[] zones)
        {
            Message message = new Message();
            message.Write(MessageCode.ClientAgent_RemoveInterestMultiple);
            message.Write(zones.Length);
            foreach (var zone in zones)
            {
                message.Write(zone);
            }

            SendMessage(message);
        }

        [RemoteMethod(MessageCode.ClientAgent_RemoveInterestRange)]
        public void RemoveInterestRange(uint zoneLow, uint zoneHigh) { }

        public void SendDatagram(ulong from, params object[] args)
        {
            NetStream stream = new NetStream();
            foreach (var arg in args)
            {
                stream.WriteObject(arg);
            }
            SendDatagram(from, stream);
        }

        public void SendDatagram(ulong from, NetStream stream)
        {
            Message message = new Message();
            message.From = from;
            message.Write(MessageCode.ClientAgent_SendDatagram);
            message.Write(stream);

            SendMessage(message);
        }

        [RemoteMethod(MessageCode.ClientAgent_SetStatus)]
        public void SetAuthStatus(AuthenticationState state) { }

        [RemoteMethod(MessageCode.ClientAgent_SetClientId)]
        public void SetId(ulong channel)
        {
            ServiceChannel = channel;
        }

        [RemoteMethod(MessageCode.ClientAgent_SetZoneServer)]
        public void SetZoneServer(ulong server, ulong clientChannel) { }
    }
}