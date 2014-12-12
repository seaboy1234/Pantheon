using System;
using System.Threading;
using System.Xml.Linq;
using Pantheon.Common;
using Pantheon.Common.TCP;
using Pantheon.Common.Event;
using Pantheon.Core;
using Pantheon.Core.Logging;
using Pantheon.MessageDirector;
using Pantheon.Server.Config;
using Pantheon.Server.ServiceLoader;

namespace Pantheon.Server
{
    internal class MessageDirector : PantheonService
    {
        private string _address;
        private bool _isLocal;
        private MessageDirectorManager _manager;
        private INetworkManager _networkManager;
        private int _port;
        private bool _running;
        private bool _tcp;

        public bool IsLocal
        {
            get { return _isLocal; }
        }

        public override string Name
        {
            get { return "MessageDirector"; }
        }

        public INetworkManager NetworkManager
        {
            get { return _networkManager; }
        }

        public int Port
        {
            get { return _port; }
        }

        public override void Run()
        {
            if (_isLocal)
            {
                _manager.DoMessageLoop();
            }
            else
            {
                _running = true;
                while (_running)
                {
                    MessageDirector.Pump();
                    Thread.Sleep(50);
                }
            }
        }

        public override void Start()
        {
            _networkManager.Connect();
        }

        public override void Stop()
        {
            _running = false;
            if (_isLocal)
            {
                Program.EventLog.Log(LogLevel.Info, "Stopping Message Director");
                Thread.Sleep(100);
                _manager.StopMessageLoop();
            }
        }

        protected override ConfigurationModule GenerateDefault()
        {
            return null;
        }

        protected override void LoadConfig()
        {
            _isLocal = (bool)Config.Child("IsLocal").Boolean(true);
            _tcp = (bool)Config.Child("UseTcp").Boolean();
            _port = (int)Config.Child("Port").Int32(9090);

            if (_isLocal)
            {
                _networkManager = GetServerNetworkManager();
                _manager = new MessageDirectorManager(_networkManager);
                MessageDirector = _manager.MessageDirector;
                _manager.OnHeartbeatStarted += OnStarted;
            }
            else
            {
                _address = Config.Child("Address").String("localhost");
                _networkManager = GetClientNetworkManager();
                MessageDirector = new MessageDirectorClient(_networkManager);
            }
            _networkManager.OnLidgrenMessage += OnErrorWarning;
        }

        private INetworkManager GetClientNetworkManager()
		{
			NetworkManagerFeatures features;
			if (_tcp) {
				features = NetworkManagerFeatures.TcpClient;
			} else {
				features = NetworkManagerFeatures.LidgrenClient;
				}
			return NetworkManagerFactory.CreateNetworkManager (features, _address, _port);
        }

        private INetworkManager GetServerNetworkManager()
        {
			NetworkManagerFeatures features;
			if (_tcp) {
				features = NetworkManagerFeatures.TcpServer;
			} else {
				features = NetworkManagerFeatures.LidgrenServer;
			}
			return NetworkManagerFactory.CreateNetworkManager (features, _port);
        }

        private void OnErrorWarning(object sender, DataEventArgs e)
        {
            Console.WriteLine(e.Data.ReadString());
        }

        private void OnStarted(object sender, EventArgs e)
        {
            Program.EventLog.Log(LogLevel.Info, "Started Message Director on port {0}", _port);
        }
    }
}