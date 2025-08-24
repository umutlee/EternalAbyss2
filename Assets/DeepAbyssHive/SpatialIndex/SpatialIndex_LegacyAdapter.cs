using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;
using SIRaycastHit = DeepAbyssHive.SpatialIndex.Data.RaycastHit;

namespace DeepAbyssHive.SpatialIndex
{
    /// <summary>
    /// 契約對齊的集中入口。呼叫端直接用這組簽名即可。
    /// 內部由 ISpatialIndexShim 提供真實實作；未註入時為安全 no-op / 空清單。
    /// </summary>
    public static class SpatialIndexCompat
    {
        public static void Insert(object id, Vector3 pos, Vector3 size)
            => SpatialIndexShimProvider.Service?.Insert(id, pos, size);

        public static void Update(object id, Vector3 pos, Vector3 size, Vector3? oldPos = null)
            => SpatialIndexShimProvider.Service?.Update(id, pos, size, oldPos);

        public static void Remove(object id, Vector3 pos, Vector3 size)
            => SpatialIndexShimProvider.Service?.Remove(id, pos, size);

        public static List<SpatialNode> Raycast(Vector3 origin, Vector3 direction, float maxDistance)
            => SpatialIndexShimProvider.Service?.Raycast(origin, direction, maxDistance) ?? new List<SpatialNode>(0);

        public static List<SpatialNode> Range(Vector3 center, float radius)
            => SpatialIndexShimProvider.Service?.Range(center, radius) ?? new List<SpatialNode>(0);

        public static List<SpatialNode> Nearest(Vector3 position, int k = 1, float maxDistance = float.PositiveInfinity)
            => SpatialIndexShimProvider.Service?.Nearest(position, k, maxDistance) ?? new List<SpatialNode>(0);
    }

    /// <summary>
    /// 由既有空間索引系統以 adapter/partial 實作並註入。
    /// </summary>
    public interface ISpatialIndexShim
    {
        void Insert(object id, Vector3 pos, Vector3 size);
        void Update(object id, Vector3 pos, Vector3 size, Vector3? oldPos);
        void Remove(object id, Vector3 pos, Vector3 size);
        List<SpatialNode> Raycast(Vector3 origin, Vector3 direction, float maxDistance);
        List<SpatialNode> Range(Vector3 center, float radius);
        List<SpatialNode> Nearest(Vector3 position, int k, float maxDistance);
    }

    /// <summary>
    /// Service Provider。遊戲啟動時或測試中註入具體 shim。
    /// </summary>
    public static class SpatialIndexShimProvider
    {
        public static ISpatialIndexShim Service { get; set; }
    }
}