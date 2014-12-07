using System;

namespace Pantheon.Client.Services
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcAttribute : Attribute
    {
        private int _callingCode;

        public int CallingCode
        {
            get { return _callingCode; }
        }

        public RpcAttribute(int callingCode)
        {
            _callingCode = callingCode;
        }

        public RpcAttribute(object callingCode)
        {
            _callingCode = Convert.ToInt32(callingCode);
        }
    }
}