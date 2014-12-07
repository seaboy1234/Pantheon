using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pantheon.Core.MessageRouting
{
    public class MessageHandlerInfo
    {
        private IEnumerable<ulong> _channels;
        private MethodInfo _method;
        private object _target;

        public IEnumerable<ulong> Channels
        {
            get { return _channels; }
        }

        public MethodInfo Method
        {
            get { return _method; }
        }

        public object Target
        {
            get { return _target; }
        }

        public MessageHandlerInfo(object target, MethodInfo method, IEnumerable<ulong> channels)
        {
            _channels = channels;
            _method = method;
            _target = target;
        }

        public MessageHandlerInfo(Action<Message> method, IEnumerable<ulong> channels)
        {
            _method = method.Method;
            _target = method.Target;
            _channels = channels;
        }

        public Exception CallMethod(Message message)
        {
            try
            {
                _method.Invoke(_target, new[] { message });
                return null;
            }
            catch (TargetInvocationException e)
            {
                return e.InnerException;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}