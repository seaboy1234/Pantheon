using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Pantheon.Server.Config;

namespace Pantheon.Server.ServiceLoader
{
    public static class PantheonServiceLoader
    {
        private static Dictionary<string, Type> _Services;

        static PantheonServiceLoader()
        {
            _Services = new Dictionary<string, Type>();
        }

        public static ConfigurationModule GenerateDefaultServices()
        {
            ConfigurationModule services = new ConfigurationModule("Services");
            foreach (var info in _Services)
            {
                var service = (PantheonService)MakeObject(info.Value);

                var config = service.GenerateDefaultConfig();
                if (config == null)
                {
                    continue;
                }

                services.Add(config);
            }

            return services;
        }

        public static PantheonService GetServiceManager(ConfigurationModule config)
        {
            PantheonService service = Construct(config.Child("Type").String());
            return service;
        }

        public static void LoadAssembly(string path)
        {
            Assembly assembly = Assembly.LoadFrom(path);
            LoadAssembly(assembly);
        }

        public static void LoadAssembly(Assembly assembly)
        {
            IEnumerable<Type> types;
            try
            {
                types = assembly
                       .GetTypes()
                       .Where(t => t.IsSubclassOf(typeof(PantheonService)));
            }
            catch (TypeLoadException e)
            {
                Console.WriteLine(e);
                return;
            }
            foreach (var type in types)
            {
                object obj = MakeObject(type);
                string name = ((PantheonService)obj).Name;
                _Services.Add(name, type);
            }
        }

        private static PantheonService Construct(string type)
        {
            Type serviceType;
            if (!_Services.TryGetValue(type, out serviceType))
            {
                throw new InvalidServiceException(type + " is not a valid service");
            }

            var service = (PantheonService)MakeObject(serviceType);

            return service;
        }

        private static object MakeObject(Type type)
        {
            var cotr = type.GetConstructor(Type.EmptyTypes);
            object obj;
            if (cotr == null)
            {
                cotr = type.GetConstructor(new[] { typeof(XElement) });
                obj = cotr.Invoke(new object[] { null });
            }
            else
            {
                obj = cotr.Invoke(null);
            }

            return obj;
        }
    }
}