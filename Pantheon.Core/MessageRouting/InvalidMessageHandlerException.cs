using System;
using Pantheon.Common.Exceptions;

namespace Pantheon.Core.MessageRouting
{
    [Serializable]
    public class InvalidMessageHandlerException : PantheonException
    {
        public InvalidMessageHandlerException()
        {
        }

        public InvalidMessageHandlerException(string message) : base(message)
        {
        }

        public InvalidMessageHandlerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidMessageHandlerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}