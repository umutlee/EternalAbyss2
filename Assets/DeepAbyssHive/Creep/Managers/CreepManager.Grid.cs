using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep.Managers
{
    // 最小擴充：不動原有 CreepManager 的生命週期/結構
    public partial class CreepManager : MonoBehaviour
    {
        // 每個 Chunk 一份 grid（bitset）
        private readonly Dictionary<Vector2Int, CreepGrid> _grids = new Dictionary<Vector2Int, CreepGrid>();

        // 如果原類沒有單例，這裡提供「保守型」取用；有單例也不衝突（我們不賦值）
        public static CreepManager GetActive()
        {
            // 尋找場景中的第一個活動 CreepManager（Boot 會掛上去）
            return FindObjectOfType<CreepManager>(includeInactive: false);
        }

        private struct CreepGrid
        {
            public int size;          // 邊長（格數），size×size
            public BitArray bits;     // true=有菌毯
            public int setCount;      // 目前 true 的數量（覆蓋格數）
        }

        private static int Idx(int x, int y, int size) => y * size + x;

        // 建立/確保網格
        public void EnsureGrid(Vector2Int chunkCoord, int chunkSize)
        {
            if (_grids.ContainsKey(chunkCoord)) return;
            int s = Mathf.Max(1, chunkSize);
            var ba = new BitArray(s * s, false);
            _grids[chunkCoord] = new CreepGrid { size = s, bits = ba, setCount = 0 };
            // Debug.Log($"[CREEP] EnsureGrid {chunkCoord} size={s}");
        }

        public void RemoveGrid(Vector2Int chunkCoord)
        {
            if (_grids.Remove(chunkCoord))
            {
                // Debug.Log($"[CREEP] RemoveGrid {chunkCoord}");
            }
        }

        public bool IsSet(Vector2Int chunkCoord, int x, int y)
        {
            if (!_grids.TryGetValue(chunkCoord, out var g)) return false;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return false;
            return g.bits[Idx(x, y, g.size)];
        }

        public void Set(Vector2Int chunkCoord, int x, int y)
        {
            if (!_grids.TryGetValue(chunkCoord, out var g)) return;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return;
            int i = Idx(x, y, g.size);
            if (!g.bits[i])
            {
                g.bits[i] = true;
                g.setCount++;
                _grids[chunkCoord] = g; // 結構體回寫
            }
        }

        public void Unset(Vector2Int chunkCoord, int x, int y)
        {
            if (!_grids.TryGetValue(chunkCoord, out var g)) return;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return;
            int i = Idx(x, y, g.size);
            if (g.bits[i])
            {
                g.bits[i] = false;
                g.setCount--;
                if (g.setCount < 0) g.setCount = 0;
                _grids[chunkCoord] = g;
            }
        }

        // 取得總格數與覆蓋格數（for HUD/統計）
        public void GetTotals(out int totalCells, out int coveredCells)
        {
            totalCells = 0; coveredCells = 0;
            foreach (var kv in _grids)
            {
                var g = kv.Value;
                totalCells  += g.size * g.size;
                coveredCells += g.setCount;
            }
        }

        // === 便利方法：為 CreepDebugInput 等提供簡化 API ===
        
        /// <summary>
        /// 將世界座標轉換為格子座標（假設每格 2x2 單位，與 TerrainManager 一致）
        /// </summary>
        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            // 假設格子大小為 2x2 單位（與 TerrainManager 的 CellSize 一致）
            const float cellSize = 2f;
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / cellSize),
                Mathf.FloorToInt(worldPos.z / cellSize)
            );
        }

        /// <summary>
        /// 在指定格子座標添加菌毯
        /// </summary>
        public void AddCreep(Vector2Int cellCoord)
        {
            // 將格子座標轉換為 chunk 座標和本地座標
            // 假設每個 chunk 是 32x32 格子
            const int chunkSize = 32;
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt((float)cellCoord.x / chunkSize),
                Mathf.FloorToInt((float)cellCoord.y / chunkSize)
            );
            
            int localX = cellCoord.x - chunkCoord.x * chunkSize;
            int localY = cellCoord.y - chunkCoord.y * chunkSize;
            
            // 確保本地座標為正數
            if (localX < 0) { localX += chunkSize; chunkCoord.x--; }
            if (localY < 0) { localY += chunkSize; chunkCoord.y--; }
            
            // 確保 grid 存在
            EnsureGrid(chunkCoord, chunkSize);
            Set(chunkCoord, localX, localY);
        }

        /// <summary>
        /// 移除指定格子座標的菌毯
        /// </summary>
        /// <returns>如果成功移除返回 true，如果該格子本來就沒有菌毯返回 false</returns>
        public bool RemoveCreep(Vector2Int cellCoord)
        {
            // 將格子座標轉換為 chunk 座標和本地座標
            const int chunkSize = 32;
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt((float)cellCoord.x / chunkSize),
                Mathf.FloorToInt((float)cellCoord.y / chunkSize)
            );
            
            int localX = cellCoord.x - chunkCoord.x * chunkSize;
            int localY = cellCoord.y - chunkCoord.y * chunkSize;
            
            // 確保本地座標為正數
            if (localX < 0) { localX += chunkSize; chunkCoord.x--; }
            if (localY < 0) { localY += chunkSize; chunkCoord.y--; }
            
            // 檢查是否有菌毯
            bool hadCreep = IsSet(chunkCoord, localX, localY);
            if (hadCreep)
            {
                Unset(chunkCoord, localX, localY);
            }
            return hadCreep;
        }
    }

    // 提供 Terrain 端易用的 Hook，不侵入 CreepManager 內部
    public static class CreepManagerGridHooks
    {
        public static void OnChunkLoaded(Vector2Int chunkCoord, int chunkSize)
        {
            var cm = CreepManager.GetActive();
            if (cm) { cm.EnsureGrid(chunkCoord, chunkSize); cm.EnsureCooldown(chunkCoord, chunkSize); }
        }

        public static void OnChunkUnloaded(Vector2Int chunkCoord)
        {
            var cm = CreepManager.GetActive();
            if (cm) { cm.RemoveGrid(chunkCoord); cm.RemoveCooldown(chunkCoord); }
        }
    }
}