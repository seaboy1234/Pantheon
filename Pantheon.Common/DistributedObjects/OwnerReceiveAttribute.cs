using System;

namespace Pantheon.Common.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class OwnerReceiveAttribute : Attribute
    {
    }
}