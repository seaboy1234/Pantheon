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
        ///   Reads a number of bytes from this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="count">The number of bytes to read from the stream.</param>
        /// <returns>An array of bytes taken from the stream.</returns>
        public byte[] Read(int count)
        {
            byte[] buffer = new byte[count];
            Read(buffer, 0, count);
            return buffer;
        }

        /// <summary>
        ///   Reads a Boolean from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The boolean being read.</returns>
        public bool ReadBoolean()
        {
            return ReadByte() == 1;
        }

        /// <summary>
        ///   Reads a byte from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The byte being read.</returns>
        public new byte ReadByte()
        {
            byte[] buffer = Read(1);
            return buffer[0];
        }

        /// <summary>
        ///   Reads a number of bytes from this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="count">The number of bytes to read from the stream.</param>
        /// <returns>An array of bytes taken from the stream.</returns>
        public byte[] ReadBytes(int count)
        {
            return Read(count);
        }

        /// <summary>
        ///   Reads a <see cref="DateTime" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="DateTime" /> being read.</returns>
        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        public DisconnectCode ReadDisconnectCode()
        {
            return (DisconnectCode)ReadInt32();
        }

        /// <summary>
        ///   Reads a <see cref="Double" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="Double" /> being read.</returns>
        public double ReadDouble()
        {
            return BitConverter.ToDouble(ReadLittleEndian(sizeof(double)), 0);
        }

        /// <summary>
        ///   Reads a <see cref="Int16" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="Int16" /> being read.</returns>
        public short ReadInt16()
        {
            return BitConverter.ToInt16(ReadLittleEndian(sizeof(short)), 0);
        }

        /// <summary>
        ///   Reads an <see cref="Int32" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="Int32" /> being read.</returns>
        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadLittleEndian(sizeof(int)), 0);
        }

        /// <summary>
        ///   Reads an <see cref="Int64" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="Int64" /> being read.</returns>
        public long ReadInt64()
        {
            return BitConverter.ToInt64(ReadLittleEndian(sizeof(long)), 0);
        }

        public byte[] ReadLittleEndian(int count)
        {
            byte[] buffer = Read(count);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return buffer;
        }

        /// <summary>
        ///   Calls the correct read method for a given type.
        /// </summary>
        /// <param name="type">The type to read.</param>
        /// <returns>The object being read.</returns>
        public object ReadObject(Type type)
        {
            object value = null;

            // find read method
            MethodInfo readMethod;
            if (_ReadMethods.TryGetValue(type, out readMethod))
            {
                if (readMethod.IsStatic)
                {
                    value = readMethod.Invoke(null, new[] { this });
                }
                else
                {
                    value = readMethod.Invoke(this, null);
                }
            }
            return value;
        }

        /// <summary>
        ///   Calls the correct read method for a given type and casts the result to a given type.
        /// </summary>
        /// <typeparam name="T">The type to read and return.</typeparam>
        /// <returns>The object being read.</returns>
        public T ReadObject<T>()
        {
            return (T)ReadObject(typeof(T));
        }

        /// <summary>
        ///   Populates the properties of an object with the values stored in this
        ///   <see cref="NetStream" />
        /// </summary>
        /// <param name="target">The object that is being populated.</param>
        public void ReadProperties(object target)
        {
            ReadProperties(target, p => true);
        }

        /// <summary>
        ///   Populates the properties of the target object that meet a predicate with the values
        ///   stored in this <see cref="NetStream" /> .
        /// </summary>
        /// <param name="target">The object that is being populated</param>
        /// <param name="predicate">The predicate to test against.</param>
        public void ReadProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            Type tp = target.GetType();
            var flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var fields = tp.GetProperties(flags).Where(predicate);
            foreach (PropertyInfo fi in fields)
            {
                MethodInfo setMethod = fi.GetSetMethod();
                if (setMethod != null)
                {
                    object value = ReadObject(fi.PropertyType);
                    setMethod.Invoke(target, new object[] { value });
                }
            }
        }

        /// <summary>
        ///   Reads a signed byte from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="SByte" /> being read.</returns>
        public sbyte ReadSByte()
        {
            return ConvertBits.ToSByte(ReadByte());
        }

        /// <summary>
        ///   Reads a <see cref="Single" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="Single" /> being read.</returns>
        public float ReadSingle()
        {
            return BitConverter.ToSingle(ReadLittleEndian(sizeof(float)), 0);
        }

        /// <summary>
        ///   Reads a string from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The string being read.</returns>
        public string ReadString()
        {
            long position = Position;
            int length = ReadInt32();
            string value = Encoding.UTF8.GetString(ReadLittleEndian(length));
            return value;
        }

        public Type ReadType()
        {
            string fullName = ReadString();
            return Type.GetType(fullName);
        }

        /// <summary>
        ///   Reads a <see cref="UInt16" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="UInt16" /> being read.</returns>
        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadLittleEndian(sizeof(ushort)), 0);
        }

        /// <summary>
        ///   Reads a <see cref="UInt32" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="UInt32" /> being read.</returns>
        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadLittleEndian(sizeof(uint)), 0);
        }

        /// <summary>
        ///   Reads a <see cref="UInt64" /> from this <see cref="NetStream" /> .
        /// </summary>
        /// <returns>The <see cref="UInt64" /> being read.</returns>
        public ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadLittleEndian(sizeof(ulong)), 0);
        }

        /// <summary>
        /// Reads a dictionary of key-value pairs and returns the result.
        /// </summary>
        /// <typeparam name="K">The type of the key to read.  A reader must be implemented.</typeparam>
        /// <typeparam name="V">The type of the value to read. A reader must be implemented.</typeparam>
        /// <returns>A dictionary containing zero or more pars of keys and values.</returns>
        public IDictionary<K, V> ReadDictionary<K, V>()
        {
            if (!_ReadMethods.ContainsKey(typeof(K)))
            {
                throw new ArgumentException(string.Format("No reader for {0}", typeof(K).Name), "K");
            }

            if (!_ReadMethods.ContainsKey(typeof(V)))
            {
                throw new ArgumentException(string.Format("No reader for {0}", typeof(V).Name), "V");
            }
            Dictionary<K, V> values = new Dictionary<K, V>();

            int count = ReadInt32();
            for (int i = 0; i < count; i++)
            {
                values.Add(ReadObject<K>(), ReadObject<V>());
            }

            return values;
        }
    }
}