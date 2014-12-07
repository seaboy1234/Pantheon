using System;
using System.Runtime.CompilerServices;

namespace Pantheon.Common.Utility
{
    public static class Throw
    {
        public static void If(bool condition, string conditionName, string className)
        {
            if (condition)
            {
                const string format = "{0} is in an invalid state ({1} is {2})";
                throw new InvalidOperationException(string.Format(format, className, conditionName, condition));
            }
        }

        public static void IfDisposed(bool isDisposed, string className)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(className);
            }
        }

        public static void IfEmpty(string value, string parameterName)
        {
            IfNull(value, parameterName);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(parameterName, "value cannot be empty");
            }
        }

        public static void IfEquals(object obj, object other, string parameterName)
        {
            if (obj == other)
            {
                throw new ArgumentException(parameterName, "cannot use value: " + obj.ToString());
            }
        }

        public static void IfNull(object obj, string parameterName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(parameterName, "input cannot be null");
            }
        }

        public static void IfValueNotGreaterThan(long value, long other, string parameterName)
        {
            if (value <= other)
            {
                string message = string.Format("value is out of range {0}.  Given value: {1}", other, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        public static void IfValueNotGreaterThan(ulong value, ulong other, string parameterName)
        {
            if (value <= other)
            {
                string message = string.Format("value is out of range {0}.  Given value: {1}", other, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        public static void IfValueNotInRange(long value, long min, long max, string parameterName)
        {
            if (value > max || value < min)
            {
                string message = string.Format("value is out of range {0}, {1}.  Given value: {2}", min, max, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        public static void IfValueNotInRange(ulong value, ulong min, ulong max, string parameterName)
        {
            if (value > max || value < min)
            {
                string message = string.Format("value is out of range {0}, {1}.  Given value: {2}", min, max, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        public static void IfValueNotLessThan(long value, long other, string parameterName)
        {
            if (value >= other)
            {
                string message = string.Format("value is out of range {0}.  Given value: {1}", other, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }

        public static void IfValueNotLessThan(ulong value, ulong other, string parameterName)
        {
            if (value >= other)
            {
                string message = string.Format("value is out of range {0}.  Given value: {1}", other, value);
                throw new ArgumentOutOfRangeException(parameterName, message);
            }
        }
    }
}