using System;

namespace Pantheon.Common.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OwnerSendAttribute : PantheonAttribute
    {
    }
}