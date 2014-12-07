using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Common;

namespace Pantheon.Common
{
    public class LidgrenInitializer : PantheonInitializer
    {
        public override void Initialize()
        {
            NetworkManagerFactory.AddNetworkManager(NetworkManagerFeatures.LidgrenClient, typeof(ClientNetworkManager));
            NetworkManagerFactory.AddNetworkManager(NetworkManagerFeatures.LidgrenServer, typeof(ServerNetworkManager));
        }
    }
}