using System;
using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Common.Placement;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 統一的放置驗證器，支援 Physics 檢查與 SpatialIndex/Creep 整合
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>SpatialIndex 並聯檢查：true 表示通過（無衝突）</summary>
        public static Func<Bounds, LayerMask, float, bool> SpatialIndexPredicate;
        /// <summary>菌毯要求：true 表示該 Bounds 位於菌毯覆蓋內</summary>
        public static Func<Bounds, bool> RequireCreepPredicate;
        /// <summary>邊界檢查：true 表示超界（不可放）</summary>
        public static Func<Bounds, bool> OutOfBoundsPredicate;

        /// <summary>最後一次驗證結果（給 Dev HUD 用）</summary>
        public static Result<Bounds> LastResult { get; private set; }
        public static bool HasSpatialIndex => SpatialIndexPredicate != null;
        public static bool HasRequireCreep => RequireCreepPredicate != null;
        public static bool HasOutOfBounds => OutOfBoundsPredicate != null;

        /// <summary>
        /// 驗證指定位置和邊界是否可以放置物件
        /// </summary>
        public static Result<Bounds> ValidatePlacement(Vector3 center, Vector3 size, LayerMask blockMask)
        {
            var cfg = GameConfigProvider.Current;
            var margin = cfg.margin;
            var bounds = new Bounds(center, size + Vector3.one * margin);

            // 1) Out-of-bounds（可選）
            if (OutOfBoundsPredicate != null && OutOfBoundsPredicate(bounds))
            {
                LastResult = PlacementResults.OutOfBounds("[Placement] Out of bounds");
                return LastResult;
            }

            // 2) Physics 檢查（必要）
            if (HasPhysicsCollision(bounds, blockMask))
            {
                LastResult = PlacementResults.Collision("[Placement] Physics collision");
                return LastResult;
            }

            // 3) SpatialIndex 並聯（可選）
            if (cfg.useSpatialIndexForPlacement)
            {
                if (SpatialIndexPredicate != null)
                {
                    if (!SpatialIndexPredicate(bounds, blockMask, margin))
                    {
                        LastResult = PlacementResults.Collision("[Placement] SpatialIndex collision");
                        return LastResult;
                    }
                }
                else
                {
                    Debug.LogWarning("[DEV HUD] useSpatialIndexForPlacement=true，但尚未提供 SpatialIndexPredicate；暫以 Physics 結果為準。");
                }
            }

            // 3.5) 最小間距（獨立於 margin 的規則）
            if (cfg.minSpacing > 0f && ViolatesMinSpacing(new Bounds(center, size), cfg.minSpacing, blockMask))
            {
                LastResult = PlacementResults.Collision("[Placement] MinSpacing violation");
                return LastResult;
            }

            // 4) 菌毯要求（可選）
            if (cfg.requireCreep && RequireCreepPredicate != null)
            {
                if (!RequireCreepPredicate(bounds))
                {
                    LastResult = PlacementResults.RequireCreep("[Placement] Require creep");
                    return LastResult;
                }
            }

            LastResult = PlacementResults.OkBounds(bounds);
            return LastResult;
        }

        private static bool HasPhysicsCollision(Bounds b, LayerMask blockMask)
        {
            var hits = Physics.OverlapBox(
                b.center,
                b.extents,
                Quaternion.identity,
                blockMask,
                QueryTriggerInteraction.Ignore
            );
            return hits != null && hits.Length > 0;
        }

        /// <summary>
        /// 最小間距檢查（不依賴 margin）：以中心為球心，用半徑=minSpacing*0.5f 快查周遭是否有其他放置物。
        /// 注意：這是 MVP 快徑；若之後要更精準，可改為用 SpatialIndex 最近鄰距離。
        /// </summary>
        private static bool ViolatesMinSpacing(Bounds originalBounds, float minSpacing, LayerMask blockMask)
        {
            if (minSpacing <= 0f) return false;
            float r = minSpacing * 0.5f;
            var hits = Physics.OverlapSphere(
                originalBounds.center,
                r,
                blockMask,
                QueryTriggerInteraction.Ignore
            );
            return hits != null && hits.Length > 0;
        }
    }
}