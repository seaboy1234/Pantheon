using System;

namespace Pantheon.Core.Exceptions
{
    [Serializable]
    public class PantheonServiceException : Exception
    {
        public PantheonServiceException()
        {
        }

        public PantheonServiceException(string message) : base(message)
        {
        }

        public PantheonServiceException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PantheonServiceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}