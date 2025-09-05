using System;
using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 統一的放置驗證器，支援 Physics 檢查與未來的 SpatialIndex 整合
    /// </summary>
    public class PlacementValidator
    {
        private readonly GameConfigSO config;

        public PlacementValidator(GameConfigSO config = null)
        {
            this.config = config ?? GameConfigProvider.Current;
        }

        /// <summary>
        /// 驗證指定位置和邊界是否可以放置物件
        /// </summary>
        /// <param name="center">檢查中心點</param>
        /// <param name="halfExtents">邊界半尺寸</param>
        /// <param name="rotation">旋轉</param>
        /// <param name="layerMask">檢查的圖層遮罩</param>
        /// <param name="additionalChecks">額外的驗證條件（可選）</param>
        /// <returns>驗證結果</returns>
        public Result<Bounds> ValidatePlacement(
            Vector3 center, 
            Vector3 halfExtents, 
            Quaternion rotation, 
            int layerMask,
            Predicate<Vector3> additionalChecks = null)
        {
            // 應用配置的邊界擴展
            var expandedHalf = halfExtents + Vector3.one * config.margin;
            
            // Physics 碰撞檢查
            var hits = Physics.OverlapBox(center, expandedHalf, rotation, layerMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                return PlacementResults.Collision($"與 {hits[0].name} 發生碰撞");
            }

            // 額外檢查條件（如菌毯要求等）
            if (additionalChecks != null && !additionalChecks(center))
            {
                return PlacementResults.RequireCreep("不符合額外放置條件");
            }

            // 返回成功結果，包含實際使用的邊界
            var resultBounds = new Bounds(center, expandedHalf * 2);
            return PlacementResults.OkBounds(resultBounds);
        }

        /// <summary>
        /// 簡化版本：僅 Physics 檢查，用於快速替換現有代碼
        /// </summary>
        public bool IsBlocked(Vector3 center, Vector3 halfExtents, Quaternion rotation, int layerMask)
        {
            var expandedHalf = halfExtents + Vector3.one * config.margin;
            var hits = Physics.OverlapBox(center, expandedHalf, rotation, layerMask, QueryTriggerInteraction.Ignore);
            return hits != null && hits.Length > 0;
        }
    }
}