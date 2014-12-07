using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;

namespace Pantheon.Client.DistributedObjects
{
    public abstract class DObjectBaseClient : INotifyPropertyChanged, IDisposable, IDistributedObject
    {
        private List<DObjectBaseClient> _children;
        private DistributedObjectDefinition _def;
        private bool _isOwnerView;
        private INetworkManager _networkManager;
        private uint _objectId;
        private DObjectBaseClient _parent;
        private ClientRepositoryBase _repository;
        private object _serverObject;
        private PropertyInfo _updating;

        public IEnumerable<DObjectBaseClient> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public abstract Type DistributedType { get; }

        public uint DoId
        {
            get { return _objectId; }
        }

        public bool IsOwnerView
        {
            get { return _isOwnerView; }
            internal set { _isOwnerView = value; }
        }

        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        public DObjectBaseClient Parent
        {
            get { return _parent; }
            internal set { _parent = value; }
        }

        public ClientRepositoryBase Repository
        {
            get { return _repository; }
        }

        protected object ServerObject
        {
            get { return _serverObject; }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public event PropertyChangedEventHandler PropertyChangedServer = delegate { };

        protected DObjectBaseClient(INetworkManager networkManager, uint doid)
        {
            _networkManager = networkManager;
            _objectId = doid;
            _isOwnerView = false;
            _serverObject = new DistributedObjectProxy(this, _def).GetTransparentProxy();
            _children = new List<DObjectBaseClient>();
            _def = DistributedObjectRepository.GetObjectDefinition(DistributedType);
            PropertyChanged += NotifyPropertyChanged;
        }

        public T As<T>() where T : DObjectBaseClient
        {
            if (!(this is T))
            {
                throw new ArgumentException("Invalid type " + typeof(T).Name, "T");
            }
            return this as T;
        }

        public void CallMethod(NetStream stream)
        {
            var method = _def.GetMethod(stream.ReadInt32());
            var parameters = method.GetParameters();
            int count = parameters.Length;

            object[] args = new object[count];
            for (int i = 0; i < count; i++)
            {
                if (parameters[i].ParameterType.Is(typeof(IDistributedObject)))
                {
                    var doid = stream.ReadUInt32();
                    Console.WriteLine("doid: " + doid);
                    args[i] = Repository.GetObject(doid);
                }
                else
                {
                    args[i] = stream.ReadObject(parameters[i].ParameterType);
                }
            }

            method.Invoke(this, args);
        }

        public void Generate(ClientRepositoryBase repository, NetStream stream)
        {
            _repository = repository;

            Type type = GetType();
            SerializedObject obj = new SerializedObject(DistributedType, stream);

            foreach (var property in obj)
            {
                var realProp = DistributedType.GetInterfaceProperty(property.Key);
                if (realProp != null && realProp.GetSetMethod() != null)
                {
                    _updating = realProp;
                    realProp.SetValue(this, property.Value);
                    _updating = null;
                }
            }

            Generated();
        }

        public void SetPropertyValue(NetStream stream)
        {
            PropertyInfo info = _def.GetProperty(stream.ReadInt32());
            object value = stream.ReadObject(info.PropertyType);

            _updating = info;
            info.SetValue(this, value);
            PropertyChangedServer(this, new PropertyChangedEventArgs(info.Name));
            _updating = null;
        }

        public override string ToString()
        {
            return string.Format("{0} (doid:{1})", DistributedType.Name, DoId);
        }

        internal void AddChild(DObjectBaseClient obj)
        {
            _children.Add(obj);
        }

        internal void RemoveChild(DObjectBaseClient obj)
        {
            _children.Remove(obj);
        }

        protected void GenerateChildren(uint zoneId)
        {
            _repository.FindObjectChildren(_objectId, zoneId);
        }

        protected virtual void Generated()
        {
        }

        protected PropertyInfo GetProperty(Expression<Func<object>> propertyExpression)
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

            return memberExp.Member as PropertyInfo;
        }

        protected PropertyInfo GetProperty(string name)
        {
            return DistributedType.GetInterfaceProperty(name);
        }

        protected bool IsServerSetting(Expression<Func<object>> propertyExpression)
        {
            var property = GetProperty(propertyExpression);
            if (_updating == null)
            {
                return false;
            }
            return property.Name == _updating.Name;
        }

        protected void OnPropertyChanged(Expression<Func<object>> propertyExpression)
        {
            PropertyInfo info = GetProperty(propertyExpression);
            if (info == null)
            {
                throw new ArgumentException("Must express a property", "propertyExpression");
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(info.Name);
            PropertyChanged(this, args);
        }

        private void NotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var prop = DistributedType.GetInterfaceProperty(e.PropertyName);

            if (prop != null && _updating != prop)
            {
                if (!(prop.HasAttribute<ClientSendAttribute>() || prop.HasAttribute<OwnerSendAttribute>()))
                {
                    return;
                }
                var value = prop.GetValue(this);
                if (value != null)
                {
                    prop.SetValue(ServerObject, value);
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var child in _children)
                    {
                        child.Dispose(disposing);
                    }

                    _parent.RemoveChild(this);
                    _parent = null;
                }

                _networkManager = null;
                _repository = null;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~DObjectBaseClient() { // Do not change this code. Put cleanup
        // code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }
}