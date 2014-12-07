using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pantheon.Client;
using Pantheon.Client.Packets;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;

namespace Pantheon.ClientAgent
{
    public sealed class PantheonClient : IDisposable
    {
        private const string ERR_BadHash = "Your Distributed Object hash is outdated.";
        private const string ERR_BadInterest = "You do not have interest on that channel.";
        private const string ERR_BadPacket = "You cannot send that packet type.";
        private const string ERR_BadService = "You cannot connect to that service.";
        private const string ERR_BadVersion = "Invalid game version.";
        private const string ERR_Exception = "An internal server error has occurred.";
        private const string ERR_InvalidState = "Invalid authentication state.";
        private const string ERR_NewClient = "First packet is not ClientHello.";
        private const string ERR_NoObject = "There is no object with that DO_ID.";
        private const string ERR_NotOwner = "You are not that object's owner.";
        private const string ERR_Rejected = "Your authentication status is Rejected.";
        private const string ERR_ServerSet = "Only the server may set that.";
        private const string ERR_Unauthenticated = "Message requires authenticated status.";
        private AuthenticationState _authState;
        private ulong _channel;
        private ClientManager _clientManager;
        private EventLogger _eventLog;
        private List<ulong> _interestedChannels;
        private DateTime _lastHearbeat;
        private INetworkManager _networkManager;
        private List<DistributedObjectPointer> _objects;
        private Dictionary<uint, Message> _onDisconnect;
        private Dictionary<ClientControlCode, Action<NetStream>> _packetHandlers;
        private MessageRouter _router;
        private List<uint> _rpcIds;

        public PantheonClient(ClientManager clientManager, INetworkManager networkManager)
        {
            _clientManager = clientManager;
            _networkManager = networkManager;
            _authState = AuthenticationState.NewClient;
            _interestedChannels = new List<ulong>();
            _objects = new List<DistributedObjectPointer>();
            _lastHearbeat = DateTime.Now;
            _router = new MessageRouter(new NodeMessageDirector(clientManager.MessageDirector));
            _router.OnException += OnException;
            _channel = _clientManager.GetNewId();
            _packetHandlers = new Dictionary<ClientControlCode, Action<NetStream>>();
            _onDisconnect = new Dictionary<uint, Message>();
            _rpcIds = new List<uint>();

            _router.RegisterRoute(HandleServerMessage, _channel, Channel.AllClientAgents);

            networkManager.OnDataReceived += HandleIncomingMessage;
            _eventLog = ServiceManager.GetServiceHandle<EventLogger>(_router);
            _eventLog.SetLoggerName("CA");
            _eventLog.Log(LogLevel.Info,
                "New client connected from {0} with an Id of {1}",
                _networkManager.Host,
                _channel);
            NetStream stream;
            while ((stream = networkManager.ReadMessage()) != null)
            {
                stream.Position = 0;
                if (stream.Position == 0)
                {
                    HandleIncomingMessage(null, new Common.Event.DataEventArgs(networkManager, stream));
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void AddInterest(NetStream stream)
        {
            uint zone = stream.ReadUInt32();

            ulong channel = Channel.Combine(Channel.DoPrefix, zone);
            ulong clientChannel = Channel.Combine(Channel.ClPrefix, zone);

            _router.RegisterRoute(HandleServerMessage, channel, clientChannel);

            Message message = new Message();
            message.From = Channel.GenerateCallback();
            message.AddChannel(channel);
            message.Write(MessageCode.StateServer_ObjectGetRequired);

            var reply = message.AwaitReply(_router);
            reply.Position += sizeof(int);
            HandleGenerate(reply);
        }

        private void AddInterestMultiple(NetStream stream)
        {
            int count = stream.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                AddInterest(stream);
            }
        }

        private void AddInterestRange(NetStream stream)
        {
            uint low = stream.ReadUInt32();
            uint high = stream.ReadUInt32();

            for (uint i = low; i < high; i++)
            {
                ulong channel = Channel.Combine(Channel.DoPrefix, i);
                ulong clientChannel = Channel.Combine(Channel.ClPrefix, i);

                if (Message.QueryChannel(_router, channel) != "distributed_object")
                {
                    Disconnect(DisconnectCode.ERR_BadInterest, ERR_NoObject);
                    return;
                }

                _router.RegisterRoute(HandleServerMessage, channel, clientChannel);

                Message message = new Message();
                message.AddChannel(channel);
                message.Write(MessageCode.StateServer_ObjectGetRequired);

                var reply = message.AwaitReply(_router);
                reply.Position += sizeof(int);
                HandleGenerate(reply);
            }
        }

        private void ChangeId(ulong newChannel)
        {
            const string format = "My Id is being changed from {0} to {1}";
            _eventLog.Log(LogLevel.Debug, format, _channel, newChannel);

            _router.RegisterRoute(HandleServerMessage, newChannel);
            _router.DestroyRoute(r => r.Channels.First() == _channel);

            _channel = newChannel;
        }

        private bool ClientShouldReceive(DistributedObjectPointer pointer, MemberInfo member)
        {
            bool isOwner = pointer.Owner == _channel;

            if (member.HasAttribute<RequiredAttribute>())
            {
                return true;
            }
            else if (member.HasAttribute<BroadcastAttribute>() || member.HasAttribute<ClientReceiveAttribute>())
            {
                return true;
            }
            else if (member.HasAttribute<OwnerReceiveAttribute>() && isOwner)
            {
                return true;
            }
            return false;
        }

        private void Disconnect(DisconnectCode code, string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!_networkManager.IsConnected)
            {
                return;
            }

            Action<Message> route = HandleServerMessage;
            _router.DestroyRoute(r => r.Method == route.Method);
            _authState = AuthenticationState.Rejected;
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_Eject);
            stream.Write((int)code);
            stream.Write(message);
            _networkManager.WriteMessage(stream);

            _networkManager.Disconnect(string.Format("{0}:::{1}", (int)code, message));
            _eventLog.Log(LogLevel.Info, "{0} in {1}, at line {2}", memberName, sourceFilePath, sourceLineNumber);
            _eventLog.Log(LogLevel.Info, "Client {0} ejected {1}({2}): {3}", _channel, code, (int)code, message);
        }

