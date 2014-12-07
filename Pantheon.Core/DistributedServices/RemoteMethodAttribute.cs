using System;

namespace Pantheon.Core.DistributedServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RemoteMethodAttribute : Attribute
    {
        private MessageCode _callingCode;

        public bool AwaitResponse { get; set; }

        public MessageCode CallingCode
        {
            get { return _callingCode; }
        }

        public bool ReplySkipMessageCode { get; set; }

        public RemoteMethodAttribute(MessageCode callingCode)
        {
            _callingCode = callingCode;
            AwaitResponse = true;
            ReplySkipMessageCode = false;
        }

        public RemoteMethodAttribute(int callingCode)
            : this((MessageCode)callingCode)
        {
        }

        public RemoteMethodAttribute(object callingCode)
            : this((int)callingCode)
        {
        }
    }
}