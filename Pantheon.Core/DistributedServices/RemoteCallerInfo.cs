using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Common.Utility;
using Pantheon.Core.ClientAgent;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.DistributedServices
{
    public struct RemoteCallerInfo
    {
        private readonly ulong _channel;

        public ulong CallerChannel
        {
            get { return _channel; }
        }

        public bool IsGameClient
        {
            get { return Channel.IsGameChannel(_channel); }
        }

        public RemoteCallerInfo(ulong channel)
        {
            _channel = channel;
        }

        public void GetClientAgent(MessageRouter router, Action<PantheonClientAgent> action)
        {
            Throw.If(!IsGameClient, nameof(IsGameClient), nameof(RemoteCallerInfo));

            using (PantheonClientAgent agent = PantheonClientAgent.Create(router, _channel))
            {
                action(agent);
            }
        }
    }
}