        private void Disconnecting(NetStream stream)
        {
            _clientManager.ClientDisconnecting(this);
            _authState = AuthenticationState.Rejected;
            _interestedChannels.Clear();
            _objects.Clear();
            _packetHandlers.Clear();
            _networkManager.Disconnect();
        }

        private void DiscoverObjectChildren(NetStream stream)
        {
            uint doid = stream.ReadUInt32();
            ushort zone = stream.ReadUInt16();

            if (_objects.Find(g => g.DoId == doid) == null)
            {
                Disconnect(DisconnectCode.ERR_BadInterest, ERR_BadInterest);
                return;
            }

            Message message = new Message();
            message.From = Channel.GenerateCallback();
            message.AddChannel(Channel.Combine(Channel.DoPrefix, doid));

            message.Write(MessageCode.StateServer_DiscoverChildren);
            message.Write(zone);

            var reply = message.AwaitReply(_router);

            if (reply == null)
            {
                Disconnect(DisconnectCode.CA_IOException, ERR_Exception);
                return;
            }
            reply.Position += sizeof(int);

            uint[] objs = reply.ReadValues<uint>();

            foreach (var id in objs)
            {
                message = new Message();
                message.From = _channel;
                message.AddChannel(Channel.Combine(Channel.DoPrefix, id));

                message.Write(MessageCode.StateServer_ObjectGetRequired);

                reply = message.AwaitReply(_router);
                reply.Position += sizeof(int);
                HandleGenerate(reply);
            }
        }

        private void DiscoverService(NetStream stream)
        {
            DiscoverServiceAsync(stream).Wait();
        }

