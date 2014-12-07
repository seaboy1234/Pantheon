using System;
using Pantheon.Common;

namespace Pantheon.Client.DistributedObjects
{
    public interface IDistributedObjectProvider
    {
        DObjectBaseClient Generate(INetworkManager networkManager, Type baseType, uint doid);

        DObjectBaseClient GenerateOwnerView(INetworkManager networkManager, Type baseType, uint doid);
    }
}