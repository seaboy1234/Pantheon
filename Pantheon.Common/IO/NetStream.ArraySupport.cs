using System;
using Pantheon.Common.Utility;

namespace Pantheon.Common.IO
{
    public partial class NetStream
    {
        public T[] ReadValues<T>()
        {
            int count = ReadInt32();
            T[] values = new T[count];

            for (int i = 0; i < count; i++)
            {
                values[i] = ReadObject<T>();
            }

            return values;
        }

        public void WriteValues<T>(T[] values)
        {
            Throw.IfNull(values, "values");

            Write(values.Length);
            foreach (T value in values)
            {
                WriteObject(value);
            }
        }
    }
}