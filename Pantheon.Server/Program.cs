using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Pantheon.Common;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;
using Pantheon.Server.Config;
using Pantheon.Server.Config.Xml;
using Pantheon.Server.Exceptions;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server
{
    internal static class Program
    {
        public static EventLogger EventLog;
        private static ServiceContainer _ServiceContainer;

        internal static void Main(string[] args)
        {
            string configFile = "Pantheon.xml";
            Console.Title = "Pantheon";
            Configuration document;

            AssemblyResolver.Enable();
            LoadModules();

            if (!File.Exists(configFile))
            {
                GenerateDefaultConfig(configFile);

                Console.WriteLine("Generated default config at {0}.  Press any key to exit.", configFile);
                Console.ReadKey(true);
                return;
            }
            document = LoadConfigFile();

            string name;

            ConfigurationModule services, messageDirectorElement;
            LoadConfiguration(document, out name, out services, out messageDirectorElement);

            SetConsoleTitle(name, "Connecting to Cluster");

            MessageDirector messageDirector = CreateMessageDirector(messageDirectorElement);

            var top = new NodeMessageDirector(messageDirector.MessageDirector);

            _ServiceContainer = new ServiceContainer(new MessageRouter(new NodeMessageDirector(top)));
            LogFile.Initialize(new MessageRouter(new NodeMessageDirector(top)));

            List<PantheonService> serviceRunners = LoadServices(services);

            StartServices(serviceRunners, top);

            if (!EventLog.IsConnected)
            {
                if (!EventLog.DiscoverServer())
                {
                    Console.WriteLine("|---------------------ERROR---------------------|");
                    Console.WriteLine("| No EventLogger found on Pantheon Shard.       |");
                    Console.WriteLine("| To add an event logger, insert the following  |");
                    Console.WriteLine("| directive into your <Services /> node.        |");
                    Console.WriteLine("|       |----------------------------|          |");
                    Console.WriteLine("|       | <Service>                  |          |");
                    Console.WriteLine("|       |   <Type>EventLogger</Type> |          |");
                    Console.WriteLine("|       |   <Channel>100</Channel>   |          |");
                    Console.WriteLine("|       | </Service>                 |          |");
                    Console.WriteLine("|       |----------------------------|          |");
                    Console.WriteLine("|                                               |");
                    Console.WriteLine("|-----------------------------------------------|");
                    while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
                    return;
                }
            }
            Task.Run(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
                messageDirector.Stop();
            });
            RunServices(name, serviceRunners);
            if (!messageDirector.IsLocal)
            {
                SetConsoleTitle(name, "Connected");
            }
            else
            {
                SetConsoleTitle(name, "Message Director");
            }
            messageDirector.Run();
        }

        private static MessageDirector CreateMessageDirector(ConfigurationModule messageDirectorConfig)
        {
            MessageDirector messageDirector = new MessageDirector();
            try
            {
                messageDirector.Load(messageDirectorConfig);
                messageDirector.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey(true);
                Environment.Exit(1);
            }
            Task.Delay(500).Wait();

            if (!messageDirector.NetworkManager.IsConnected)
            {
                Console.WriteLine("Connection failed.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
            Console.WriteLine("Press ESC to exit.");
            return messageDirector;
        }

        private static void GenerateDefaultConfig(string configFile)
        {
            var root = new ConfigurationModule("PantheonServer",
                                 new ConfigurationModule("Name", "Core Services").Attribute());

            root.Add(new ConfigurationModule("MessageDirector",
                         new ConfigurationModule("IsLocal", true),
                         new ConfigurationModule("UseTcp", false),
                         new ConfigurationModule("Address", IPAddress.Loopback),
                         new ConfigurationModule("Port", 9090)));

            root.Add(PantheonServiceLoader.GenerateDefaultServices());

            XmlConfiguration document = new XmlConfiguration(root);

            document.Save(configFile);
        }

        private static Configuration LoadConfigFile()
        {
            Configuration document = null;
            try
            {
                document = new XmlConfiguration(XDocument.Load("Pantheon.xml"));
            }
            catch (XmlException e)
            {
                Console.WriteLine("There was an error in your XML syntax:");
                Console.WriteLine(e.Message);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine("There was an error while parsing your Pantheon.xml:");
                Console.WriteLine(e);
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }

            return document;
        }

        private static void LoadConfiguration(Configuration document, out string name,
            out ConfigurationModule services, out ConfigurationModule messageDirectorElement)
        {
            ConfigurationModule server = document.Root;
            if (server == null)
            {
                throw new ServerConfigException("missing PantheonServer element.");
            }
            ConfigurationModule xname = server.Child("Name");
            name = "Undefined";
            if (xname != null)
            {
                name = xname.RawValue;
            }

            var logFile = server.Child("LogFile");

            if (logFile != null && logFile.Child("Enabled").Boolean())
            {
                LogFile.SetLogOutput(Path.Combine("LogFile", DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"));
            }

            services = server.Child("Services");
            if (services == null)
            {
                throw new ServerConfigException("missing PantheonServer.Services element.");
            }

            messageDirectorElement = server.Child("MessageDirector");
            if (messageDirectorElement == null)
            {
                throw new ServerConfigException("missing PantheonServer.MessageDirector element.");
            }
        }

        private static void LoadModules()
        {
            PantheonServiceLoader.LoadAssembly(Assembly.GetExecutingAssembly());

            if (!Directory.Exists("ServerModules"))
            {
                Directory.CreateDirectory("ServerModules");
            }

            foreach (string file in Directory.GetFiles("ServerModules"))
            {
                if (!file.EndsWith(".dll"))
                {
                    continue;
                }
                PantheonServiceLoader.LoadAssembly(file);
            }

            PantheonInitializer.InitializeAll();
        }

        private static List<PantheonService> LoadServices(ConfigurationModule services)
        {
            _ServiceContainer.LoadServices(services);

            var serviceRunners = _ServiceContainer.Services;

            return serviceRunners;
        }

        private static void RunServices(string name, List<PantheonService> serviceRunners)
        {
            foreach (var service in serviceRunners)
            {
                Task.Run(() =>
                {
                    try
                    {
                        service.Run();
                    }
                    catch (Exception e)
                    {
                        EventLog.Log(LogLevel.Critical, "Service {0} on {1} crashed.", service.Name, name);
                        EventLog.LogCritical(e);
                    }
                });
            }
        }

        private static void SetConsoleTitle(string name, string message)
        {
            Console.Title = string.Format("Pantheon Server - {0} ({1})", name, message);
        }

        private static void StartServices(List<PantheonService> serviceRunners, NodeMessageDirector top)
        {
            ServiceManager.EnableServiceDirectory(new MessageRouter(new NodeMessageDirector(top)));

            EventLog = new EventLogger(new MessageRouter(top.AddMessageDirector()));
            EventLog.SetLoggerName("MD");
            EventLog.DiscoverServer();

            foreach (var service in serviceRunners)
            {
                service.MessageDirector = new NodeMessageDirector(top);
                try
                {
                    service.Start();
                }
                catch (Exception e)
                {
                    const string format = "Failed to start {0} service due to a {1}";
                    if (EventLog.IsConnected)
                    {
                        EventLog.Log(LogLevel.Critical, format, service.Name, e.GetType().Name);
                        EventLog.Log(e);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(format, service.Name, e.GetType().Name);
                        Console.WriteLine(e);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
            }
        }
    }
}