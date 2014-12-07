using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pantheon.Server.Config
{
    public class Configuration
    {
        private ConfigurationModule _root;

        public ConfigurationModule Root
        {
            get
            {
                return _root;
            }
            protected set
            {
                if (_root != null)
                {
                    throw new InvalidOperationException("Root must be null.");
                }
                _root = value;
            }
        }

        public Configuration(ConfigurationModule root)
        {
            _root = root;
        }

        protected Configuration()
        {
        }

        public ConfigurationModule Module(string name)
        {
            string[] parts = name.Split('.');
            int index = 0;
            ConfigurationModule module = Root;

            while (index < parts.Length && (module = module.Child(parts[index])) != null)
            {
                index++;
            }

            if(module?.Name != parts.Last())
            {
                return null;
            }

            return module;
        }

        public virtual void Save(string file)
        {
        }
    }
}