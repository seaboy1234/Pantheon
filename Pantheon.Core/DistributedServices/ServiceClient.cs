using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pantheon.Common.DistributedServices;
using Pantheon.Core.MessageRouting;

namespace Pantheon.Core.DistributedServices
{
    public abstract class ServiceClient<TServer> : DistributedService<IEmptyService, TServer>, IEmptyService
        where TServer : IServiceServer
    {
        public ServiceClient(MessageRouter router)
            : base(router, 0)
        {
            FindServer();
        }
    }
}