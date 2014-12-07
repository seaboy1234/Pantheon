using System;
using Pantheon.Common.DistributedObjects;

namespace Pantheon.ClientAgent
{
    public class DistributedObjectPointer
    {
        public DistributedObjectDefinition Definition { get; set; }

        public uint DoId { get; set; }

        public ulong Owner { get; set; }

        public uint Parent { get; set; }

        public Type Type { get; set; }

        public DistributedObjectPointer(uint doid, uint parent, ulong owner, Type type)
        {
            DoId = doid;
            Parent = parent;
            Owner = owner;
            Type = type;
            Definition = DistributedObjectRepository.GetObjectDefinition(type);
        }
    }
}