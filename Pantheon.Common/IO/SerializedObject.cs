using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common.Utility;

namespace Pantheon.Common.IO
{
    public static class SerializedObjectStreamExtensions
    {
        public static SerializedObject ReadSerializedObject(this NetStream stream)
        {
            Type type = stream.ReadType();
            SerializedObject obj = new SerializedObject(type);
            obj.ReadFrom(stream);
            return obj;
        }

        public static void Write(this NetStream stream, SerializedObject obj)
        {
            stream.Write(obj.Type);
            obj.WriteTo(stream);
        }
    }

    public class SerializedObject : IEnumerable<KeyValuePair<string, object>>
    {
        private Dictionary<string, object> _objects;
        private Type _type;

        public Type Type
        {
            get { return _type; }
        }

        public object this[string name]
        {
            get
            {
                if (_objects.ContainsKey(name))
                {
                    return _objects[name];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (!_objects.ContainsKey(name))
                {
                    _objects.Add(name, value);
                }
                else
                {
                    _objects[name] = value;
                }
            }
        }

        static SerializedObject()
        {
            NetStream.AddDataHandler(SerializedObjectStreamExtensions.ReadSerializedObject,
                                     SerializedObjectStreamExtensions.Write);
        }

        public SerializedObject()
        {
            _objects = new Dictionary<string, object>();
        }

        public SerializedObject(Type type)
            : this()
        {
            _type = type;
        }

        public SerializedObject(NetStream stream)
            : this()
        {
            _type = stream.ReadType();
            ReadFrom(stream);
        }

        public SerializedObject(Type type, NetStream stream)
            : this(type)
        {
            ReadFrom(stream);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _objects.GetEnumerator();
        }

        public T GetValue<T>(string name)
        {
            return (T)this[name];
        }

        public void ReadFrom(NetStream stream)
        {
            Throw.IfNull(stream, "stream");

            int count = stream.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                string name = stream.ReadString();
                object value = stream.ReadObject(_type.GetAnyProperty(name).PropertyType);
                this[name] = value;
            }
        }

        public void SetValue<T>(string name, T value)
        {
            if (_objects.ContainsKey(name))
            {
                Type oldType = _objects[name].GetType();
                Type type = typeof(T);

                bool fail = !type.IsSubclassOf(oldType) || type != oldType;
                if (fail)
                {
                    throw new InvalidCastException("Cannot cast value to " + oldType.FullName);
                }
            }
            this[name] = value;
        }

        public void UpdateObject(object target)
        {
            Throw.IfNull(target, "target");

            Type type = target.GetType();
            if (!type.Is(_type))
            {
                throw new ArgumentException("must derive from " + _type.Name, "target");
            }

            foreach (var info in type.GetAllProperties())
            {
                if (_objects.ContainsKey(info.Name))
                {
                    info.SetValue(target, this[info.Name]);
                }
            }
        }

        public void WriteTo(NetStream stream)
        {
            Throw.IfNull(stream, "stream");

            var objects = _objects.Where(o => o.Value != null);
            stream.Write(objects.Count());
            foreach (var obj in objects)
            {
                stream.Write(obj.Key);
                stream.WriteObject(obj.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _objects.GetEnumerator();
        }
    }
}