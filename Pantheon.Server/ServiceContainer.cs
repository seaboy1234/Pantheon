using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server
{
    public class ServiceContainer : IServiceContainer
    {
        private MessageRouter _router;
        private List<PantheonService> _services;

        internal List<PantheonService> Services
        {
            get { return _services; }
        }

        public ServiceContainer(MessageRouter router)
        {
            _router = router;
            _services = new List<PantheonService>();
        }

        public PantheonService GetServiceManager(string service)
        {
            return _services.FirstOrDefault(g => g.Name == service);
        }

        public T GetServiceManager<T>(string service)
            where T : PantheonService
        {
            return (T)GetServiceManager(service);
        }

        public bool IsServiceActive(string service)
        {
            return _services.Any(g => g.Name == service);
        }

        public bool IsServiceRemote(string service)
        {
            return ServiceManager.ServiceExists(_router, service);
        }

        public void LoadServices(ConfigurationModule element)
        {
            var services = element.Children("Service");
            var waiting = new Dictionary<ConfigurationModule, PantheonService>();

            int i = 0;

            foreach (var config in services)
            {
                if (config.Child("Disabled") != null && config.Child("Disabled").Boolean())
                {
                    continue;
                }
                PantheonService service = PantheonServiceLoader.GetServiceManager(config);
                waiting.Add(config, service);
            }

            while (waiting.Count > 0 && i < services.Count())
            {
                foreach (var service in waiting.ToArray())
                {
                    if (service.Value.CanLoad(this))
                    {
                        try
                        {
                            service.Value.Load(service.Key);
                            _services.Add(service.Value);
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error parsing service config for {0}.", service.Key.Child("Type")?.String());
                            Console.WriteLine(e);
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        finally
                        {
                            waiting.Remove(service.Key);
                        }
                    }
                }
                i++;
            }

            if (waiting.Count > 0)
            {
                Console.WriteLine("Could not fully load the following services:");
                foreach (var service in waiting)
                {
                    Console.WriteLine(service.Value.Name);
                    service.Value.Load(service.Key);
                    _services.Add(service.Value);
                }
            }
        }
    }
}