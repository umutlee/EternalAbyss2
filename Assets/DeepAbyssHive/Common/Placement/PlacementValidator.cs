using System;
using UnityEngine;
using DeepAbyssHive.Core.Config;          // GameConfigProvider
using DeepAbyssHive.Common.Placement;     // Result<>, PlaceResultCode, PlacementResults

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 放置驗證幫手：統一檢查路徑（Physics + 可選 SpatialIndex/菌毯/邊界/最小間距）
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>SpatialIndex 並聯檢查：true=通過（無衝突）</summary>
        public static Func<Bounds, LayerMask, float, bool> SpatialIndexPredicate;
        /// <summary>菌毯要求：true=該 Bounds 位於菌毯覆蓋內</summary>
        public static Func<Bounds, bool> RequireCreepPredicate;
        /// <summary>邊界檢查：true=超界（不可放）</summary>
        public static Func<Bounds, bool> OutOfBoundsPredicate;

        /// <summary>最後一次驗證結果（供 HUD/Debug）</summary>
        public static Result<Bounds> LastResult { get; private set; }

        public static bool HasSpatialIndex => SpatialIndexPredicate != null;
        public static bool HasRequireCreep => RequireCreepPredicate != null;
        public static bool HasOutOfBounds => OutOfBoundsPredicate != null;

        /// <summary>
        /// 依 GameConfig 驗證放置；blockMask：阻擋圖層；extraMargin：臨時外擴
        /// </summary>
        public static Result<Bounds> ValidateByConfig(Bounds rawBounds, LayerMask blockMask, float extraMargin = 0f)
        {
            var cfg = GameConfigProvider.Current;
            // margin 取較大者（config vs 呼叫端）
            float margin = Mathf.Max(cfg.margin, extraMargin);
            var bounds = ExpandBounds(rawBounds, margin);

            // 1) 邊界（可選）
            if (OutOfBoundsPredicate != null && OutOfBoundsPredicate(bounds))
            {
                LastResult = PlacementResults.OutOfBounds("[Placement] Out of bounds");
                return LastResult;
            }

            // 2) Physics
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
                    bool spatialOk = SpatialIndexPredicate(bounds, blockMask, margin);
                    if (!spatialOk)
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

            // 3.5) 最小間距（獨立於 margin）
            if (cfg.minSpacing > 0f && ViolatesMinSpacing(rawBounds, cfg.minSpacing, blockMask))
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

            // OK
            LastResult = PlacementResults.OkBounds(bounds);
            return LastResult;
        }

        private static Bounds ExpandBounds(Bounds b, float margin)
        {
            if (margin <= 0f) return b;
            var s = b.size;
            s.x += margin * 2f; s.y += margin * 2f; s.z += margin * 2f;
            b.size = s;
            return b;
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
        /// 最小間距：以中心球體（半徑=minSpacing*0.5f）快查周遭是否有其他放置物。
        /// 後續可替換為 SpatialIndex 最近鄰以更精準。
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