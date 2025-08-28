
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// <summary>對 ISpatialIndexService 的兼容擴充：提供實際橋接</summary>
    public static class ISpatialIndexService_CompatExtensions
    {
        public static NativeArray<int> QueryAll(this ISpatialIndexService svc)
        {
            if (svc is SpatialIndexService impl)
            {
                var list = impl.QueryAll(DeepAbyssHive.SpatialIndex.Enums.SpatialObjectType.All).Select(n => n.Id);
                return DeepAbyssHive.Common.Collections.NativeArrayCompat.ToNativeArray(list);
            }
            return new NativeArray<int>(0, Allocator.Temp);
        }

        public static NativeArray<int> QueryRange(this ISpatialIndexService svc, Bounds bounds)
            => svc.QueryBounds(bounds, DeepAbyssHive.SpatialIndex.Enums.SpatialObjectType.All);
    }
}
