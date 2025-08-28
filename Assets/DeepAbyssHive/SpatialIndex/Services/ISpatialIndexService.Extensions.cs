using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// <summary>
    /// 針對 ISpatialIndexService 的實用擴充（回傳 NativeArray<int>，符合呼叫端需求）
    /// </summary>
    public static class ISpatialIndexServiceExtensions
    {
        /// <summary>舊呼叫：中心+半徑 → 直接轉呼叫介面 QueryRange(center, radius, type)</summary>
        public static NativeArray<int> QueryRangeIds(this ISpatialIndexService svc, Vector3 center, float radius, SpatialObjectType type = SpatialObjectType.All)
            => svc.QueryRange(center, radius, type);

        /// <summary>舊呼叫：所有指定類型 → 若為具體實作 SpatialIndexService，透過其 LegacyAPI 取得所有節點並取 Id；否則回傳空陣列。</summary>
        public static NativeArray<int> QueryAllIds(this ISpatialIndexService svc, SpatialObjectType type = SpatialObjectType.All)
        {
            if (svc is SpatialIndexService impl)
            {
                var list = impl.QueryAll(type).Select(n => n.Id);
                return DeepAbyssHive.Common.Collections.NativeArrayCompat.ToNativeArray(list);
            }
            return new NativeArray<int>(0, Allocator.Temp);
        }

        /// <summary>舊呼叫：最近 K 個 → 直接轉呼叫介面 QueryKNearest(position, k, type, maxDistance)</summary>
        public static NativeArray<int> QueryNearestIds(this ISpatialIndexService svc, Vector3 position, int maxResults = 1, SpatialObjectType type = SpatialObjectType.All, float maxDistance = float.MaxValue)
            => svc.QueryKNearest(position, maxResults, type, maxDistance);

        /// <summary>從實作取節點（僅在具體類別可用）；否則回傳 null。</summary>
        public static SpatialNode? GetNodeById(this ISpatialIndexService svc, int id)
        {
            if (svc is SpatialIndexService impl)
                return impl.GetNodeById(id);
            return null;
        }
    }
}
