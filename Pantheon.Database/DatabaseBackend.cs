using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.IO;

namespace Pantheon.Database
{
    public abstract class DatabaseBackend
    {
        public abstract string Name { get; }

        public static DatabaseBackend CreateInstance(string backendName, params object[] args)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                                             .SelectMany(a => a.GetTypes())
                                             .Where(t => t.IsSubclassOf(typeof(DatabaseBackend))
                                                      && t.Name.StartsWith(backendName))
                                             .FirstOrDefault();
            return (DatabaseBackend)Activator.CreateInstance(type, args);
        }

        public abstract SerializedObject GetObject(int id);

        public abstract object GetProperty(int id, string name);

        public abstract void SetObject(int id, SerializedObject obj);

        protected IEnumerable<PropertyInfo> GetDbProperties(Type type)
        {
            return type.GetProperties().Where(p => p.HasAttribute<DbAttribute>());
        }
    }
}