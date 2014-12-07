using System;
using System.Collections.Generic;

namespace Pantheon.Common
{
    public static class QueueExtensions
    {
        public static IEnumerable<T> DequeueWhile<T>(this Queue<T> queue, Predicate<T> predicate)
        {
            List<T> items = new List<T>();
            while (queue.Count > 0 && predicate(queue.Peek()))
            {
                items.Add(queue.Dequeue());
            }

            return items;
        }
    }
}