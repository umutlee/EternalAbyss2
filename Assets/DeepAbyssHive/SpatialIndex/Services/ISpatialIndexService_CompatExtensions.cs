
using Unity.Collections;
using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// 對 ISpatialIndexService 的兼容擴充：補舊呼叫點會用到的方法（全回 default）
    public static class ISpatialIndexService_CompatExtensions
    {
        public static NativeArray<int> QueryAll(this ISpatialIndexService svc) => default;
        public static NativeArray<int> QueryRange(this ISpatialIndexService svc, Bounds bounds) => default;
    }
}
