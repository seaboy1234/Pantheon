using System;
using System.Reflection;

namespace Pantheon.Core.Event
{
    public class PropertyUpdatedEventArgs : EventArgs
    {
        private PropertyInfo _info;

        public PropertyInfo Property
        {
            get { return _info; }
        }

        public PropertyUpdatedEventArgs(PropertyInfo info)
        {
            _info = info;
        }
    }
}