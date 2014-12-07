using System;
using System.Collections.Generic;
using System.Linq;
using Pantheon.Common;
using Pantheon.Common.DistributedObjects;

namespace Pantheon.Client.DistributedObjects
{
    public class DistributedObjectProvider : IDistributedObjectProvider
    {
        private Dictionary<Type, Type> _distributedOwnerTypes;
        private Dictionary<Type, Type> _distributedTypes;

        public DistributedObjectProvider()
        {
            _distributedOwnerTypes = new Dictionary<Type, Type>();
            _distributedTypes = new Dictionary<Type, Type>();

            GetDistributedTypes(true, _distributedOwnerTypes);
            GetDistributedTypes(false, _distributedTypes);
        }

        public DObjectBaseClient Generate(INetworkManager networkManager, Type baseType, uint doid)
        {
            Type type;

            Console.WriteLine("Generating a {0}", baseType);

            if (!_distributedTypes.TryGetValue(baseType, out type))
            {
                return null;
            }
            var cotr = type.GetConstructor(new[] { typeof(INetworkManager), typeof(uint) });
            var obj = cotr.Invoke(new object[] { networkManager, doid });

            return (DObjectBaseClient)obj;
        }

        public DObjectBaseClient GenerateOwnerView(INetworkManager networkManager, Type baseType, uint doid)
        {
            Type type;

            Console.WriteLine("Generating a {0}", baseType);

            if (!_distributedOwnerTypes.TryGetValue(baseType, out type))
            {
                return null;
            }
            var cotr = type.GetConstructor(new[] { typeof(INetworkManager), typeof(uint) });
            var obj = cotr.Invoke(new object[] { networkManager, doid });

            return (DObjectBaseClient)obj;
        }

        private void GetDistributedTypes(bool ownerView, Dictionary<Type, Type> dictionary)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies
                                     .SelectMany(a => a.GetTypes())
                                     .Where(t => t.IsSubclassOf(typeof(DObjectBaseClient)))
                                     .Where(t => !t.IsAbstract && !t.IsInterface);

            types = types.Where(t => t.HasAttribute<OwnerViewAttribute>() == ownerView);

            foreach (var type in types)
            {
                if (!dictionary.ContainsKey(type.GetInterfaces().First(g => g != typeof(IDistributedObject))))
                {
                    dictionary.Add(type.GetInterfaces().First(g => g != typeof(IDistributedObject) && g.Is(typeof(IDistributedObject))), type);
                }
            }
        }
    }
}