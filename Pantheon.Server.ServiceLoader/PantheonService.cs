using System;
using System.Linq;
using System.Xml.Linq;
using Pantheon.Core;
using Pantheon.Server.Config;

namespace Pantheon.Server.ServiceLoader
{
    public abstract class PantheonService
    {
        private ConfigurationModule _config;
        private IMessageDirector _messageDirector;

        public IMessageDirector MessageDirector
        {
            get
            {
                return _messageDirector;
            }
            set
            {
                if (_messageDirector != null)
                {
                    throw new InvalidOperationException("Cannot change MessageDirector.");
                }
                _messageDirector = value;
            }
        }

        public abstract string Name { get; }

        protected ConfigurationModule Config
        {
            get { return _config; }
        }

        protected virtual string[] Dependencies
        {
            get { return new string[0]; }
        }

        protected PantheonService()
        {
        }

        [Obsolete("Use the default constructor now.")]
        protected PantheonService(XElement config)
        {
        }

        public virtual bool CanLoad(IServiceContainer container)
        {
            return Dependencies.Length == 0 || Dependencies.All(g => container.IsServiceActive(g));
        }

        public ConfigurationModule GenerateDefaultConfig()
        {
            ConfigurationModule element = GenerateDefault();

            if (element == null)
            {
                return null;
            }

            if (element.Child("Type") == null)
            {
                element.Add(new ConfigurationModule("Type", Name).Attribute());
            }
            if (element.Name != "Service")
            {
                element.Name = "Service";
            }

            return element;
        }

        public void Load(ConfigurationModule config)
        {
            _config = config;
            LoadConfig();
        }

        public virtual void Run()
        {
        }

        public abstract void Start();

        public abstract void Stop();

        protected virtual ConfigurationModule GenerateDefault()
        {
            return new ConfigurationModule("Service",
                new ConfigurationModule("Channel", Channel.GenerateService()));
        }

        protected abstract void LoadConfig();
    }
}