using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pantheon.Common.DistributedObjects
{
    public class DistributedMethod
    {
        private readonly MethodInfo _method;
        private readonly string _name;
        private readonly int _typeId;

        public MethodInfo Method
        {
            get { return _method; }
        }

        public string Name
        {
            get { return _name; }
        }

        public int TypeId
        {
            get { return _typeId; }
        }

        public DistributedMethod(int typeId, string name, MethodInfo method)
        {
            _typeId = typeId;
            _name = name;
            _method = method;
        }
    }

    public class DistributedObjectDefinition
    {
        private readonly Type _type;
        private readonly int _typeId;
        private int _memberCount;
        private List<DistributedMethod> _methods;

        private List<DistributedProperty> _properties;

        public Type Type
        {
            get { return _type; }
        }

        public int TypeId
        {
            get { return _typeId; }
        }

        public DistributedObjectDefinition(Type type)
        {
            _type = type;
            if (type.HasAttribute<IdAttribute>())
            {
                _typeId = type.GetAttribute<IdAttribute>().Id;
            }

            _methods = new List<DistributedMethod>();
            _properties = new List<DistributedProperty>();

            GetProperties();
            GetMethods();
        }

        public MethodInfo GetMethod(int id) => _methods.Find(g => g.TypeId == id)?.Method;

        public int GetMethodId(string name) => _methods.Find(g => g.Name == name)?.TypeId ?? -1;

        public PropertyInfo GetProperty(int id) => _properties.Find(g => g.TypeId == id)?.Property;

        public int GetPropertyId(string name) => _properties.Find(g => g.Name == name)?.TypeId ?? -1;

        private void GetMethods()
        {
            var methods = _type.GetInterfaceMethods().Where(g => !g.Name.StartsWith("get_") && !g.Name.StartsWith("set_"));
            foreach (var method in methods)
            {
                var id = ++_memberCount + 100;
                if (method.HasAttribute<IdAttribute>())
                {
                    id = method.GetAttribute<IdAttribute>().Id;
                }
                var info = new DistributedMethod(id, method.Name, method);

                _methods.Add(info);
            }
        }

        private void GetProperties()
        {
            var properties = _type.GetInterfaceProperties();
            foreach (var property in properties)
            {
                var id = ++_memberCount + 200;
                if (property.HasAttribute<IdAttribute>())
                {
                    id = property.GetAttribute<IdAttribute>().Id;
                }
                var info = new DistributedProperty(id, property.Name, property);

                _properties.Add(info);
            }
        }
    }

    public class DistributedProperty
    {
        private readonly string _name;
        private readonly PropertyInfo _property;
        private readonly int _typeId;

        public string Name
        {
            get { return _name; }
        }

        public PropertyInfo Property
        {
            get { return _property; }
        }

        public int TypeId
        {
            get { return _typeId; }
        }

        public DistributedProperty(int typeId, string name, PropertyInfo property)
        {
            _typeId = typeId;
            _name = name;
            _property = property;
        }
    }
}