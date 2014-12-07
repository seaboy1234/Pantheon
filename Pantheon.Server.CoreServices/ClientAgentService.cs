using System;
using System.Net;
using System.Xml.Linq;
using Pantheon.ClientAgent;
using Pantheon.Core;
using Pantheon.Core.MessageRouting;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server.CoreServices
{
    internal class ClientAgentService : PantheonService
    {
        private ulong _channel;
        private ClientAgentConfiguration _config;
        private int _port;
        private ClientAgentServer _server;
        private string _version;

        public override string Name
        {
            get { return "ClientAgent"; }
        }

        public int Port
        {
            get { return _port; }
        }

        protected override string[] Dependencies
        {
            get
            {
                return new[] { "StateServer", "EventLogger" };
            }
        }

        public override bool CanLoad(IServiceContainer container)
        {
            if (base.CanLoad(container))
            {
                return true;
            }

            bool allActive = true;

            allActive = container.IsServiceRemote("state_server");

            return allActive;
        }

        public override void Start()
        {
            _config.MessageRouter = new MessageRouter(MessageDirector);
            _server = new ClientAgentServer(_config);

            _server.Start();
        }

        public override void Stop()
        {
            _server.Stop();
        }

        protected override ConfigurationModule GenerateDefault()
        {
            return new ConfigurationModule("Service",
                new ConfigurationModule("Bind",
                    new ConfigurationModule("Port", 9091).Attribute(),
                    new ConfigurationModule("Tcp", true).Attribute()),
                new ConfigurationModule("Channel", 1000),
                new ConfigurationModule("Version", "1.0"),
                new ConfigurationModule("Services",
                    new ConfigurationModule("Service",
                        new ConfigurationModule("Name", "client_services").Attribute(),
                        new ConfigurationModule("Channel", 1234).Attribute())));
        }

        protected override void LoadConfig()
        {
            _config = new ClientAgentConfiguration();

            var bind = Config.Child("Bind");
            _port = bind.Child("Port").Int16(9091);
            _config.Tcp = bind.Child("Tcp").Boolean(true);

            _channel = Config.Child("Channel").UInt64(1000);
            _version = Config.Child("Version").String("1.0");
            if (Config.Child("Services") != null)
            {
                foreach (var element in Config.Child("Services").Children("Open"))
                {
                    string service = element.Child("Service").String();
                    ulong channel = element.Child("Channel").UInt64();

                    _config.AddDefaultChannel(service, channel);
                }
            }

            _config.Channel = _channel;
            _config.Port = _port;
            _config.Version = _version;
        }
    }
}