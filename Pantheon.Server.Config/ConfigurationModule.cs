using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pantheon.Server.Config
{
    public class ConfigurationModule : DynamicObject
    {
        private List<ConfigurationModule> _children;
        private bool _isAttribute;
        private string _name;
        private string _value;

        public bool HasChildren
        {
            get { return _children.Count > 0; }
        }

        public bool IsAttribute
        {
            get { return _isAttribute; }
            set { _isAttribute = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string RawValue
        {
            get { return _value; }
            set { _value = value; }
        }

        public ConfigurationModule(string name, string value)
        {
            _name = name;
            _value = value;

            _children = new List<ConfigurationModule>();
        }

        public ConfigurationModule(string name)
            : this(name, string.Empty)
        {
        }

        public ConfigurationModule(string name, object value)
            : this(name, value.ToString())
        {
        }

        public ConfigurationModule(string name, params ConfigurationModule[] modules)
            : this(name, string.Empty)
        {
            foreach (var module in modules)
            {
                AddModule(module);
            }
        }

        public void Add(ConfigurationModule module)
        {
            AddModule(module);
        }

        public void AddModule(ConfigurationModule module)
        {
            _children.Add(module);
        }

        public ConfigurationModule Attribute()
        {
            IsAttribute = true;
            return this;
        }

        public ConfigurationModule Child(string name)
        {
            return Child(g => g.Name == name);
        }

        public ConfigurationModule Child(Func<ConfigurationModule, bool> condition)
        {
            return Children().FirstOrDefault(condition);
        }

        public IEnumerable<ConfigurationModule> Children()
        {
            return _children.AsReadOnly();
        }

        public IEnumerable<ConfigurationModule> Children(Func<ConfigurationModule, bool> condition)
        {
            return Children().Where(condition);
        }

        public IEnumerable<ConfigurationModule> Children(string name)
        {
            return Children(g => g.Name == name);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return (result = Child(binder.Name).Value(binder.ReturnType, null)) != null;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var child = Child(binder.Name);
            if (child != null)
            {
                child.RawValue = value.ToString();
            }
            else
            {
                Add(new ConfigurationModule(binder.Name, value));
            }
            return true;
        }

        #region Value Conversion

        public bool Boolean()
        {
            return Boolean(false);
        }

        public bool Boolean(bool value)
        {
            bool result;
            bool success = bool.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public double Double()
        {
            return Double(0);
        }

        public double Double(double value)
        {
            double result;
            bool success = double.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public short Int16()
        {
            return Int16(0);
        }

        public short Int16(short value)
        {
            short result;
            bool success = short.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public int Int32()
        {
            return Int32(0);
        }

        public int Int32(int value)
        {
            int result;
            bool success = int.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public long Int64()
        {
            return Int64(0);
        }

        public long Int64(long value)
        {
            long result;
            bool success = long.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public float Single()
        {
            return Single(0);
        }

        public float Single(float value)
        {
            float result;
            bool success = float.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public string String()
        {
            return String(string.Empty);
        }

        public string String(string value)
        {
            string result = RawValue;
            bool success = !string.IsNullOrEmpty(result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public ushort UInt16()
        {
            return UInt16(0);
        }

        public ushort UInt16(ushort value)
        {
            ushort result;
            bool success = ushort.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public uint UInt32()
        {
            return UInt32Or(0);
        }

        public uint UInt32Or(uint value)
        {
            uint result;
            bool success = uint.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public ulong UInt64()
        {
            return UInt64(0);
        }

        public ulong UInt64(ulong value)
        {
            ulong result;
            bool success = ulong.TryParse(RawValue, out result);
            if (!success)
            {
                _value = value.ToString();
                result = value;
            }

            return result;
        }

        public T Value<T>()
            where T : class
        {
            return Value(default(T));
        }

        public T Value<T>(T value)
            where T : class
        {
            var type = typeof(T);
            return (T)Value(type, value);
        }

        public object Value(Type type, object value)
        {
            var parse = type.GetMethod("Parse", new Type[] { typeof(string) });
            object final;

            if (parse == null || !parse.IsStatic || parse.ReturnType != type)
            {
                final = value;
                if (final == null)
                {
                    _value = value?.ToString() ?? string.Empty;
                    final = value;
                }

                return final;
            }

            try
            {
                final = parse.Invoke(null, new[] { RawValue });
            }
            catch (Exception)
            {
                _value = value?.ToString() ?? string.Empty;
                final = value;
            }

            return final;
        }
    }

    #endregion Value Conversion
}