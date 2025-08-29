using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Interfaces
{
    /// <summary>最小可編譯版本的泛型空間索引介面（供擴充/測試編過）</summary>
    public interface ISpatialIndex<T>
    {
        List<T> Query(Bounds bounds);
        List<T> QueryRange(Vector3 center, float radius);
        T QueryNearest(Vector3 point, float maxRadius = float.PositiveInfinity);
        void Optimize();
    }
}