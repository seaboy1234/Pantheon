using System;
using Pantheon.Common.Exceptions;

namespace Pantheon.Client.Exceptions
{
    [Serializable]
    public class PantheonClientException : PantheonException
    {
        public PantheonClientException()
        {
        }

        public PantheonClientException(string message) : base(message)
        {
        }

        public PantheonClientException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PantheonClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}