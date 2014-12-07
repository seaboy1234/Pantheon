using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;
using Pantheon.Common.Utility;
using Pantheon.Core.Channel;
using Pantheon.Core.MessageRouting;
using Pantheon.Core.StateServer;

namespace Pantheon.Core.DistributedObjects
{
    public class DistributedObjectData : INotifyPropertyChanged, IDisposable,
        IEquatable<DistributedObjectData>, IEnumerable<KeyValuePair<string, object>>
    {
        private Dictionary<PropertyInfo, object> _data;
        private uint _doid;
        private ulong _owner;
        private uint _parent;
        private DistributedObjectData _parentObj;
        private DistributedObjectDefinition _def;
        private MessageRouter _router;
        private Type _type;

        public IReadOnlyDictionary<string, object> Data
        {
            get
            {
                return _data.Select(g => g.Value)
                            .ToDictionary(g => _data.Where(f => f.Value == g)
                                                    .Select(f => f.Key.Name)
                                                    .First());
            }
        }

        public ulong Owner
        {
            get { return _owner; }
        }

        public DistributedObjectData Parent
        {
            get { return _parentObj; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object this[string property]
        {
            get { return Get(property); }
            set { Set(property, value); }
        }

        public object this[int index]
        {
            get { return Get(_data.Keys.ElementAt(index)?.Name); }
            set { Set(_data.Keys.ElementAt(index)?.Name, value); }
        }

        public DistributedObjectData(MessageRouter router)
        {
            _router = router;

            PropertyChanged += HandlePropertyChanged;
        }

        public DistributedObjectData(Type distributedType, MessageRouter router)
            : this(router)
        {
            _type = distributedType;
            _def = DistributedObjectRepository.GetObjectDefinition(_type);

            InitializeData();
        }

        private struct DistributedObjectDataEnumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private IEnumerator<KeyValuePair<PropertyInfo, object>> _enumerator;

            public KeyValuePair<string, object> Current
            {
                get
                {
                    return new KeyValuePair<string, object>(_enumerator.Current.Key.Name, _enumerator.Current.Value);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public DistributedObjectDataEnumerator(IEnumerator<KeyValuePair<PropertyInfo, object>> enumerator)
            {
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }

        public static bool operator !=(DistributedObjectData left, DistributedObjectData right)
        {
            return !(left == right);
        }

        public static bool operator ==(DistributedObjectData left, DistributedObjectData right)
        {
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DistributedObjectData))
            {
                return false;
            }
            return ((DistributedObjectData)obj)._doid == _doid;
        }

        public bool Equals(DistributedObjectData other)
        {
            return other._doid == _doid;
        }

        public void GenerateFromExisting(uint doid)
        {
            Throw.IfValueNotGreaterThan(doid, 0, nameof(doid));
            _doid = doid;

            ulong channel, aiChannel, clChannel;
            channel = Combine(DoPrefix, _doid);
            aiChannel = Combine(AIPrefix, _doid);
            clChannel = Combine(ClPrefix, _doid);

            Message message = new Message();
            message.AddChannel(channel);

            message.Write(MessageCode.StateServer_ObjectGetAll);

            var reply = message.AwaitReply(_router);

            if (reply == null)
            {
                return;
            }

            reply.Position += sizeof(int); // message code
            _parent = reply.ReadUInt32();
            _owner = reply.ReadUInt64();
            _def = DistributedObjectRepository.GetObjectDefinition(reply.ReadInt32());
            _type = _def.Type;

            InitializeData();

            int count = reply.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var property = _def.GetProperty(reply.ReadInt32());
                object value = message.ReadObject(property.PropertyType);

                SuppressPropertyChanged(g => g.Set(property.Name, value));
            }

            _router.RegisterRoute(HandlePropertyChangedRemote, channel, aiChannel, clChannel);

            if (_parent != 0)
            {
                _parentObj = new DistributedObjectData(_router);
                _parentObj.GenerateFromExisting(_parent);
            }
        }

        public void GenerateWithCurrent(ulong owner, IObjectGenerator generator)
        {
            var obj = Serialize();

            _doid = generator.GenerateObject(owner, _def.TypeId, obj);

            ulong channel, aiChannel, clChannel;
            channel = Combine(DoPrefix, _doid);
            aiChannel = Combine(AIPrefix, _doid);
            clChannel = Combine(ClPrefix, _doid);

            _router.RegisterRoute(HandlePropertyChangedRemote, channel, aiChannel, clChannel);
        }

        public T Get<T>(string property)
        {
            return (T)Get(property);
        }

        public object Get(string property)
        {
            var info = _type.GetProperty(property);
            if (info == null)
            {
                throw new KeyNotFoundException("Invalid property: " + property);
            }
            return _data[info];
        }

        public IEnumerable<DistributedObjectData> GetChildren()
        {
            var items = new List<DistributedObjectData>();

            Message message = new Message();
            message.AddChannel(Combine(DoPrefix, _doid));

            message.Write(MessageCode.StateServer_DiscoverChildren);
            message.Write((ushort)0);

            var reply = message.AwaitReply(_router);

            reply.Position += sizeof(int);

            var children = reply.ReadValues<uint>();
            foreach (var child in children)
            {
                DistributedObjectData obj = new DistributedObjectData(_router);
                obj.GenerateFromExisting(child);
                items.Add(obj);
            }

            return items;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return new DistributedObjectDataEnumerator(_data.GetEnumerator());
        }

        public override int GetHashCode()
        {
            int code = 0;
            int doid = ConvertBits.ToInt32(_doid);
            int parentId = ConvertBits.ToInt32(_parent);

            code ^= doid;
            code ^= parentId;

            return code;
        }

        public Type GetPropertyType(string property)
        {
            return _type.GetProperty(property)?.PropertyType;
        }

        public void Set(string property, object value)
        {
            var info = _type.GetProperty(property);
            if (info == null)
            {
                throw new KeyNotFoundException("Invalid property: " + property);
            }
            if (!info.PropertyType.Is(value.GetType()))
            {
                throw new ArgumentException(value.GetType().Name + "cannot be cast to a " + info.PropertyType.Name, "value");
            }
            _data[info] = value;

            OnPropertyChanged(property);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_doid == 0)
            {
                return;
            }

            Message message = new Message();
            message.AddChannel(Combine(DoPrefix, _doid));
            message.From = Combine(AIPrefix, _doid);

            message.Write(MessageCode.StateServer_ObjectSetField);
            message.Write(_def.GetPropertyId(e.PropertyName));
            message.WriteObject(Get(e.PropertyName));

            _router.MessageDirector.QueueSend(message);
        }

        private void HandlePropertyChangedRemote(Message message)
        {
            MessageCode action = message.ReadMessageCode();
            if (action != MessageCode.DObject_BroadcastUpdate)
            {
                return;
            }

            var property = _def.GetProperty(message.ReadInt32());
            object value = message.ReadObject(property.PropertyType);

            SuppressPropertyChanged(g => g.Set(property.Name, value));
        }

        private void InitializeData()
        {
            _data = new Dictionary<PropertyInfo, object>();

            foreach (var property in _type.GetProperties())
            {
                _data.Add(property, null);
            }
        }

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private IDictionary<int, object> Serialize()
        {
            var obj = new Dictionary<int, object>();
            foreach (var item in _data)
            {
                if (item.Key.HasAttribute<RequiredAttribute>() || item.Key.HasAttribute<RamAttribute>())
                {
                    if (item.Value == null)
                    {
                        continue;
                    }
                    obj.Add(_def.GetPropertyId(item.Key.Name), item.Value);
                }
            }

            return obj;
        }

        private void SuppressPropertyChanged(Action<DistributedObjectData> action)
        {
            PropertyChanged -= HandlePropertyChanged;

            try
            {
                action(this);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                PropertyChanged += HandlePropertyChanged;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _doid = 0;
                }

                _data = null;
                _router.DestroyRoute(g => g.Target.GetHashCode() == GetHashCode());
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}