using System.Collections.Generic;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex
{
    /// <summary>
    /// 臨時：將舊 List<object> 轉為 List<SpatialNode>。
    /// 僅在過渡期使用；之後應全面切到新的 List<SpatialNode> API。
    /// </summary>
    public static class SpatialIndexLegacyListExtensions
    {
        public static List<SpatialNode> ToSpatialNodes(this List<object> legacy)
        {
            var result = new List<SpatialNode>(legacy == null ? 0 : legacy.Count);
            if (legacy == null) return result;
            for (int i = 0; i < legacy.Count; i++)
            {
                // 只有當舊清單本來就存放 SpatialNode 時才收錄；避免猜測欄位造成編譯/序列化風險。
                if (legacy[i] is SpatialNode sn) result.Add(sn);
            }
            return result;
        }
    }
}