using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;
using SIRaycastHit = DeepAbyssHive.SpatialIndex.Data.RaycastHit;

namespace DeepAbyssHive.SpatialIndex
{
    /// <summary>
    /// 舊 Query 類的兼容擴充：統一回傳 List<SpatialNode>。
    /// 若專案中已有 Query 類，這裡以 partial 追加；若沒有，這個檔案會提供最小可用版本。
    /// </summary>
    public partial class Query
    {
        public List<SpatialNode> Raycast(Vector3 origin, Vector3 direction, float maxDistance)
            => SpatialIndexCompat.Raycast(origin, direction, maxDistance);

        public List<SpatialNode> Range(Vector3 center, float radius)
            => SpatialIndexCompat.Range(center, radius);

        public List<SpatialNode> Nearest(Vector3 position, int k = 1, float maxDistance = float.PositiveInfinity)
            => SpatialIndexCompat.Nearest(position, k, maxDistance);
    }
}