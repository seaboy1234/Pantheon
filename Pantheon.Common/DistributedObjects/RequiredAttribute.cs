using System;

namespace Pantheon.Common.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RequiredAttribute : PantheonAttribute
    {
    }
}