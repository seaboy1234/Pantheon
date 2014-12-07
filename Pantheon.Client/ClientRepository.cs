using System;
using System.Collections.Generic;
using System.Threading;
using Pantheon.Client.DistributedObjects;
using Pantheon.Client.PacketRouting;
using Pantheon.Client.Services;
using Pantheon.Common;
using Pantheon.Common.Event;
using Pantheon.Common.IO;

namespace Pantheon.Client
{
    /// <summary>
    ///   Provides basic services to a client.
    /// </summary>
    public class ClientRepositoryBase
    {
        private int _dobjHash;
        private INetworkManager _networkManager;
        private List<DObjectBaseClient> _objects;
        private IDistributedObjectProvider _provider;
        private PacketRouter _router;
        private List<IService> _services;
        private ITaskMarshaler _taskQueue;
        private string _version;

        /// <summary>
        ///   Gets all Distributed Objects contained within this client's interest.
        /// </summary>
        public IEnumerable<DObjectBaseClient> DistributedObjects
        {
            get { return _objects; }
        }

        /// <summary>
        ///   Gets the underlying <see cref="INetworkManager" /> which provides communication to a
        ///   Client Agent.
        /// </summary>
        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        public event Action Authenticated = delegate { };

        public event Action ConnectionReady = delegate { };

        public event Action<DObjectBaseClient> DestroyObject = delegate { };

        public event Action Disconnected = delegate { };

        public event Action<DisconnectCode, string> Ejected = delegate { };

        public event Action<DObjectBaseClient> Generated = delegate { };

        public event Action<DObjectBaseClient> GeneratedOwner = delegate { };

        public event Action<Exception> NetworkException = delegate { };

        /// <summary>
        ///   Initializes a new <see cref="ClientRepositoryBase" />
        /// </summary>
        /// <param name="settings"></param>
        public ClientRepositoryBase(ConnectionSettings settings)
        {
            _networkManager = settings.Create();
            _router = new PacketRouter(_networkManager, settings.EnableAutomaticRouting);
            _objects = new List<DObjectBaseClient>();
            _services = new List<IService>();
            _version = settings.Version;
            _provider = settings.DistributedObjectProvider;
            _dobjHash = settings.DistributedObjectAssembly.GetHashCode();
            _taskQueue = settings.TaskMarshaler;
            _router.EnableRoutingOn(this);

            _router.OnException += OnRouterException;
            _router.DefaultRoute += DefaultRoute;

            _networkManager.OnConnected += SendHello;
            _networkManager.OnDisconnected += OnDisconnected;
            _networkManager.OnLidgrenMessage += OnErrorWarning;
        }

