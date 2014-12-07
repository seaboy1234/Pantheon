using System;

namespace Pantheon.Common.DistributedServices
{
    public class MessageCodeAttribute : Attribute
    {
        private int _messageCode;
        private int _responseCode;

        public int MessageCode
        {
            get { return _messageCode; }
        }

        public object RespondWith
        {
            get { return _responseCode; }
            set { _responseCode = (int)value; }
        }

        public int ResponseCode
        {
            get { return _responseCode; }
        }

        public MessageCodeAttribute(int messageCode)
        {
            _messageCode = messageCode;
        }

        public MessageCodeAttribute(object messageCode)
            : this((int)messageCode)
        {
        }
    }
}