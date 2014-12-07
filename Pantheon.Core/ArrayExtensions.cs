using System;

namespace Pantheon.Core
{
    public static class ArrayExtensions
    {
        public static void Add<T>(this T[] array, T item)
        {
            T[] newArray = new T[] { item };
            int length = array.Length;
            Array.Resize<T>(ref array, length + newArray.Length);
            array[array.Length] = item;
        }
    }
}