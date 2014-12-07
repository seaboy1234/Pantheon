using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pantheon.Common.DistributedObjects;
using Pantheon.Common.TCP;

namespace Pantheon.Common
{
    public class CommonInitializer : PantheonInitializer
    {
        public override void Initialize()
        {
            NetworkManagerFactory.AddNetworkManager(NetworkManagerFeatures.TcpClient, typeof(TcpNetworkManager));
            NetworkManagerFactory.AddNetworkManager(NetworkManagerFeatures.TcpServer, typeof(TcpNetworkManagerServer));

            DistributedObjectRepository.Initialize();
        }
    }
}