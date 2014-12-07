using System;

namespace Pantheon.Client.Services
{
    public class EndpointAttribute : Attribute
    {
        private int _dataCode;

        public int ControlCode
        {
            get { return _dataCode; }
        }

        public EndpointAttribute(object controlCode)
        {
            _dataCode = (int)controlCode;
        }

        public EndpointAttribute(int controlCode)
        {
            _dataCode = controlCode;
        }
    }
}