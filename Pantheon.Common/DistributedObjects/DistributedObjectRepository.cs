using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Common.Exceptions;

namespace Pantheon.Common.DistributedObjects
{
    public static class DistributedObjectRepository
    {
        private static List<DistributedObjectDefinition> _Objects;

        public static DistributedObjectDefinition GetObjectDefinition(Type type)
        {
            return _Objects.Find(g => g.Type == type);
        }

        public static DistributedObjectDefinition GetObjectDefinition(int id)
        {
            return _Objects.Find(g => g.TypeId == id);
        }

        internal static void Initialize()
        {
            _Objects = new List<DistributedObjectDefinition>();

            var types = AppDomain.CurrentDomain
                                 .GetAssemblies()
                                 .SelectMany(g => g.GetTypes()
                                                   .Where(f => f.HasAttribute<DistributedObjectAttribute>())
                                                   .Where(f => f.IsInterface));

            foreach (var type in types)
            {
                if (!type.Is(typeof(IDistributedObject)))
                {
                    const string message = "Type {0} must derive from IDistributedObject.";
                    throw new PantheonException(string.Format(message, type.Name));
                }

                _Objects.Add(new DistributedObjectDefinition(type));
            }
        }
    }
}