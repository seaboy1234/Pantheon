using System;

namespace Pantheon.Client.Exceptions
{
    [Serializable]
    public class DistributedObjectException : PantheonClientException
    {
        public DistributedObjectException()
        {
        }

        public DistributedObjectException(string message) : base(message)
        {
        }

        public DistributedObjectException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DistributedObjectException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}