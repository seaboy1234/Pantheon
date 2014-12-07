using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pantheon.Common;

namespace Pantheon.Core.MessageRouting
{
    internal static class MessageRouterFactory
    {
        internal static IEnumerable<MessageHandlerInfo> GenerateMessageHandlers(object target)
        {
            Type type = target.GetType();
            var messageHandlers = new List<MessageHandlerInfo>();

            var methods = type.GetMethods()
                              .Where(m => m.HasAttribute<MessageHandlerAttribute>());
            foreach (var method in methods)
            {
                messageHandlers.Add(GenerateMessageHandlerInfo(target, method));
            }

            return messageHandlers;
        }

        internal static IEnumerable<MessageHandlerInfo> GenerateMessageHandlers(Type type)
        {
            var messageHandlers = new List<MessageHandlerInfo>();

            var methods = type.GetRuntimeMethods()
                              .Where(m => m.HasAttribute<MessageHandlerAttribute>() && m.IsStatic);
            foreach (var method in methods)
            {
                messageHandlers.Add(GenerateMessageHandlerInfo(null, method));
            }

            return messageHandlers;
        }

        private static MessageHandlerInfo GenerateMessageHandlerInfo(object target, MethodInfo method)
        {
            var paramaters = method.GetParameters();
            if (method.ReturnType != typeof(void) ||
                method.IsConstructor ||
                method.IsAbstract ||
                method.IsGenericMethod ||
                paramaters.Count() != 1 ||
                (!paramaters[0].ParameterType.IsSubclassOf(typeof(Message))
                 && paramaters[0].ParameterType != typeof(Message)))
            {
                string message = "The given method is not a valid MessageHandler: " + method.MethodSignature();
                message += "\r\n";
                List<string> errors = new List<string>();
                if (method.IsConstructor)
                {
                    errors.Add("Method must not be a constructor");
                }
                if (method.IsAbstract)
                {
                    errors.Add("Method must not be abstract");
                }
                if (method.IsGenericMethod)
                {
                    errors.Add("Method must not be generic");
                }
                if (paramaters.Count() != 1)
                {
                    errors.Add("Method must accept only one paramater");
                }
                if (paramaters.Count() > 0 && !paramaters[0].ParameterType.IsSubclassOf(typeof(Message)))
                {
                    errors.Add("Method paramater must be of type Pantheon.Core.Message");
                }
                message += string.Join(", ", errors) + ".";
                throw new InvalidMessageHandlerException(message);
            }

            var attributes = method.GetCustomAttributes<MessageHandlerAttribute>();

            List<ulong> channels = new List<ulong>();
            foreach (var attribute in attributes)
            {
                channels.AddRange(attribute.Channels);
            }

            return new MessageHandlerInfo(target, method, channels);
        }
    }
}