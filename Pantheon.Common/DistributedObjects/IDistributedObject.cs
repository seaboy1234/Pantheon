using System;

namespace Pantheon.Common.DistributedObjects
{
    public interface IDistributedObject
    {
        uint DoId { get; }
    }
}