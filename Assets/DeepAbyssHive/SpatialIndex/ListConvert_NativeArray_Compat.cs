
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex
{
    /// 臨時：允許舊端把 NativeArray<int> 轉成節點清單（先回空，日後補真實映射）
    public static class SpatialIndexNativeArrayCompatExtensions
    {
        public static List<SpatialNode> ToSpatialNodes(this NativeArray<int> ids)
        {
            return new List<SpatialNode>(0);
        }
    }
}
