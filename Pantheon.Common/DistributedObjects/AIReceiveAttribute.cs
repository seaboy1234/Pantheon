using System;

namespace Pantheon.Common.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AIReceiveAttribute : PantheonAttribute
    {
    }
}