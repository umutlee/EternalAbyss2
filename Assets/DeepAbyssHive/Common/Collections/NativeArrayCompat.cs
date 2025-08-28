using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace DeepAbyssHive.Common.Collections
{
    public static class NativeArrayCompat
    {
        public static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> src, Allocator allocator = Allocator.Temp) where T : struct
        {
            var arr = src as T[] ?? src.ToArray();
            var na = new NativeArray<T>(arr.Length, allocator, NativeArrayOptions.UninitializedMemory);
            na.CopyFrom(arr);
            return na;
        }
    }
}