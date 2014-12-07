using System;

namespace Pantheon.Common.DistributedServices
{
    public interface IServiceClient
    {
        string ServiceName { get; }

        void FindServer();
    }
}