        private async Task DiscoverServiceAsync(NetStream stream)
        {
            string name = stream.ReadString();

            var service = await ServiceManager.QueryServiceAsync(_router, name);
            if (service == null)
            {
                _eventLog.Log(LogLevel.Debug, "Query to service \"{0}\" failed.", name);
                Disconnect(DisconnectCode.ERR_BadInterest, ERR_BadService);
                return;
            }
            if (!service.IsClientService)
            {
                Disconnect(DisconnectCode.ERR_BadService, ERR_BadService);
                return;
            }
            NetStream reply = new NetStream();

            _interestedChannels.Add(service.Channel);

            reply.Write(ClientControlCode.Client_DiscoverServiceResp);
            reply.Write(name);
            reply.Write(service.Channel);

            _networkManager.WriteMessage(reply);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _authState = AuthenticationState.Rejected;
                if (_networkManager.IsConnected)
                {
                    _networkManager.Disconnect("disposed");
                    _networkManager.OnDataReceived -= HandleIncomingMessage;
                    _router.DestroyRoute(e => e.Target == this);

                    _router.MessageDirector.QueueSend(_onDisconnect.Values);
                }
            }
        }

        private void HandleAuthenticated(NetStream stream)
        {
            var controlCode = stream.ReadPacketId();
            Action<NetStream> packetHandler;
            if (_packetHandlers.TryGetValue(controlCode, out packetHandler))
            {
                packetHandler(stream);
            }
            else
            {
                Disconnect(DisconnectCode.CA_InvalidMessage, ERR_BadPacket);
            }
        }

        private void HandleGenerate(Message message)
        {
            uint doid = Channel.SplitPrefix(message.From);
            uint parent = message.ReadUInt32();
            ulong owner = message.ReadUInt64();
            Type type = message.ReadType();

            try
            {
                if (parent != 0 && _objects.Find(g => g.DoId == parent) == null)
                {
                    var query = new Message();
                    query.AddChannel(Channel.Combine(Channel.DoPrefix, parent));

                    query.Write(MessageCode.StateServer_ObjectGetRequired);

                    var reply = query.AwaitReply(_router);
                    reply.Position += sizeof(int);
                    HandleGenerate(reply);
                }
            }
            catch (Exception e)
            {
                _eventLog.Log(e);
            }
            if (_objects.Find(g => g.DoId == doid) != null)
            {
                return;
            }

            var pointer = new DistributedObjectPointer(doid, parent, owner, type);
            _objects.Add(pointer);
            _router.RegisterRoute(HandleServerMessage, Channel.Combine(Channel.DoPrefix, doid));
            _router.RegisterRoute(HandleServerMessage, Channel.Combine(Channel.ClPrefix, doid));

            NetStream stream = new NetStream();
            if (owner == _channel)
            {
                stream.Write(ClientControlCode.Client_GenerateObjectOwner);
            }
            else
            {
                stream.Write(ClientControlCode.Client_GenerateObject);
            }
            stream.Write(type);
            stream.Write(doid);
            stream.Write(parent);

            stream.Write(message.ReadBytes((int)(message.Length - message.Position)));

            _networkManager.WriteMessage(stream);
        }

        private void HandleIncomingMessage(object sender, Common.Event.DataEventArgs e)
        {
            try
            {
                switch (_authState)
                {
                    case AuthenticationState.NewClient:
                        HandleIncomingNewClient(e.Data);
                        break;

                    case AuthenticationState.Unauthenticated:
                        HandleUnauthenticated(e.Data).Wait();
                        break;

                    case AuthenticationState.Authenticated:
                        HandleAuthenticated(e.Data);
                        break;

                    case AuthenticationState.Rejected:
                        Disconnect(DisconnectCode.ERR_Unauthenticated, ERR_Rejected);
                        break;

                    default:
                        Disconnect(DisconnectCode.CA_IOException, ERR_InvalidState);
                        break;
                }
            }
            catch (Exception ex)
            {
                _eventLog.Log(ex);
                Disconnect(DisconnectCode.CA_IOException, ERR_Exception);
            }
        }