        public void AddInterest(uint zoneId)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_AddInterest);
            stream.Write(zoneId);
            _networkManager.WriteMessage(stream);
        }

        public void AddInterestMultiple(params uint[] zoneIds)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_AddInterestMultiple);
            stream.Write(zoneIds.Length);
            foreach (uint zoneId in zoneIds)
            {
                stream.Write(zoneId);
            }
            _networkManager.WriteMessage(stream);
        }

        public void AddInterestRange(uint zoneIdLow, uint zoneIdHigh)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_AddInterestRange);
            stream.Write(zoneIdLow);
            stream.Write(zoneIdHigh);
            _networkManager.WriteMessage(stream);
        }

        public void Connect()
        {
            _networkManager.Connect();
        }

        public void Disconnect()
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_Disconnecting);
            _networkManager.WriteMessage(stream);
            _networkManager.Disconnect("client exited");
        }

        public void FindObjectChildren(uint doid, uint zoneId)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_DiscoverObjectChildren);
            stream.Write(doid);
            stream.Write(zoneId);

            _networkManager.WriteMessage(stream);
        }

        public DObjectBaseClient GetObject(uint doid)
        {
            var obj = _objects.Find(o => o.DoId == doid);

            if (obj == null)
            {
                Console.WriteLine("object with doid {0} was null", doid);
            }

            return obj;
        }

        [Obsolete("Use GetServiceHandle()")]
        public TService GetService<TService>() where TService : Service
        {
            Type serviceType = typeof(TService);

            TService service = (TService)serviceType.GetConstructor(new[] { typeof(PacketRouter) })
                                                    .Invoke(new[] { _router });
            service = ServiceProxy.CreateProxy(service);

            if (!service.IsConnected)
            {
                service.Discover();
            }
            _services.Add(service);
            return service;
        }

        public TService GetServiceHandle<TService>()
            where TService : IService
        {
            Type serviceType = typeof(TService);

            TService service = (TService)serviceType.GetConstructor(new[] { typeof(PacketRouter) })
                                                    .Invoke(new[] { _router });
            _services.Add(service);
            Console.WriteLine("Constructed Service Handle for {0}", serviceType.Name);
            return service;
        }

        public void RemoveInterest(uint zoneId)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_RemoveInterest);
            stream.Write(zoneId);
            _networkManager.WriteMessage(stream);
        }

        public void RemoveInterestMultiple(params uint[] zoneIds)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_RemoveInterestMultiple);
            stream.Write(zoneIds.Length);
            foreach (uint zoneId in zoneIds)
            {
                stream.Write(zoneId);
            }
            _networkManager.WriteMessage(stream);
        }

        public void RemoveInterestRange(uint zoneIdLow, uint zoneIdHigh)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_RemoveInterestRange);
            stream.Write(zoneIdLow);
            stream.Write(zoneIdHigh);
            _networkManager.WriteMessage(stream);
        }

        public void RouteMessages()
        {
            NetStream stream = null;
            while ((stream = _networkManager.ReadMessage()) != null)
            {
                _router.RoutePacket(stream);
            }
        }

        private void AwaitMarshaledTask(Action task)
        {
            bool complete = false;

            _taskQueue.MarshalTask(() =>
            {
                task();
                complete = true;
            });

            while (!complete)
            {
                Thread.Sleep(1);
            }
        }

        private void DefaultRoute(NetStream obj)
        {
            Console.WriteLine(obj.ReadPacketId().ToString());
        }

        [PacketHandler(ClientControlCode.Client_DestroyObject)]
        private void Destroy(NetStream stream)
        {
            uint doid = stream.ReadUInt32();
            var obj = GetObject(doid);

            AwaitMarshaledTask(() => DestroyObject(obj));

            _objects.Remove(obj);
        }

        [PacketHandler(ClientControlCode.Client_GenerateObject)]
        private void Generate(NetStream stream)
        {
            Console.WriteLine("Generating a new object...");
            Type type = stream.ReadType();
            uint doid = stream.ReadUInt32();
            uint parent = stream.ReadUInt32();

            DObjectBaseClient obj = null;
            AwaitMarshaledTask(() => obj = _provider.Generate(_networkManager, type, doid));
            if (obj == null)
            {
                Console.WriteLine("doid:{0} had invalid distributed type {1}", doid, type.AssemblyQualifiedName);
                return;
            }

            obj.Parent = GetObject(parent);

            AwaitMarshaledTask(() => obj.Generate(this, stream));
            _objects.Add(obj);

            _taskQueue.MarshalTask(() => Generated(obj));

            Console.WriteLine("A new {0} (doid: {1}) has entered your interest area.", type.Name, doid);
        }

        [PacketHandler(ClientControlCode.Client_GenerateObjectOwner)]
        private void GenerateOwnerView(NetStream stream)
        {
            Console.WriteLine("Generating a new object(owner)...");

            Type type = stream.ReadType();
            uint doid = stream.ReadUInt32();
            uint parent = stream.ReadUInt32();

            DObjectBaseClient obj = null;
            AwaitMarshaledTask(() => obj = _provider.GenerateOwnerView(_networkManager, type, doid));

            if (obj == null)
            {
                Console.WriteLine(string.Format("doid:{0}(owned) had invalid distributed type {1}", doid, type.AssemblyQualifiedName));
                return;
            }

            obj.IsOwnerView = true;
            obj.Parent = GetObject(parent);

            AwaitMarshaledTask(() => obj.Generate(this, stream));
            _objects.Add(obj);

            _taskQueue.MarshalTask(() => GeneratedOwner(obj));

            Console.WriteLine("A new {0} (doid: {1}) has entered your ownership area.", type.Name, doid);
        }

        [PacketHandler(ClientControlCode.Client_Authenticated)]
        private void HandleAuthenticated(NetStream stream)
        {
            Console.WriteLine("Authenticated");
            _taskQueue.MarshalTask(Authenticated);
        }

        [PacketHandler(ClientControlCode.Client_ObjectMoved)]
        private void HandleObjectMoved(NetStream stream)
        {
            uint doid = stream.ReadUInt32();
            uint parentId = stream.ReadUInt32();

            var obj = GetObject(doid);
            var parent = GetObject(parentId);

            obj.Parent.RemoveChild(obj);
            obj.Parent = parent;

            parent.AddChild(obj);
        }

        [PacketHandler(ClientControlCode.Client_SendRPC)]
        private void HandleRpc(NetStream stream)
        {
            uint doid = stream.ReadUInt32();
            var obj = _objects.Find(o => o.DoId == doid);

            if (obj != null)
            {
                _taskQueue.MarshalTask(() => obj.CallMethod(stream));
            }
        }

        private void OnDisconnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine("Lost connection");
            _services.ForEach(s => s.Dispose());
            _taskQueue.MarshalTask(() => Disconnected());
        }

        [PacketHandler(ClientControlCode.Client_Eject)]
        private void OnEjected(NetStream stream)
        {
            DisconnectCode code = (DisconnectCode)stream.ReadInt32();
            string reason = stream.ReadString();

            foreach (IService service in _services)
            {
                service.Dispose();
            }

            _taskQueue.MarshalTask(() => Ejected(code, reason));
            Console.WriteLine("Connection Closed: {0}({2}) - {1}", code, reason, (int)code);
        }

        private void OnErrorWarning(object sender, DataEventArgs e)
        {
            Console.WriteLine(e.Data.ReadString());
        }

        [PacketHandler(ClientControlCode.Client_HelloResp)]
        private void OnReady(NetStream stream)
        {
            Console.WriteLine("Connected");
            _taskQueue.MarshalTask(ConnectionReady);
        }

        private void OnRouterException(Exception obj)
        {
            Console.WriteLine(obj.ToString());
            NetworkException(obj);
        }

        private void SendHello(object sender, ConnectionEventArgs e)
        {
            NetStream stream = new NetStream();
            stream.Write(ClientControlCode.Client_Hello);
            stream.Write(_version);
            stream.Write(_dobjHash);

            _networkManager.WriteMessage(stream);
        }

        [PacketHandler(ClientControlCode.Client_ObjectSetField)]
        private void SetObjectValue(NetStream stream)
        {
            uint doid = stream.ReadUInt32();
            var obj = _objects.Find(o => o.DoId == doid);
            if (obj != null)
            {
                _taskQueue.MarshalTask(() => obj.SetPropertyValue(stream));
            }
        }
    }
}