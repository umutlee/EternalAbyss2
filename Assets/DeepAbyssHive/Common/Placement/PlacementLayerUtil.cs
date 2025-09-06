using UnityEngine;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 統一管理放置相關的圖層遮罩計算，避免各檔案各自手寫而跑偏。
    /// </summary>
    public static class PlacementLayerUtil
    {
        /// <summary>
        /// 放置驗證（Physics/MinSpacing）使用的遮罩：
        /// 取 ~0，排除 Terrain 與 Ignore Raycast；其他交由場景與 Prefab 決定。
        /// </summary>
        public static int GetPlacementBlockMask()
        {
            int mask = ~0;
            int terrain = LayerMask.NameToLayer("Terrain");
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (terrain >= 0) mask &= ~(1 << terrain);
            if (ignore  >= 0) mask &= ~(1 << ignore);
            return mask;
        }

        /// <summary>
        /// 僅含 Building 層（用於刪除工具的 Raycast）。
        /// 若找不到 Building 層則回傳 0（表示不命中任何物件）。
        /// </summary>
        public static int GetBuildingOnlyMask()
        {
            int building = LayerMask.NameToLayer("Building");
            if (building < 0) return 0;
            return 1 << building;
        }
    }
}