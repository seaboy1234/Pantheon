using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Pantheon.Common.IO
{
    /// <summary>
    ///   General purpose wrapper around a byte array.
    /// </summary>
    public partial class NetStream : Stream
    {
        private static readonly MethodInfo _ArrayReader;
        private static readonly MethodInfo _ArrayWriter;
        private static readonly Dictionary<Type, MethodInfo> _ReadMethods;
        private static readonly Type _Type;
        private static readonly Dictionary<Type, MethodInfo> _WriteMethods;
        private byte[] _data;
        private long _length;
        private bool _lockReads;
        private bool _lockWrites;

        /// <summary>
        ///   Gets whether or not this <see cref="NetStream" /> is able to read.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        ///   Gets whether this <see cref="NetStream" /> can seek.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        ///   Gets whether this <see cref="NetStream" /> can be written to.
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        ///
        /// </summary>
        public virtual byte[] Data
        {
            get
            {
                byte[] data = new byte[_length];
                for (int i = 0; i < _length; i++)
                {
                    data[i] = _data[i];
                }
                return data;
            }
            set
            {
                _data = value;
                _length = _data.Length;
                Position = 0;
            }
        }

        /// <summary>
        ///   Gets the length of this <see cref="NetStream" /> 's buffer.
        /// </summary>
        public override long Length
        {
            get { return _length; }
        }

        /// <summary>
        ///   Gets or sets the current position to read or write from in the buffer.
        /// </summary>
        public override long Position { get; set; }

        public event Action<NetStream, byte[]> DataUpdated = delegate { };

        static NetStream()
        {
            _Type = typeof(NetStream);
            _ArrayWriter = _Type.GetMethod("WriteValues");
            _ArrayReader = _Type.GetMethod("ReadValues");

            _ReadMethods = new Dictionary<Type, MethodInfo>();
            MethodInfo[] methods = typeof(NetStream).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.GetParameters().Length == 0 && mi.Name.StartsWith("Read", StringComparison.InvariantCulture) && mi.Name.Substring(4) == mi.ReturnType.Name)
                {
                    _ReadMethods[mi.ReturnType] = mi;
                    _ReadMethods[mi.ReturnType.MakeArrayType()] = _ArrayReader.MakeGenericMethod(mi.ReturnType);
                }
            }

            _WriteMethods = new Dictionary<Type, MethodInfo>();
            methods = typeof(NetStream).GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = mi.GetParameters();
                    if (pis.Length == 1)
                    {
                        _WriteMethods[pis[0].ParameterType] = mi;
                        _WriteMethods[pis[0].ParameterType.MakeArrayType()] = _ArrayWriter.MakeGenericMethod(pis[0].ParameterType);
                    }
                }
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="NetStream" /> class with an empty buffer.
        /// </summary>
        public NetStream()
        {
            _data = new byte[0];
            _length = 0;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="NetStream" /> class and writes the
        ///   contents of <paramref name="buffer" /> to the stream.
        /// </summary>
        /// <param name="buffer">An array of bytes to write to the buffer.</param>
        public NetStream(byte[] buffer)
            : this()
        {
            Write(buffer);
            Position = 0;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="NetStream" /> class and copies the
        ///   contents of <paramref name="other" /> to the stream.
        /// </summary>
        /// <param name="other">The stream to copy.</param>
        public NetStream(NetStream other)
            : this()
        {
            other.CopyTo(this);
            Position = 0;
        }

        public static void AddDataHandler<T>(Func<NetStream, T> reader, Action<NetStream, T> writer)
        {
            if (reader.Target != null || writer.Target != null)
            {
                throw new ArgumentException("Provided methods must be static!");
            }
            _ReadMethods.Add(typeof(T), reader.Method);
            _WriteMethods.Add(typeof(T), writer.Method);

            _ReadMethods.Add(typeof(T[]), _ArrayReader.MakeGenericMethod(typeof(T)));
            _WriteMethods.Add(typeof(T[]), _ArrayWriter.MakeGenericMethod(typeof(T)));
        }

        public static void AddDataHandlers(Type type)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == typeof(NetStream) && mi.Name.StartsWith("Read", StringComparison.InvariantCulture) && mi.Name.Substring(4) == mi.ReturnType.Name)
                {
                    _ReadMethods[mi.ReturnType] = mi;
                    _ReadMethods[mi.ReturnType.MakeArrayType()] = _ArrayReader.MakeGenericMethod(mi.ReturnType);
                }
            }

            methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                if (mi.Name.Equals("Write", StringComparison.InvariantCulture))
                {
                    ParameterInfo[] pis = mi.GetParameters();
                    if (pis.Length == 2 && mi.GetParameters()[0].ParameterType == typeof(NetStream))
                    {
                        _WriteMethods[pis[1].ParameterType] = mi;
                        _WriteMethods[pis[1].ParameterType.MakeArrayType()] = _ArrayWriter.MakeGenericMethod(pis[1].ParameterType);
                    }
                }
            }
        }

        public override void Close()
        {
            _lockWrites = true;
            _lockReads = true;
        }

        public virtual void CopyTo(NetStream other)
        {
            long pos = other.Position;
            other.Write(Data);
            other.Position = pos;
        }

        /// <summary>
        ///   Flushes this <see cref="NetStream" /> 's buffer. This method is not supported.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Reads a number of bytes from the buffer, advancing the <see cref="Position" /> of the
        ///   stream.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        /// <param name="offset">
        ///   The offset to start at inside <paramref name="buffer" /> .
        /// </param>
        /// <param name="count">The number of bytes from the stream to read.</param>
        /// <returns>
        ///   The number of bytes read. If this is less than <paramref name="count" /> , the end of
        ///   the stream has been reached.
        /// </returns>
        /// <exception cref="EndOfStreamException">
        ///   When the current position is greater then the length of the stream.
        /// </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            //if (_lockReads)
            //{
            //    throw new ObjectDisposedException("NetStream");
            //}
            long i;
            if (Position >= _length)
            {
                throw new EndOfStreamException();
            }

            for (i = 0; i < count; i++)
            {
                if (Position + i >= Length)
                {
                    break;
                }
                buffer[i + offset] = _data[Position + i];
            }
            Position += i;
            return (int)i;
        }

        /// <summary>
        ///   Seeks to a position in this <see cref="NetStream" /> . This method is not supported.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Resizes this <see cref="NetStream" /> to a given value.
        /// </summary>
        /// <param name="value">The new size to resize the array to.</param>
        public override void SetLength(long value)
        {
            if (_length < value)
            {
                ResizeArray((int)value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            //base.Dispose(disposing);
            if (disposing)
            {
                //_data = null;
                //_length = 0;
                //_lockReads = true;
                //_lockWrites = true;
            }
        }

        private void ResizeArray(int size)
        {
            byte[] data = new byte[size];

            _data.CopyTo(data, 0);
            _data = data;
        }
    }
}