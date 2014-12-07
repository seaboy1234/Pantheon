using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Pantheon.Common;
using Pantheon.Common.IO;

namespace Pantheon.Client.PacketRouting
{
    internal static class PacketRouterFactory
    {
        public static IEnumerable<PacketHandlerInfo> GenerateHandlersFor(Type type)
        {
            var handlers = new List<PacketHandlerInfo>();
            var methods = type.GetMethods()
                              .Where(m => m.HasAttribute<PacketHandlerAttribute>());

            foreach (var method in methods)
            {
                handlers.Add(GeneratePacketHandler(method, null));
            }
            return handlers;
        }

        public static IEnumerable<PacketHandlerInfo> GenetateHandlersFor(object target)
        {
            var type = target.GetType();
            var handlers = new List<PacketHandlerInfo>();
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance)
                              .Where(m => m.HasAttribute<PacketHandlerAttribute>());

            foreach (var method in methods)
            {
                handlers.Add(GeneratePacketHandler(method, target));
            }
            return handlers;
        }

        private static PacketHandlerInfo GeneratePacketHandler(MethodInfo method, object target)
        {
            var paramaters = method.GetParameters();
            bool fail = method.ReturnType != typeof(void) ||
                        method.IsConstructor ||
                        method.IsAbstract ||
                        method.IsGenericMethod;

            if (!fail)
            {
                if (paramaters.Count() == 2)
                {
                    fail = !(paramaters.Count() > 1
                           && paramaters[0].ParameterType == typeof(INetworkManager)
                           && paramaters[0].ParameterType.IsSubclassOf(typeof(INetworkManager))
                           && paramaters[1].ParameterType.IsSubclassOf(typeof(IPacket))
                           && paramaters[1].ParameterType == typeof(NetStream));
                }
                else if (paramaters.Count() == 1)
                {
                    fail = (!paramaters[0].ParameterType.IsSubclassOf(typeof(IPacket))
                           || paramaters[0].ParameterType != typeof(NetStream));
                }
            }
            if (fail)
            {
                string message = "The given method is not a valid PacketHandler: "
                                 + method.MethodSignature();
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
                if (paramaters.Count() > 1
                    && paramaters[0].ParameterType != typeof(INetworkManager)
                    && !paramaters[0].ParameterType.IsSubclassOf(typeof(INetworkManager))
                    && !paramaters[1].ParameterType.IsSubclassOf(typeof(IPacket))
                    && paramaters[1].ParameterType != typeof(NetStream))
                {
                    const string msg = @"First Method paramater must be of type Pantheon.Common.INetworkManager";
                    errors.Add(msg);
                }
                if (paramaters.Count() > 0
                    && paramaters.Last().ParameterType.GetInterface(typeof(IPacket).FullName) == null
                    && paramaters.Last().ParameterType != typeof(NetStream))
                {
                    const string msg = @"Method paramater must be of type Pantheon.Client.Core.IPacket or Pantheon.Common.IO.NetStream";
                    errors.Add(msg);
                }
                if (errors.Count != 0)
                {
                    message += string.Join(", ", errors.ToArray()) + ".";
                    throw new InvalidOperationException(message);
                }
            }

            var attribute = method.GetAttribute<PacketHandlerAttribute>();
            var info = new PacketHandlerInfo(method, attribute, target);
            return info;
        }
    }
}