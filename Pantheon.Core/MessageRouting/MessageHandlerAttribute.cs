using System;
using System.Collections.Generic;
using System.Linq;

namespace Pantheon.Core.MessageRouting
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class MessageHandlerAttribute : Attribute
    {
        private ulong[] _channels;

        public ulong[] Channels { get { return _channels; } }

        public MessageHandlerAttribute(ulong channel)
        {
            _channels = new[] { channel };
        }

        public MessageHandlerAttribute(IEnumerable<ulong> channels)
        {
            _channels = channels.ToArray();
        }
    }
}