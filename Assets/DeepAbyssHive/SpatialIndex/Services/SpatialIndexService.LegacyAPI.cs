using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex.Services
{
    // 舊呼叫點期望回傳 List<SpatialNode> 的相容層
    public partial class SpatialIndexService
    {
        public List<SpatialNode> QueryRange(Vector3 center, Vector3 size, SpatialObjectType type = SpatialObjectType.All)
        {
            if (!IsInitialized) return new List<SpatialNode>();
            var nodes = _spatialIndex.QueryRange(center, size);
            if (type == SpatialObjectType.All) return nodes;
            var typeStr = type.ToString();
            return nodes.Where(n => n.Category.Equals(typeStr, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<SpatialNode> QueryAll(SpatialObjectType type = SpatialObjectType.All)
        {
            if (!IsInitialized) return new List<SpatialNode>();
            if (type == SpatialObjectType.All) return new List<SpatialNode>(_allNodes.Values);
            var typeStr = type.ToString();
            return _allNodes.Values.Where(n => n.Category.Equals(typeStr, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<SpatialNode> QueryNearest(Vector3 position, float maxDistance, int maxResults, SpatialObjectType type = SpatialObjectType.All)
        {
            if (!IsInitialized) return new List<SpatialNode>();

            // 簡易：用較大半徑抓候選，再排序取前 K
            var radius = Mathf.Min(maxDistance, 100f);
            var candidates = QueryRange(position, new Vector3(radius * 2, radius * 2, radius * 2), type);

            var list = candidates
                .Select(n => (n, dist: Vector3.Distance(position, n.Position)))
                .Where(t => t.dist <= maxDistance)
                .OrderBy(t => t.dist)
                .Take(maxResults)
                .Select(t => t.n)
                .ToList();

            return list;
        }

        public SpatialNode? GetNodeById(int id)
        {
            if (!IsInitialized) return null;
            return _allNodes.TryGetValue(id, out var node) ? node : (SpatialNode?)null;
        }
    }
}