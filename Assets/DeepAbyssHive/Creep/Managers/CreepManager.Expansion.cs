using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using DeepAbyssHive.Terrain.Config; // 讀取 TerrainConfigSO（換算 world<->chunk）

namespace DeepAbyssHive.Creep.Managers
{
    public partial class CreepManager : MonoBehaviour
    {
        // —— frontier 佇列（cell = chunkCoord + local XY）——
        private readonly Queue<(Vector2Int chunk, int x, int y)> _frontier = new Queue<(Vector2Int, int, int)>();
        private readonly HashSet<(int cx, int cy, int x, int y)> _queued = new HashSet<(int, int, int, int)>();

        [Header("Creep Expansion (M2-02)")]
        [SerializeField] private int expansionPerTick = 2000;  // 每幀最多擴張格數
        [SerializeField] private int neighborMode = 4;         // 4或8 鄰居（預設4方向）
        [SerializeField] private bool restrictToLoadedChunks = true; // 只在已載入的 chunk 內擴張
        [Header("Terrain Gate")]
        [SerializeField, Tooltip("允許的最大坡度（角度）")]
        private float maxSlopeDegrees = 25f;
        [SerializeField, Tooltip("單步允許的最大高度差")]
        private float maxStepHeight = 0.8f;

        [Header("Building Block")]
        [SerializeField, Tooltip("建築阻擋的 LayerMask（若為 0 則自動取 'Building' 層；不存在則忽略）")]
        private LayerMask buildingBlockMask = 0;
        [SerializeField, Tooltip("建築阻擋檢查盒子半徑（XZ）與高度（Y）")]
        private Vector2 buildingCheckHalf = new Vector2(0.45f, 1.0f); // xz 半徑，y 高度

        // 效能統計（HUD 讀取）
        private int   _lastStepCells = 0;
        private float _lastStepMillis = 0f;

        public void GetLastPerf(out int cells, out float ms) { cells = _lastStepCells; ms = _lastStepMillis; }
        public int  GetBudget() => expansionPerTick;
        public void SetBudget(int v) => expansionPerTick = Mathf.Max(0, v);

        // —— 對外最小 API —— 
        public void Seed(Vector2Int chunk, int x, int y)
        {
            if (!_grids.ContainsKey(chunk)) return;
            EnqueueIfApplicable(chunk, x, y);
        }

        /// <summary>以世界座標種子（Editor/Dev 刷子用）</summary>
        public void SeedWorld(Vector3 worldPos)
        {
            var cfg = Resources.Load<TerrainConfigSO>("Configs/TerrainConfig");
            if (!cfg) return;
            int s = Mathf.Max(1, cfg.chunkSize);
            float tile = Mathf.Max(0.0001f, cfg.tileSize);
            float cw = s * tile;
            int cx = Mathf.FloorToInt(worldPos.x / cw);
            int cy = Mathf.FloorToInt(worldPos.z / cw);
            int lx = Mathf.FloorToInt((worldPos.x - cx * cw) / tile);
            int ly = Mathf.FloorToInt((worldPos.z - cy * cw) / tile);
            Seed(new Vector2Int(cx, cy), lx, ly);
        }

        public void StepExpansionBudgeted(int budgetOverride = -1)
        {
            int budget = (budgetOverride >= 0) ? budgetOverride : expansionPerTick;
            // 先遞減一點冷卻，避免同區域短時間反覆入列
            DecaySomeCooldowns(cooldownDecayBudgetPerFrame);
            if (budget == 0 || _frontier.Count == 0) { _lastStepCells = 0; _lastStepMillis = 0f; return; }

            var sw = Stopwatch.StartNew();
            int expanded = 0;

            while (expanded < budget && _frontier.Count > 0)
            {
                var cell = _frontier.Dequeue();
                _queued.Remove((cell.chunk.x, cell.chunk.y, cell.x, cell.y));

                if (!_grids.TryGetValue(cell.chunk, out var g)) continue;
                if ((uint)cell.x >= (uint)g.size || (uint)cell.y >= (uint)g.size) continue;

                // 已有則略過（避免重工）
                if (g.bits[Idx(cell.x, cell.y, g.size)]) continue;

                // 設置並計數
                g.bits[Idx(cell.x, cell.y, g.size)] = true;
                g.setCount++;
                _grids[cell.chunk] = g;
                expanded++;

                // 擴張到鄰居（套用 冷卻/坡度/建築 阻擋）
                foreach (var n in Neighbors(cell.chunk, cell.x, cell.y, g.size, neighborMode))
                {
                    if (restrictToLoadedChunks && !_grids.ContainsKey(n.chunk)) continue;
                    if (!CanEnterFrom(cell.chunk, cell.x, cell.y, n.chunk, n.x, n.y)) continue;
                    if (EnqueueIfApplicable(n.chunk, n.x, n.y))
                        TouchCooldown(n.chunk, n.x, n.y, neighborCooldownFrames);
                }
            }

            sw.Stop();
            _lastStepCells  = expanded;
            _lastStepMillis = (float)sw.Elapsed.TotalMilliseconds;
        }

