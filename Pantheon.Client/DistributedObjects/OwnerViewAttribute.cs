using System;

namespace Pantheon.Client.DistributedObjects
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class OwnerViewAttribute : Attribute
    {
    }
}