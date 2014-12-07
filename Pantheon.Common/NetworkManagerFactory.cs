using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Common.Exceptions;

namespace Pantheon.Common
{
    public static class NetworkManagerFactory
    {
        private static Dictionary<NetworkManagerFeatures, Type> _NetworkManagers;

        static NetworkManagerFactory()
        {
            _NetworkManagers = new Dictionary<NetworkManagerFeatures, Type>();
        }

        public static void AddNetworkManager(NetworkManagerFeatures features, Type type)
        {
            if (!type.GetInterfaces().Contains(typeof(INetworkManager)))
            {
                throw new ArgumentException("Parameter is not an INetworkManager!", "type");
            }
            _NetworkManagers.Add(features, type);
        }

        public static INetworkManager CreateNetworkManager(NetworkManagerFeatures features, params object[] args)
        {
            Type value = null;

            foreach (var info in _NetworkManagers)
            {
                if ((features & info.Key) == features)
                {
                    value = info.Value;
                    break;
                }
            }

            if (value == null)
            {
                throw new PantheonException("No Network Manager with requested features");
            }

            var obj = Activator.CreateInstance(value, args);

            return (INetworkManager)obj;
        }
    }
}