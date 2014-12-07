using System;
using System.Reflection;
using Pantheon.Client.DistributedObjects;
using Pantheon.Common;
using Pantheon.Common.TCP;

namespace Pantheon.Client
{
    public class ConnectionSettings
    {
        private Assembly _assembly;
        private bool _autoRoute;
        private string _host;
        private bool _locked;
        private ITaskMarshaler _marshaler;
        private int _port;
        private IDistributedObjectProvider _provider;
        private bool _useTcp;
        private string _version;

        public Assembly DistributedObjectAssembly
        {
            get
            {
                return _assembly;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _assembly = value;
            }
        }

        public IDistributedObjectProvider DistributedObjectProvider
        {
            get
            {
                return _provider;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _provider = value;
            }
        }

        public bool EnableAutomaticRouting
        {
            get
            {
                return _autoRoute;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _autoRoute = value;
            }
        }

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _host = value;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _port = value;
            }
        }

        public ITaskMarshaler TaskMarshaler
        {
            get
            {
                return _marshaler;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _marshaler = value;
            }
        }

        public bool Tcp
        {
            get
            {
                return _useTcp;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _useTcp = value;
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                if (_locked)
                {
                    throw new InvalidOperationException("Settings are locked");
                }
                _version = value;
            }
        }

        public ConnectionSettings()
        {
            _assembly = Assembly.GetEntryAssembly();
            _host = "";
            _port = 0;
            _version = "1.0";
            _provider = new DistributedObjectProvider();
            _marshaler = new PantheonTaskMarshaler();
            _autoRoute = true;
            _useTcp = true;
            _locked = false;
        }

        public INetworkManager Create()
        {
            _locked = true;
            NetworkManagerFeatures features = NetworkManagerFeatures.Client |
                                   (_useTcp ? NetworkManagerFeatures.Tcp : NetworkManagerFeatures.Udp);
            return NetworkManagerFactory.CreateNetworkManager(features, _host, _port);
        }
    }
}