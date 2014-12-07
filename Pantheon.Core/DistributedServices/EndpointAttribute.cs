using System;

namespace Pantheon.Core.DistributedServices
{
    public class EndpointAttribute : Attribute
    {
        private int _callingCode;

        public int CallingCode
        {
            get { return _callingCode; }
        }

        public bool RequireReturnPath { get; set; }

        public EndpointAttribute(object callingCode)
        {
            _callingCode = Convert.ToInt32(callingCode);
        }
    }
}