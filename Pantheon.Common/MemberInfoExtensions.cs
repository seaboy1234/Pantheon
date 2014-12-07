using System;
using System.Linq;
using System.Reflection;

namespace Pantheon.Common
{
    public static class MemberInfoExtensions
    {
        public static PropertyInfo[] GetAllProperties(this Type type)
        {
            var properties = type.GetInterfaces().SelectMany(i => i.GetProperties());
            properties = properties.Concat(type.GetProperties());
            properties = properties.Distinct();
            return properties.ToArray();
        }

        public static PropertyInfo GetAnyProperty(this Type type, string name)
        {
            return GetAllProperties(type).FirstOrDefault(p => p.Name == name);
        }

        public static object GetAttribute(this MemberInfo member, Type type)
        {
            return member.GetCustomAttributes(type, true).FirstOrDefault();
        }

        public static T GetAttribute<T>(this MemberInfo member)
        {
            return (T)GetAttribute(member, typeof(T));
        }

        public static object GetAttribute(this Assembly assembly, Type type)
        {
            return assembly.GetCustomAttributes(type, true).FirstOrDefault();
        }

        public static T GetAttribute<T>(this Assembly assembly)
        {
            return (T)GetAttribute(assembly, typeof(T));
        }

        public static object[] GetAttributes(this Assembly assembly, Type type)
        {
            return assembly.GetCustomAttributes(type, true);
        }

        public static T[] GetAttributes<T>(this Assembly assembly)
        {
            return (T[])GetAttribute(assembly, typeof(T));
        }

        public static MemberInfo[] GetInterfaceMembers(this Type type, string name)
        {
            var members = type.GetInterfaces().SelectMany(i => i.GetMembers());
            return members.Where(m => m.Name == name).ToArray();
        }

        public static MethodInfo[] GetInterfaceMembers(this Type type)
        {
            return type.GetInterfaces().SelectMany(i => i.GetMethods()).ToArray();
        }

        public static MethodInfo GetInterfaceMethod(this Type type, string name)
        {
            var methods = type.GetInterfaces().SelectMany(i => i.GetMethods());
            return methods.Where(m => m.Name == name).FirstOrDefault();
        }

        public static MethodInfo[] GetInterfaceMethods(this Type type)
        {
            return type.GetInterfaces().SelectMany(i => i.GetMethods()).ToArray();
        }

        public static PropertyInfo[] GetInterfaceProperties(this Type type)
        {
            return type.GetInterfaces().SelectMany(i => i.GetProperties()).ToArray();
        }

        public static PropertyInfo GetInterfaceProperty(this Type type, string name)
        {
            var properties = type.GetInterfaces().SelectMany(i => i.GetProperties());
            return properties.Where(p => p.Name == name).FirstOrDefault();
        }

        public static object GetValue(this PropertyInfo info, object target)
        {
            return info.GetValue(target, null);
        }

        public static bool HasAttribute(this MemberInfo member, Type attributeType)
        {
            return member.GetCustomAttributes(attributeType, true).Count() > 0;
        }

        public static bool HasAttribute<T>(this MemberInfo member)
        {
            return HasAttribute(member, typeof(T));
        }

        public static bool Is(this Type type, Type other)
        {
            return other == type || other.IsSubclassOf(type) || other.IsAssignableFrom(type);
        }

        public static string MethodSignature(this MethodInfo mi)
        {
            String[] param = mi.GetParameters()
                          .Select(p => String.Format("{0} {1}", p.ParameterType.Name, p.Name))
                          .ToArray();

            string signature = String.Format("{0} {1}({2})", mi.ReturnType.Name, mi.Name, String.Join(",", param));

            return signature;
        }

        public static void SetValue(this PropertyInfo info, object target, object value)
        {
            info.SetValue(target, value, null);
        }
    }
}