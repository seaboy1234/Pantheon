using System;
using System.IO;
using System.Xml.Linq;
using Pantheon.Core.Logging;
using Pantheon.Core.MessageRouting;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server.CoreServices
{
    internal class EventLoggerService : PantheonService
    {
        private ulong _channel;
        private bool _console;
        private bool _file;
        private EventLogger _manager;

        public override string Name
        {
            get { return "EventLogger"; }
        }

        public override void Start()
        {
            MessageRouter router = new MessageRouter(MessageDirector);
            _manager = new EventLogger(router, _channel);
            _manager.SetLoggerName("EL");

            if (_file)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }
                string log = Path.Combine("Logs",
                    "log_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                new FileLogOutput(_manager, log);
            }
            if (_console)
            {
                new ConsoleLogOutput(_manager);
            }

            _manager.DiscoverServer();
        }

        public override void Stop()
        {
        }

        protected override ConfigurationModule GenerateDefault()
        {
            return new ConfigurationModule("Service",
                                new ConfigurationModule("Channel", 100),
                                new ConfigurationModule("Outputs",
                                    new ConfigurationModule("LogOutput", "Console"),
                                    new ConfigurationModule("LogOutput", "File")));
        }

        protected override void LoadConfig()
        {
            _channel = Config.Child("Channel").UInt64(100);
            if (Config.Child("Outputs") != null)
            {
                foreach (var output in Config.Child("Outputs").Children("LogOutput"))
                {
                    string type = output.String();
                    switch (type)
                    {
                        case "Console":
                            _console = true;
                            break;

                        case "File":
                            _file = true;
                            break;
                    }
                }
            }
        }
    }
}