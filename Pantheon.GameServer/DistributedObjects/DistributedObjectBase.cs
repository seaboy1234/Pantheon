using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;
using Pantheon.Core;
using Pantheon.Core.DistributedObjects;
using Pantheon.Core.Event;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;
using Pantheon.Core.StateServer;

namespace Pantheon.GameServer.DistributedObjects
{
    /// <summary>
    ///   Base class for all Distributed Objects. It is recommended to inherit from
    ///   <see cref="DistributedObject{T, TRepository}" /> instead of this class.
    /// </summary>
    [DebuggerDisplay("{DistributedType} {DoId}", Type = "Proxy of {DistributedType}")]
    public abstract class DistributedObjectBase : IDistributedObject, IObjectGenerator, IDisposable
    {
        private bool _asyncDone;
        private DistributedObjectDefinition _def;
        private uint _doid;
        private bool _isGenerated;
        private IMessageDirector _messageDirector;
        private MessageRouter _messageRouter;
        private ulong _owner;
        private DistributedObjectBase _parent;
        private bool _pauseSending;
        private object _remoteObject;
        private IAIRepository _repository;
        private List<PropertyInfo> _updates;
        private PropertyInfo _updating;
        private uint _zoneId;
        private bool disposedValue = false;

        public ulong AIChannel
        {
            get { return Channel.Combine(Channel.AIPrefix, _doid); }
        }

        public ulong ClChannel
        {
            get { return Channel.Combine(Channel.ClPrefix, _doid); }
        }

        public abstract Type DistributedType { get; }

        public uint DoId
        {
            get { return _doid; }
        }

        public IMessageDirector MessageDirector
        {
            get { return _messageDirector; }
        }

        public MessageRouter MessageRouter
        {
            get { return _messageRouter; }
        }

        public ulong ObjectChannel
        {
            get { return Channel.Combine(Channel.DoPrefix, _doid); }
        }

        public ulong Owner
        {
            get { return _owner; }
        }

