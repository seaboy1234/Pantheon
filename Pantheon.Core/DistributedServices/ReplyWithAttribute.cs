using System;

namespace Pantheon.Core.DistributedServices
{
    public class ReplyWithAttribute : Attribute
    {
        private int _code;
        private Serialization _serializationType;

        public int Code
        {
            get { return _code; }
        }

        public Serialization Serialization
        {
            get { return _serializationType; }
            set { _serializationType = value; }
        }

        public ReplyWithAttribute(object code)
            : this((int)code)
        {
        }

        public ReplyWithAttribute(int code)
        {
            _code = code;
            Serialization = Serialization.WriteProperties;
        }
    }
}