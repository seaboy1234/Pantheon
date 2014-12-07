using System;
using System.Collections.Generic;
using Pantheon.Core.MessageRouting;

namespace Pantheon.ClientAgent
{
    public class ClientAgentConfiguration
    {
        private ulong _controlChannel;
        private Dictionary<string, ulong> _defaultChannels;
        private bool _locked;

        private int _port;
        private MessageRouter _router;
        private bool _tcp;
        private string _version;

        public ulong Channel
        {
            get
            {
                return _controlChannel;
            }
            set
            {
                if (_locked)
                {
                    const string message = "Cannot change value after use.";
                    throw new InvalidOperationException(message);
                }
                _controlChannel = value;
            }
        }

        public IReadOnlyDictionary<string, ulong> DefaultChannels
        {
            get { return _defaultChannels; }
        }

        public MessageRouter MessageRouter
        {
            get
            {
                return _router;
            }
            set
            {
                if (_locked)
                {
                    const string message = "Cannot change value after use.";
                    throw new InvalidOperationException(message);
                }
                _router = value;
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
                    const string message = "Cannot change value after use.";
                    throw new InvalidOperationException(message);
                }
                _port = value;
            }
        }

        public bool Tcp
        {
            get
            {
                return _tcp;
            }
            set
            {
                if (_locked)
                {
                    const string message = "Cannot change value after use.";
                    throw new InvalidOperationException(message);
                }
                _tcp = value;
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
                    const string message = "Cannot change value after use.";
                    throw new InvalidOperationException(message);
                }
                _version = value;
            }
        }

        public ClientAgentConfiguration()
        {
            _locked = false;
            _version = "UNSET";
            _port = 0;
            _router = null;
            _tcp = true;
            _controlChannel = 0;
            _defaultChannels = new Dictionary<string, ulong>();
        }

        public void AddDefaultChannel(string service, ulong channel)
        {
            if (_locked)
            {
                throw new InvalidOperationException("Configuration is in a locked state.");
            }
            _defaultChannels.Add(service, channel);
        }

        internal void Lock()
        {
            _locked = true;
        }
    }
}