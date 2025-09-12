using System;
using UnityEngine;
using DeepAbyssHive.Core.Config;          // GameConfigProvider
using DeepAbyssHive.Common.Placement;     // Result<>, PlaceResultCode, PlacementResults
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 放置驗證幫手：統一檢查路徑（Physics + 可選 SpatialIndex/菌毯/邊界/最小間距）
    /// </summary>
    public static class PlacementValidator
    {
        /// <summary>SpatialIndex 並聯檢查：true=通過（無衝突）</summary>
        public static Func<Bounds, LayerMask, float, bool> SpatialIndexPredicate;
        /// <summary>最近鄰距離檢查：回傳 true 表示「半徑內沒有鄰居」（通過）</summary>
        public static Func<Vector3, float, LayerMask, bool> NoNeighborWithinRadiusPredicate;
        /// <summary>菌毯要求：true=該 Bounds 位於菌毯覆蓋內</summary>
        public static Func<Bounds, bool> RequireCreepPredicate;
        /// <summary>邊界檢查：true=超界（不可放）</summary>
        public static Func<Bounds, bool> OutOfBoundsPredicate;

        /// <summary>最後一次驗證結果（供 HUD/Debug）</summary>
        public static Result<Bounds> LastResult { get; private set; }

        public static bool HasSpatialIndex => SpatialIndexPredicate != null;
        public static bool HasRequireCreep => RequireCreepPredicate != null;
        public static bool HasOutOfBounds => OutOfBoundsPredicate != null;

        // 舊版：僅 AABB，保持相容
        public static Result<Bounds> ValidateByConfig(Bounds rawBounds, LayerMask blockMask, float extraMargin = 0f)
        {
            return ValidateByConfig(rawBounds.center, rawBounds.extents, Quaternion.identity, blockMask, extraMargin);
        }

        /// <summary>
        /// 新版：支援旋轉的放置驗證（Physics.OverlapBox 使用 rotation）
        /// </summary>
        public static Result<Bounds> ValidateByConfig(Vector3 center, Vector3 halfExtents, Quaternion rotation, LayerMask blockMask, float extraMargin = 0f)
        {
            var cfg = GameConfigProvider.Current;
            float margin = Mathf.Max(cfg.margin, extraMargin);
            // 傳回資料仍以 AABB 表示，但物理碰撞用有向盒
            var rawBounds = new Bounds(center, halfExtents * 2f);
            var bounds = ExpandBounds(rawBounds, margin);

            // 1) 邊界（可選）
            if (OutOfBoundsPredicate != null && OutOfBoundsPredicate(bounds))
            {
                LastResult = PlacementResults.OutOfBounds("[Placement] Out of bounds");
                return LastResult;
            }

            // 2) Physics
            if (HasPhysicsCollisionOriented(center, halfExtents, rotation, blockMask))
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
                    DAHLog.Warning(LogCategory.SYSTEM, "[DEV HUD] useSpatialIndexForPlacement=true，但尚未提供 SpatialIndexPredicate；暫以 Physics 結果為準。");
                }
            }

            // 3.5) 最小間距（獨立於 margin；Prefer SpatialIndex 最近鄰）
            if (cfg.minSpacing > 0f)
            {
                bool spacingOk;
                if (NoNeighborWithinRadiusPredicate != null)
                {
                    spacingOk = NoNeighborWithinRadiusPredicate(rawBounds.center, cfg.minSpacing, blockMask);
                }
                else
                {
                    spacingOk = !ViolatesMinSpacing(rawBounds, cfg.minSpacing, blockMask);
                }
                if (!spacingOk)
                {
                    LastResult = PlacementResults.Collision("[Placement] MinSpacing violation");
                    return LastResult;
                }
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

        // 舊版（相容），保留給既有呼叫點
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

        // 新版：有向 OverlapBox
        private static bool HasPhysicsCollisionOriented(Vector3 center, Vector3 halfExtents, Quaternion rotation, LayerMask blockMask)
        {
            var hits = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
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