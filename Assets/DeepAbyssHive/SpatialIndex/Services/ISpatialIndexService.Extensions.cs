using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Enums;

// 透過擴充方法補上 *Ids 版本查詢與 GetNodeById，不用改介面
namespace DeepAbyssHive.SpatialIndex.Services
{
    public static class ISpatialIndexServiceExtensions
    {
        public static IEnumerable<int> QueryRangeIds(
            this ISpatialIndexService svc,
            Vector3 center, Vector3 size,
            SpatialObjectType type = SpatialObjectType.All)
        {
            var nodes = svc?.QueryRange(center, size, type);
            return nodes != null ? nodes.Select(n => n.Id) : Enumerable.Empty<int>();
        }

        public static IEnumerable<int> QueryAllIds(
            this ISpatialIndexService svc,
            SpatialObjectType type = SpatialObjectType.All)
        {
            var nodes = svc?.QueryAll(type);
            return nodes != null ? nodes.Select(n => n.Id) : Enumerable.Empty<int>();
        }

        public static IEnumerable<int> QueryNearestIds(
            this ISpatialIndexService svc,
            Vector3 position, float maxDistance, int maxResults,
            SpatialObjectType type = SpatialObjectType.All)
        {
            var nodes = svc?.QueryNearest(position, maxDistance, maxResults, type);
            return nodes != null ? nodes.Select(n => n.Id) : Enumerable.Empty<int>();
        }

        public static SpatialNode? GetNodeById(this ISpatialIndexService svc, int id)
        {
            var all = svc?.QueryAll(SpatialObjectType.All);
            if (all == null) return null;
            foreach (var n in all)
                if (n.Id == id) return n;
            return null;
        }
    }
}