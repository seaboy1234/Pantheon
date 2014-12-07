using System;

namespace Pantheon.Common.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class ClientReceiveAttribute : Attribute
    {
    }
}