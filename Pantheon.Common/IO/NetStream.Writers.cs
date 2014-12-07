using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Pantheon.Common.Utility;

namespace Pantheon.Common.IO
{
    public partial class NetStream
    {
        /// <summary>
        ///   Writes a buffer to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="offset">The offset to start from in <paramref name="buffer" /> .</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_lockWrites)
            {
                throw new ObjectDisposedException("NetStream");
            }

            long i;

            if (Position + count > _data.Length)
            {
                ResizeArray((int)(Position + count) + 16);
            }
            for (i = 0; i < count; i++)
            {
                _data[Position + i] = buffer[i + offset];
            }
            Position += i;
            _length += i;

            DataUpdated(this, Data);
        }

        /// <summary>
        ///   Writes an array of bytes to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="buffer">The array of bytes to write.</param>
        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///   Writes a string to this <see cref="NetStream" />
        /// </summary>
        /// <param name="value">The string to write.</param>
        public void Write(string value)
        {
            long position = Position;
            if (value == null)
            {
                value = string.Empty;
            }
            var data = Encoding.UTF8.GetBytes(value);

            Write(data.Length);
            WriteLittleEndian(data);
        }

        /// <summary>
        ///   Writes a boolean to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(bool value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a byte to this <see cref="NetStream" />
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(byte value)
        {
            WriteLittleEndian(new byte[] { value });
        }

        /// <summary>
        ///   Writes an <see cref="Int16" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(short value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes an <see cref="Int32" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(int value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes an <see cref="Int64" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(long value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="SByte" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(sbyte value)
        {
            Write(ConvertBits.ToByte(value));
        }

        /// <summary>
        ///   Writes a <see cref="UInt16" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ushort value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="UInt32" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(uint value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="UInt64" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(ulong value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="Single" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(float value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="Double" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(double value)
        {
            WriteLittleEndian(BitConverter.GetBytes(value));
        }

        /// <summary>
        ///   Writes a <see cref="DateTime" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(DateTime value)
        {
            Write(value.ToBinary());
        }

        /// <summary>
        ///   Writes the <see cref="P:Type.AssemblyQualifiedName" /> of a <see cref="Type" /> to
        ///   this <see cref="NetStream" />
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(Type value)
        {
            Throw.IfNull(value, "value");
            Write(value.AssemblyQualifiedName);
        }

        public void Write(DisconnectCode value)
        {
            Throw.IfEquals(value, DisconnectCode.Invalid, "value");
            Write((int)value);
        }

        /// <summary>
        ///   Writes a <see cref="NetStream" /> to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="other">The <see cref="NetStream" /> to write.</param>
        public void Write(NetStream other)
        {
            Throw.IfNull(other, "other");
            Write(other.Data);
        }

        public void WriteLittleEndian(byte[] buffer)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            Write(buffer);
        }

        /// <summary>
        ///   Writes an object to this <see cref="NetStream" /> . This method only has acces to
        ///   primitive types that are defined in this class.
        /// </summary>
        /// <param name="obj"></param>
        public bool WriteObject(object obj)
        {
            Throw.IfNull(obj, "obj");

            MethodInfo writeMethod;
            Type type = obj.GetType();
            if (type.Name == "RuntimeType")
            {
                type = typeof(Type); // woah, man.
            }

            if (type == typeof(IDictionary<,>))
            {
                var method = _Type.GetMethod("WriteDictionary");
                method = method.MakeGenericMethod(type.GetGenericArguments());
                _WriteMethods.Add(method.GetParameters().First().ParameterType, method);
            }

            if (_WriteMethods.TryGetValue(type, out writeMethod))
            {
                if (writeMethod.IsStatic)
                {
                    writeMethod.Invoke(null, new object[] { this, obj });
                }
                else
                {
                    writeMethod.Invoke(this, new object[] { obj });
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Writes all properties on an object to this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="target">The object to write the properties of.</param>
        public void WriteProperties(object target)
        {
            WriteProperties(target, p => true);
        }

        public void WriteProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            Throw.IfNull(target, "target");
            Throw.IfNull(predicate, "predicate");

            Type tp = target.GetType();

            var flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;

            var fields = tp.GetProperties(flags).Where(predicate);

            foreach (PropertyInfo fi in fields)
            {
                object value = fi.GetValue(target, null);

                WriteObject(value);
            }
        }

        public void WriteDictionary<K, V>(IDictionary<K, V> dictionary)
        {
            if (!_WriteMethods.ContainsKey(typeof(K)))
            {
                throw new ArgumentException(string.Format("No writer for {0}", typeof(K).Name), "K");
            }

            if (!_WriteMethods.ContainsKey(typeof(V)) && typeof(V) != typeof(object))
            {
                throw new ArgumentException(string.Format("No writer for {0}", typeof(V).Name), "V");
            }

            Write(dictionary.Count);
            foreach (var kvp in dictionary)
            {
                WriteObject(kvp.Key);
                WriteObject(kvp.Value);
            }
        }
    }
}