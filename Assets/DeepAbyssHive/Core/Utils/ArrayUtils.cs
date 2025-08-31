using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepAbyssHive.Core.Utils
{
    public static class ArrayUtils
    {
        public static T[] Append<T>(T[] source, T item)
        {
            if (source == null) return new T[] { item };
            var list = source.ToList();
            list.Add(item);
            return list.ToArray();
        }
    }
}