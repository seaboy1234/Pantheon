using System;

namespace Pantheon.Common.Exceptions
{
    [Serializable]
    public class PantheonException : Exception
    {
        public PantheonException()
        {
        }

        public PantheonException(string message) : base(message)
        {
        }

        public PantheonException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PantheonException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        { }
    }
}