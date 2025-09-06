using System;
using UnityEngine;

namespace DeepAbyssHive.Units.Pathfinding
{
    /// <summary>
    /// 輕量格網取樣器（純類別、無場景掛載）：
    /// Walkable 規則：
    ///   1) 地面存在（Terrain raycast 命中）
    ///   2) 坡度 <= maxSlopeDegrees（由命中 normal 計）
    ///   3) 鄰近高度差 <= maxStepHeight（以半格偏移取樣）
    ///   4) 無 Building 碰撞（Physics.CheckBox）
    /// 成本：
    ///   base=1；若 IsOnCreepPredicate(center)==true → 乘 creepCostMul（預設 0.85）
    /// </summary>
    public class PathGridSampler : IPathGrid
    {
        // 基本格網設定
        public float CellSize { get; private set; }
        public Vector3 Origin { get; private set; }

        // 規則參數
        public float maxSlopeDegrees = 45f;
        public float maxStepHeight = 1.0f;
        public float creepCostMul = 0.85f;

        // 可選：是否在 Creep 上（外部透過委派提供）
        public Func<Vector3, bool> IsOnCreepPredicate;

        // 圖層遮罩
        private readonly int _terrainMask;
        private readonly int _buildingMask;

        public PathGridSampler(Vector3 origin, float cellSize)
        {
            Origin = origin;
            CellSize = Mathf.Max(0.01f, cellSize);

            int terrain = LayerMask.NameToLayer("Terrain");
            _terrainMask = (terrain >= 0) ? (1 << terrain) : ~0; // 若無 Terrain 層則不過濾

            int building = LayerMask.NameToLayer("Building");
            _buildingMask = (building >= 0) ? (1 << building) : 0; // 無 Building 層時視為無阻擋
        }

        // === 換算 ===
        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            Vector3 local = world - Origin;
            x = Mathf.FloorToInt(local.x / CellSize);
            y = Mathf.FloorToInt(local.z / CellSize);
            // 可走檢查由 IsWalkable 負責；此處總是回 true 代表換算成功
            return true;
        }

        public Vector3 CellCenter(int x, int y)
        {
            float cx = Origin.x + (x + 0.5f) * CellSize;
            float cz = Origin.z + (y + 0.5f) * CellSize;
            // y 由地面 raycast 估算；這裡先回 Origin.y，呼叫端通常以地面高度為準
            return new Vector3(cx, Origin.y, cz);
        }

        // === 取樣 ===
        public bool IsWalkable(int x, int y)
        {
            Vector3 center = CellCenter(x, y);
            if (!SampleGround(center, out var hitCenter)) return false;

            // 坡度（以中心 normal）
            float slopeDeg = Vector3.Angle(hitCenter.normal, Vector3.up);
            if (slopeDeg > maxSlopeDegrees) return false;

            // 高差（與半格偏移點比較）
            Vector3 offset = new Vector3(CellSize * 0.5f, 0f, 0f);
            if (!SampleGround(center + offset, out var hitB)) return false;
            float step = Mathf.Abs(hitCenter.point.y - hitB.point.y);
            if (step > maxStepHeight) return false;

            // Building 碰撞（使用格中心的方盒體積）
            Vector3 half = new Vector3(CellSize * 0.5f, Mathf.Max(0.5f, maxStepHeight), CellSize * 0.5f);
            if (_buildingMask != 0 && Physics.CheckBox(hitCenter.point, half, Quaternion.identity, _buildingMask, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }

        public float Cost(int x, int y)
        {
            Vector3 center = CellCenter(x, y);
            float c = 1f;
            if (IsOnCreepPredicate != null && IsOnCreepPredicate(center))
                c *= Mathf.Clamp(creepCostMul, 0.1f, 1f);
            return c;
        }

        // === 輔助 ===
        private bool SampleGround(Vector3 around, out RaycastHit hit)
        {
            // 自上而下地面射線：對 Terrain 層優先；若專案未設 Terrain 層，會用全遮罩
            Vector3 top = around + Vector3.up * 1000f;
            int mask = (_terrainMask != 0) ? _terrainMask : ~0;
            return Physics.Raycast(top, Vector3.down, out hit, 5000f, mask, QueryTriggerInteraction.Ignore);
        }
    }
}