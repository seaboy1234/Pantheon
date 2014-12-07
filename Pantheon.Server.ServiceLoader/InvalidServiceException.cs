using System;
using System.Runtime.Serialization;

namespace Pantheon.Server.ServiceLoader
{
    [Serializable]
    internal class InvalidServiceException : Exception
    {
        public InvalidServiceException()
        {
        }

        public InvalidServiceException(string message) : base(message)
        {
        }

        public InvalidServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}