        private void HandleIncomingNewClient(Common.IO.NetStream netStream)
        {
            var action = netStream.ReadInt32();
            var version = netStream.ReadString();
            if (action != (int)ClientControlCode.Client_Hello)
            {
                Disconnect(DisconnectCode.CA_InvalidHello, ERR_NewClient);
            }
            if (version != _clientManager.GetVersion())
            {
                Disconnect(DisconnectCode.ERR_Outdated, ERR_BadVersion);
            }
            _authState = AuthenticationState.Unauthenticated;

            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_HelloResp);
            stream.Write(false); // EnableGzip
            _networkManager.WriteMessage(stream);
        }

        private void HandleServerMessage(Message message)
        {
            if (_authState == AuthenticationState.Rejected)
            {
                return;
            }

            MessageCode control = message.ReadMessageCode();
            if (control == MessageCode.ClientAgent_SetClientId)
            {
                ChangeId(message.ReadUInt64());
            }
            else if (control == MessageCode.ClientAgent_OpenChannel)
            {
                _interestedChannels.Add(message.ReadUInt64());
            }
            else if (control == MessageCode.ClientAgent_CloseChannel)
            {
                ulong channel = message.ReadUInt64();
                _interestedChannels.Remove(channel);
            }
            else if (control == MessageCode.ClientAgent_Eject)
            {
                int code = message.ReadInt32();
                string reason = message.ReadString();
                Disconnect((DisconnectCode)code, reason);
            }
            else if (control == MessageCode.ClientAgent_SetStatus)
            {
                var state = (AuthenticationState)message.ReadByte();
                if (state == _authState)
                {
                    return;
                }

                if (state == AuthenticationState.Authenticated)
                {
                    RegisterPacketHandlers();
                    NetStream stream = new NetStream();
                    stream.Write(ClientControlCode.Client_Authenticated);

                    _networkManager.WriteMessage(stream);
                    _eventLog.Log(LogLevel.Info, "Authenticated client {0}", _channel);
                }
                if (state == AuthenticationState.Rejected)
                {
                    _packetHandlers.Clear();
                }
                _authState = state;
            }
            else if (control == MessageCode.ClientAgent_SendDatagram)
            {
                byte[] data = message.Read((int)message.Length - (int)message.Position);

                Datagram datagram = new Datagram();
                datagram.Channel = message.From;
                datagram.Data = data;

                datagram.WriteTo(_networkManager);
            }
            else if (control == MessageCode.ALL_QueryChannel)
            {
                Message reply = new Message();
                reply.Channels = new[] { message.From };
                reply.From = _channel;
                reply.Write("client_agent");
                _router.MessageDirector.QueueSend(reply);
            }
            else if (control == MessageCode.ClientAgent_Generate)
            {
                HandleGenerate(message);
            }
            else if (control == MessageCode.ClientAgent_Destroy)
            {
                uint doid = message.ReadUInt32();

                _router.DestroyRoute(m => m.Channels.FirstOrDefault() == doid);

                NetStream stream = new NetStream();
                stream.Write(ClientControlCode.Client_DestroyObject);
                stream.Write(doid);

                _networkManager.WriteMessage(stream);
            }
            else if (control == MessageCode.DObject_BroadcastUpdate)
            {
                uint doid = Channel.SplitPrefix(message.From);
                var pointer = _objects.Find(o => o.DoId == doid);
                var property = pointer.Definition.GetProperty(message.ReadInt32());
                var type = pointer.Type;
                var propType = property.PropertyType;

                if (!ClientShouldReceive(pointer, property))
                {
                    // It's okay if we're not allowed to receive this. Our only problem is if the
                    // client wants to SET something it shouldn't be allowed to.
                    return;
                }

                object value = message.ReadObject(propType);
                uint rpcId = message.ReadUInt32();

                if (rpcId != 0)
                {
                    lock (_rpcIds)
                    {
                        if (_rpcIds.Contains(rpcId))
                        {
                            // This code is needed to prevent value reflection.
                            _rpcIds.Remove(rpcId);
                            return;
                        }
                    }
                }

                NetStream stream = new NetStream();

                stream.Write(ClientControlCode.Client_ObjectSetField);
                stream.Write(doid);
                stream.Write(pointer.Definition.GetPropertyId(property.Name));
                stream.WriteObject(value);

                _networkManager.WriteMessage(stream);
            }
            else if (control == MessageCode.DObject_BroadcastRpc)
            {
                uint doid = Channel.SplitPrefix(message.From);
                var pointer = _objects.Find(o => o.DoId == doid);
                var method = pointer.Definition.GetMethod(message.ReadInt32());

                if (pointer == null)
                {
                    return;
                }

                var type = pointer.Type;

                if (!ClientShouldReceive(pointer, method))
                {
                    // It's okay if we're not allowed to receive this. Our only problem is if the
                    // client wants to SET something it shouldn't be allowed to.
                    return;
                }

                var parameters = method.GetParameters();
                int count = parameters.Length;

                object[] args = new object[count];
                for (int i = 0; i < count; i++)
                {
                    if (parameters[i].ParameterType.Is(typeof(IDistributedObject)))
                    {
                        args[i] = message.ReadUInt64();
                    }
                    else
                    {
                        args[i] = message.ReadObject(parameters[i].ParameterType);
                    }
                }

                NetStream stream = new NetStream();
                stream.Write(ClientControlCode.Client_SendRPC);
                stream.Write(doid);
                stream.Write(pointer.Definition.GetMethodId(method.Name));

                foreach (var obj in args)
                {
                    stream.WriteObject(obj);
                }

                _networkManager.WriteMessage(stream);
            }
            else if (control == MessageCode.ClientAgent_AddInterest)
            {
                AddInterest(message);
            }
            else if (control == MessageCode.ClientAgent_AddInterestMultiple)
            {
                AddInterestMultiple(message);
            }
            else if (control == MessageCode.ClientAgent_AddInterestRange)
            {
                AddInterestRange(message);
            }
            else if (control == MessageCode.ClientAgent_RemoveInterest)
            {
                RemoveInterest(message);
            }
            else if (control == MessageCode.ClientAgent_RemoveInterestMultiple)
            {
                RemoveInterestMultiple(message);
            }
            else if (control == MessageCode.ClientAgent_RemoveInterestRange)
            {
                RemoveInterestRange(message);
            }
        }

        private async Task HandleUnauthenticated(NetStream stream)
        {
            var controlCode = stream.ReadPacketId();
            if (controlCode == ClientControlCode.Client_DiscoverService)
            {
                string name = stream.ReadString();

                var service = await _clientManager.GetService(name);
                if (service == null)
                {
                    _eventLog.Log(LogLevel.Debug, "Query to service \"{0}\" failed.", name);
                    Disconnect(DisconnectCode.ERR_BadInterest, ERR_BadService);
                    return;
                }
                if (!service.AllowAnonymous || !service.IsClientService)
                {
                    const string format = "Client tried to access protected service \"{0}\" (clid{1})";
                    _eventLog.Log(LogLevel.Warning, format, service.Name, _channel);
                    Disconnect(DisconnectCode.ERR_Unauthenticated, ERR_Unauthenticated);
                    return;
                }
                NetStream reply = new NetStream();

                _interestedChannels.Add(service.Channel);

                reply.Write(ClientControlCode.Client_DiscoverServiceResp);
                reply.Write(name);
                reply.Write(service.Channel);

                _networkManager.WriteMessage(reply);
            }
            else if (controlCode == ClientControlCode.Client_SendDatagram)
            {
                Datagram datagram = new Datagram();
                datagram.ReadFrom(stream);
                if (!_interestedChannels.Contains(datagram.Channel))
                {
                    Disconnect(DisconnectCode.ERR_Unauthenticated, ERR_Unauthenticated);
                }
                Message message = new Message();

                message.Channels = new[] { datagram.Channel };
                message.From = _channel;

                message.Write(datagram.Data);

                _router.MessageDirector.QueueSend(message);
            }
            else if (controlCode == ClientControlCode.Client_Heartbeat)
            {
                _lastHearbeat = DateTime.Now;
            }
            else
            {
                Disconnect(DisconnectCode.ERR_Unauthenticated, ERR_Unauthenticated);
            }
        }

        private void Heartbeat(NetStream stream)
        {
            _lastHearbeat = DateTime.Now;
        }

        private void ObjectGetField(NetStream stream)
        {
            try
            {
                Task.Run(async () =>
                {
                    uint rpcId = stream.ReadUInt32();
                    uint doid = stream.ReadUInt32();

                    var pointer = _objects.Find(p => p.DoId == doid);
                    var property = pointer.Definition.GetProperty(stream.ReadInt32());

                    if (pointer == null)
                    {
                        Disconnect(DisconnectCode.CA_InvalidMessage, ERR_NoObject);
                    }

                    Message message = new Message();
                    message.Channels = new[] { Channel.Combine(Channel.DoPrefix, doid) };
                    message.From = Channel.GenerateCallback();
                    message.Write(MessageCode.StateServer_ObjectGetField);
                    message.Write(pointer.Definition.GetPropertyId(property.Name));

                    var response = await message.SendAsync(_router);

                    if (response == null)
                    {
                        _eventLog.Log(LogLevel.Error, "null {0},{1},{2}", doid, property.Name, rpcId);
                        return;
                    }

                    response.Position += sizeof(int);

                    try
                    {
                        var name = response.ReadString();

                        object value = response.ReadObject(property.PropertyType);
                        NetStream reply = new NetStream();

                        reply.Write(ClientControlCode.Client_SendRPCResp);
                        reply.Write(rpcId);
                        reply.WriteObject(value);

                        _networkManager.WriteMessage(reply);
                    }
                    catch (IOException)
                    {
                    }
                }).Wait();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ObjectSendRpc(NetStream stream)
        {
            uint rpcId = stream.ReadUInt32();
            uint doid = stream.ReadUInt32();

            var pointer = _objects.Find(p => p.DoId == doid);
            var method = pointer.Definition.GetMethod(stream.ReadInt32());
            var parameters = method.GetParameters();
            var count = parameters.Length;

            if (pointer == null)
            {
                Disconnect(DisconnectCode.CA_InvalidMessage, ERR_NoObject);
            }
            if (!PerformSecurityCheck(pointer, method))
            {
                return;
            }

            object[] args = new object[parameters.Length];

            for (int i = 0; i < count; i++)
            {
                if (parameters[i].ParameterType.Is(typeof(IDistributedObject)))
                {
                    args[i] = stream.ReadUInt32();
                }
                else
                {
                    args[i] = stream.ReadObject(parameters[i].ParameterType);
                }
            }

            List<ulong> channels = new List<ulong>();
            ulong channel = Channel.Combine(Channel.DoPrefix, doid);
            ulong clientChannel = Channel.Combine(Channel.ClPrefix, doid);
            ulong aiChannel = Channel.Combine(Channel.AIPrefix, doid);

            if (method.HasAttribute<BroadcastAttribute>())
            {
                channels.Add(pointer.DoId);
            }
            else
            {
                if (method.HasAttribute<OwnerReceiveAttribute>() && pointer.Owner != _channel)
                {
                    channels.Add(pointer.Owner);
                }
                if (method.HasAttribute<AIReceiveAttribute>())
                {
                    channels.Add(aiChannel);
                }
                if (method.HasAttribute<ClientReceiveAttribute>())
                {
                    channels.Add(clientChannel);
                }
            }

            Message message = new Message();
            message.Channels = channels.ToArray();
            message.From = pointer.DoId;
            message.Write(MessageCode.DObject_BroadcastRpc);
            message.Write(pointer.Definition.GetMethodId(method.Name));
            foreach (var obj in args)
            {
                message.WriteObject(obj);
            }

            var response = message.SendAsync(_router).Result;
            if (method.ReturnType != typeof(void))
            {
                response.ReadInt32();
                string name = response.ReadString();
                object value = response.ReadObject(method.ReturnType);
                NetStream reply = new NetStream();

                reply.Write(ClientControlCode.Client_SendRPCResp);
                reply.Write(rpcId);
                reply.WriteObject(value);

                _networkManager.WriteMessage(reply);
            }
        }

        private void ObjectSetField(NetStream stream)
        {
            uint rpcId = stream.ReadUInt32();
            uint doid = stream.ReadUInt32();
            var pointer = _objects.Find(o => o.DoId == doid);

            if (pointer == null)
            {
                Disconnect(DisconnectCode.ERR_ObjectDef, ERR_NoObject);
                return;
            }

            var property = pointer.Definition.GetProperty(stream.ReadInt32());
            object value = stream.ReadObject(property.PropertyType);

            if (value == null)
            {
                return;
            }

            if (!PerformSecurityCheck(pointer, property))
            {
                return;
            }

            lock (_rpcIds)
            {
                _rpcIds.Add(rpcId);
            }

            Message message = new Message();
            message.Channels = new[] { Channel.Combine(Channel.DoPrefix, doid) };

            message.Write(MessageCode.StateServer_ObjectSetField);
            message.Write(pointer.Definition.GetPropertyId(property.Name));
            message.WriteObject(value);
            message.Write(rpcId);

            _router.MessageDirector.QueueSend(message);
        }

        private void OnException(MessageRouter router, Exception e)
        {
            _eventLog.Log(e);
        }

        private bool PerformSecurityCheck(DistributedObjectPointer pointer, MemberInfo member)
        {
            if (pointer == null)
            {
                Disconnect(DisconnectCode.CA_InvalidMessage, ERR_NoObject);
                return false;
            }
            else if (member.HasAttribute<OwnerSendAttribute>())
            {
                if (pointer.Owner != _channel)
                {
                    Disconnect(DisconnectCode.CA_InvalidMessage, ERR_NotOwner);
                    return false;
                }
            }
            else if (!member.HasAttribute<ClientSendAttribute>())
            {
                Disconnect(DisconnectCode.CA_InvalidMessage, ERR_ServerSet);
                return false;
            }
            return true;
        }

        private async Task<bool> QueryChannel(ulong channel)
        {
            Message message = new Message();

            message.Channels = new[] { channel };
            message.From = _channel;

            message.Write(MessageCode.ALL_QueryChannel);

            var reply = await message.SendAsync(_router);

            return reply != null;
        }

        private void RegisterPacketHandlers()
        {
            if (_packetHandlers.Count > 0)
            {
                return;
            }
            _packetHandlers.Add(ClientControlCode.Client_DiscoverService, DiscoverService);
            _packetHandlers.Add(ClientControlCode.Client_Heartbeat, Heartbeat);
            _packetHandlers.Add(ClientControlCode.Client_SendDatagram, SendDatagram);
            _packetHandlers.Add(ClientControlCode.Client_AddInterest, AddInterest);
            _packetHandlers.Add(ClientControlCode.Client_AddInterestMultiple, AddInterestMultiple);
            _packetHandlers.Add(ClientControlCode.Client_AddInterestRange, AddInterestRange);
            _packetHandlers.Add(ClientControlCode.Client_Disconnecting, Disconnecting);
            _packetHandlers.Add(ClientControlCode.Client_RemoveInterest, RemoveInterest);
            _packetHandlers.Add(ClientControlCode.Client_RemoveInterestMultiple, RemoveInterestMultiple);
            _packetHandlers.Add(ClientControlCode.Client_RemoveInterestRange, RemoveInterestRange);
            _packetHandlers.Add(ClientControlCode.Client_ObjectSetField, ObjectSetField);
            _packetHandlers.Add(ClientControlCode.Client_ObjectGetField, ObjectGetField);
            _packetHandlers.Add(ClientControlCode.Client_SendRPC, ObjectSendRpc);
            _packetHandlers.Add(ClientControlCode.Client_DiscoverObjectChildren, DiscoverObjectChildren);
        }

        private void RemoveInterest(NetStream stream)
        {
            uint zone = stream.ReadUInt32();
            _router.DestroyRoute(g => g.Channels.Contains(zone) && g.Channels.Count() == 1);
        }

        private void RemoveInterestMultiple(NetStream stream)
        {
            int count = stream.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                RemoveInterest(stream);
            }
        }

        private void RemoveInterestRange(NetStream stream)
        {
            uint low = stream.ReadUInt32();
            uint high = stream.ReadUInt32();

            for (uint i = low; i < high; i++)
            {
                _router.DestroyRoute(g => g.Channels.Contains(i) && g.Channels.Count() == 1);
            }
        }

        private void SendDatagram(NetStream stream)
        {
            Datagram datagram = new Datagram();
            datagram.ReadFrom(stream);
            if (!_interestedChannels.Contains(datagram.Channel))
            {
                Disconnect(DisconnectCode.ERR_BadInterest, ERR_BadInterest);
            }
            Message message = new Message();

            message.Channels = new[] { datagram.Channel };
            message.From = _channel;

            message.Write(datagram.Data);

            _router.MessageDirector.QueueSend(message);
        }
    }
}