        // —— helpers —— 
        // 成功入列回傳 true
        private bool EnqueueIfApplicable(Vector2Int chunk, int x, int y)
        {
            if (!_grids.TryGetValue(chunk, out var g)) return false;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return false;
            var key = (chunk.x, chunk.y, x, y);
            if (_queued.Contains(key)) return false;
            if (g.bits[Idx(x, y, g.size)]) return false;  // 已為真不再入列
            _frontier.Enqueue((chunk, x, y));
            _queued.Add(key);
            return true;
        }

        // 門檻：冷卻、坡度/高差、建築阻擋
        private bool CanEnterFrom(Vector2Int aChunk, int ax, int ay, Vector2Int bChunk, int bx, int by)
        {
            // 1) 冷卻：若目標 cell 冷卻中則略過
            if (GetCooldown(bChunk, bx, by) > 0) return false;

            // 2) 坡度/高度差：用與地形相同的噪聲公式估高，計算步進坡度
            SampleWorld(aChunk, ax, ay, out var aPos, out var aH);
            SampleWorld(bChunk, bx, by, out var bPos, out var bH);
            float dh = Mathf.Abs(bH - aH);
            if (dh > Mathf.Max(0f, maxStepHeight)) return false;
            float dxz = Vector2.Distance(new Vector2(aPos.x, aPos.z), new Vector2(bPos.x, bPos.z));
            float slopeDeg = (dxz > 0.0001f) ? Mathf.Atan2(dh, dxz) * Mathf.Rad2Deg : 0f;
            if (slopeDeg > Mathf.Max(0f, maxSlopeDegrees)) return false;

            // 3) 建築阻擋：在目標 cell 中心做小盒檢查
            int mask = buildingBlockMask.value;
            if (mask == 0)
            {
                int building = LayerMask.NameToLayer("Building");
                if (building != -1) mask = (1 << building);
            }
            if (mask != 0)
            {
                // y 提升一點避免貼地 self-intersection
                Vector3 c = bPos + Vector3.up * (buildingCheckHalf.y + 0.02f);
                Vector3 half = new Vector3(buildingCheckHalf.x, buildingCheckHalf.y, buildingCheckHalf.x);
                if (Physics.CheckBox(c, half, Quaternion.identity, mask, QueryTriggerInteraction.Ignore))
                    return false;
            }
            return true;
        }

        // 將 (chunk, local x/y) 轉世界座標並估高度（與地形生成吻合）
        private void SampleWorld(Vector2Int chunk, int x, int y, out Vector3 world, out float height)
        {
            var cfg = Resources.Load<DeepAbyssHive.Terrain.Config.TerrainConfigSO>("Configs/TerrainConfig");
            int s    = Mathf.Max(1, cfg ? cfg.chunkSize : 64);
            float t  = Mathf.Max(0.0001f, cfg ? cfg.tileSize : 1f);
            float cw = s * t;
            float wx = chunk.x * cw + (x + 0.5f) * t;
            float wz = chunk.y * cw + (y + 0.5f) * t;
            int seed = cfg ? cfg.seed : 12345;
            float ns = cfg ? cfg.noiseScale : 0.1f;
            float hs = cfg ? cfg.heightScale : 10f;
            height = Mathf.PerlinNoise((seed + wx) * ns, (seed + wz) * ns) * hs;
            world = new Vector3(wx, height, wz);
        }

        private static IEnumerable<(Vector2Int chunk, int x, int y)> Neighbors(Vector2Int chunk, int x, int y, int size, int mode)
        {
            // 允許跨 chunk（僅以 size 對齊，假設所有 chunk size 相同）
            // 4向
            yield return Wrap(chunk, x - 1, y, size);
            yield return Wrap(chunk, x + 1, y, size);
            yield return Wrap(chunk, x, y - 1, size);
            yield return Wrap(chunk, x, y + 1, size);
            if (mode >= 8)
            {
                yield return Wrap(chunk, x - 1, y - 1, size);
                yield return Wrap(chunk, x + 1, y - 1, size);
                yield return Wrap(chunk, x - 1, y + 1, size);
                yield return Wrap(chunk, x + 1, y + 1, size);
            }
        }

        private static (Vector2Int chunk, int x, int y) Wrap(Vector2Int c, int x, int y, int s)
        {
            if (x < 0) { c.x -= 1; x += s; }
            else if (x >= s) { c.x += 1; x -= s; }
            if (y < 0) { c.y -= 1; y += s; }
            else if (y >= s) { c.y += 1; y -= s; }
            return (c, x, y);
        }
    }
}