        public DistributedObjectBase Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
                StateServerChangeParent(_parent.DoId);
                OnPropertyChanged(() => Parent);
            }
        }

        public IAIRepository Repository
        {
            get { return _repository; }
        }

        public uint ZoneId
        {
            get
            {
                return _zoneId;
            }
            set
            {
                //Repository.ObjectChangeZones(this, value);
                _zoneId = value;
                OnPropertyChanged(() => ZoneId);
            }
        }

        protected bool IsClientUpdate
        {
            get { return _updating != null; }
        }

        protected object RemoteObject
        {
            get { return _remoteObject; }
        }

        public event EventHandler<PropertyUpdatedEventArgs> PropertyChanged = delegate { };

        protected DistributedObjectBase(MessageRouter router, ulong owner)
        {
            _owner = owner;
            _updates = new List<PropertyInfo>();
            _remoteObject = new DistributedObjectProxy(this).GetTransparentProxy();
            _def = DistributedObjectRepository.GetObjectDefinition(DistributedType);
            _messageDirector = router.MessageDirector;
            _messageRouter = router;
            PropertyChanged += HandlePropertyChanged;
            _asyncDone = true;
        }

        public void Destroy()
        {
            Destroy(this);
        }

        public void Destroy(DistributedObjectBase obj)
        {
            _repository.Destroy(obj);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Generate(IAIRepository repository, IObjectGenerator generator)
        {
            if (_isGenerated)
            {
                throw new InvalidOperationException("The object is already generated.");
            }
            Dictionary<int, object> obj = new Dictionary<int, object>();
            Type type = GetType();
            _repository = repository;

            PreGenerate();
            PreGenerateAsync().Wait();

            foreach (var property in DistributedType.GetInterfaceProperties())
            {
                obj.Add(_def.GetPropertyId(property.Name), property.GetValue(this));
            }

            _doid = generator.GenerateObject(_owner, _def.TypeId, obj);

            _messageRouter.RegisterRoute(HandleBroadcastMessage, ObjectChannel, AIChannel);
            Generated();
            GeneratedAsync().Wait();
            _isGenerated = true;
        }

        public T GenerateChild<T>(params object[] args)
            where T : DistributedObjectBase
        {
            return (T)GenerateChild(typeof(T), args);
        }

        public DistributedObjectBase GenerateChild(Type type, params object[] args)
        {
            return GenerateChildWithOwner(type, Repository.ServiceChannel, args);
        }

        public T GenerateChildWithOwner<T>(ulong owner, params object[] args)
            where T : DistributedObjectBase
        {
            return (T)GenerateChildWithOwner(typeof(T), owner, args);
        }

        public DistributedObjectBase GenerateChildWithOwner(Type type, ulong owner, params object[] args)
        {
            return Repository.GenerateWithParent(type, owner, this, args);
        }

        public void GenerateFromPreexisting(AIRepositoryBase repository, uint doid)
        {
            _doid = doid;

            Message message = new Message();
            message.AddChannel(ObjectChannel);
            message.From = Channel.GenerateCallback();

            message.Write(MessageCode.StateServer_ObjectGetAll);

            var reply = message.AwaitReply(repository.MessageRouter);

            reply.Position += sizeof(int);
            uint parent = reply.ReadUInt32();
            ulong owner = reply.ReadUInt64();
            reply.Position += sizeof(int);

            int count = reply.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                HandleUpdateField(reply);
            }
        }

        public uint GenerateObject(ulong owner, int type, IDictionary<int, object> obj)
        {
            Message message = new Message();
            message.AddChannel(ObjectChannel);

            message.Write(MessageCode.StateServer_Generate);
            message.Write(owner);
            message.Write(type);
            message.WriteDictionary(obj);

            var reply = message.AwaitReply(_messageRouter);

            if(reply == null)
            {
                reply = message.AwaitReply(_messageRouter);
            }

            reply.Position += sizeof(int);
            return reply.ReadUInt32();
        }

        public void UpdateObject(GameTime delta)
        {
            BeginUpdate();

            try
            {
                Update(delta);
            }
            catch (Exception e)
            {
                _repository.Log(new
                {
                    LogLevel = LogLevel.Error,
                    Message = "An error has occurred while updating an object.",
                    DoId = DoId,
                    Exception = e
                });
            }
            finally
            {
                EndUpdate();
            }
        }

        public async Task UpdateObjectAsync(GameTime delta)
        {
            await Task.Run(() => UpdateObject(delta));
            if (_asyncDone)
            {
                BeginUpdate();
                _asyncDone = false;
                await UpdateAsync(delta);
                _asyncDone = true;
                EndUpdate();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _messageRouter.DestroyRoute(g => g.Target == this);
                }
                _messageRouter = null;
                _messageDirector = null;
                _parent = null;
                _remoteObject = null;
                _repository = null;
                _updates = null;
                _doid = 0;

                disposedValue = true;
            }
        }

        protected virtual void Generated()
        {
        }

        protected virtual async Task GeneratedAsync()
        {
            await Task.Yield();
        }

        /// <summary>
        ///   Notifies the DistributedObject system of an updated value.
        /// </summary>
        /// <param name="propertyExpression">
        ///   An expression that returns the property. For example: <c>OnPropertyChanged(() =&gt; this.Name);</c>
        /// </param>
        protected void OnPropertyChanged(Expression<Func<object>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }
            MemberExpression memberExp = propertyExpression.Body as MemberExpression;
            if (memberExp == null && propertyExpression.Body is UnaryExpression)
            {
                var unaryExp = propertyExpression.Body as UnaryExpression;
                if (unaryExp == null)
                {
                    throw new ArgumentException("Must return property: () => Property");
                }
                memberExp = unaryExp.Operand as MemberExpression;
                if (memberExp == null)
                {
                    throw new ArgumentException("Must return property: () => Property");
                }
            }

            PropertyInfo info = memberExp.Member as PropertyInfo;
            if (info == null)
            {
                throw new ArgumentException("Must express a property", "propertyExpression");
            }

            PropertyUpdatedEventArgs args = new PropertyUpdatedEventArgs(info);
            PropertyChanged(this, args);
        }

        protected virtual void PreGenerate()
        {
        }

        protected virtual async Task PreGenerateAsync()
        {
        }

        protected virtual void Update(GameTime delta)
        {
        }

        protected virtual async Task UpdateAsync(GameTime delta)
        {
        }

        private void BeginUpdate()
        {
            while (_pauseSending)
            {
                Thread.Sleep(10);
            }
            _pauseSending = true;
        }

        private void EndUpdate()
        {
            _pauseSending = false;

            var properties = _updates.ToArray();
            _updates.Clear();

            Message message = new Message();
            message.Channels = new[] { ObjectChannel };
            message.From = ObjectChannel;

            message.Write(MessageCode.StateServer_ObjectSetFields);
            message.Write(properties.Length);
            foreach (var prop in properties)
            {
                message.Write(_def.GetPropertyId(prop.Name));
                if (prop.PropertyType.Is(typeof(IDistributedObject)))
                {
                    message.Write(((IDistributedObject)prop.GetValue(this)).DoId);
                }
                else
                {
                    message.WriteObject(prop.GetValue(this));
                }
                message.Write(0u);
            }

            MessageDirector.QueueSend(message);
        }

        private void HandleBroadcastMessage(Message message)
        {
            MessageCode code = message.ReadMessageCode();

            switch (code)
            {
                case MessageCode.DObject_BroadcastUpdate:
                    HandleUpdateField(message);
                    break;

                case MessageCode.DObject_BroadcastRpc:
                    HandleRpc(message);
                    break;
            }
        }

        private void HandlePropertyChanged(object sender, PropertyUpdatedEventArgs e)
        {
            if (DistributedType.GetInterfaceProperty(e.Property.Name) == null)
            {
                return;
            }
            if (_updating != null && _updating.Name == e.Property.Name)
            {
                return;
            }
            if (_pauseSending)
            {
                if (!_updates.Contains(e.Property))
                {
                    _updates.Add(e.Property);
                }
                return;
            }
            Message message = new Message();
            message.Channels = new[] { ObjectChannel };
            message.From = ObjectChannel;
            message.Write(MessageCode.StateServer_ObjectSetField);
            message.Write(_def.GetPropertyId(e.Property.Name));

            if (e.Property.PropertyType.Is(typeof(IDistributedObject)))
            {
                message.Write(((IDistributedObject)e.Property.GetValue(this)).DoId);
            }
            else
            {
                message.WriteObject(e.Property.GetValue(this));
            }
            _messageDirector.QueueSend(message);
        }

        private void HandleRpc(Message message)
        {
            MethodInfo info = _def.GetMethod(message.ReadInt32());
            var parameters = info.GetParameters();
            int count = parameters.Length;

            object[] args = new object[count];
            for (int i = 0; i < count; i++)
            {
                if (parameters[i].ParameterType.Is((typeof(IDistributedObject))))
                {
                    var doid = message.ReadUInt32();
                    args[i] = _repository.GetObject(doid);
                }
                else
                {
                    args[i] = message.ReadObject(parameters[i].ParameterType);
                }
            }

            object ret = info.Invoke(this, args);

            if (ret != null)
            {
                if (info.ReturnType.Is(typeof(IDistributedObject)))
                {
                    ret = ((IDistributedObject)ret).DoId;
                }

                Message reply = new Message();
                reply.From = ObjectChannel;
                reply.Channels = new[] { message.From };

                reply.Write(MessageCode.DObject_BroadcastRpcResp);
                reply.Write(_def.GetMethodId(info.Name));
                reply.WriteObject(ret);

                _messageDirector.QueueSend(reply);
            }
        }

        private void HandleUpdateField(Message message)
        {
            PropertyInfo info = _def.GetProperty(message.ReadInt32());

            if (info == null)
            {
                return;
            }

            object value = message.ReadObject(info.PropertyType);
            if (info.GetValue(this) == value)
            {
                return;
            }

            if (!info.HasAttribute<AIReceiveAttribute>() &&
                !info.HasAttribute<BroadcastAttribute>())
            {
                return;
            }
            while (_updating != null) ;

            _updating = info;
            info.SetValue(this, value);
            _updating = null;
        }

        private void StateServerChangeParent(uint doId)
        {
            Message message = new Message();
            message.From = Owner;
            message.AddChannel(ObjectChannel);
            message.Write(MessageCode.StateServer_ObjectSetParent);
            message.Write(doId);
        }
    }
}