using System;
using Pantheon.Common.Exceptions;

namespace Pantheon.Server.Exceptions
{
    [Serializable]
    public class ServerConfigException : PantheonException
    {
        public ServerConfigException()
        {
        }

        public ServerConfigException(string message) : base(message)
        {
        }

        public ServerConfigException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServerConfigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}