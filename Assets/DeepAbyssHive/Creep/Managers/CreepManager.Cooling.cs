using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep.Managers
{
    public partial class CreepManager : MonoBehaviour
    {
        // 每個 Chunk 的冷卻表（size*size 的 byte），以及遞減游標
        private readonly Dictionary<Vector2Int, byte[]> _cooldowns = new Dictionary<Vector2Int, byte[]>();
        private readonly Dictionary<Vector2Int, int>    _coolCursor = new Dictionary<Vector2Int, int>();

        [Header("Creep Cooling")]
        [SerializeField, Tooltip("鄰接入列後套用的冷卻幀數（防止短時間反覆入列）")]
        private byte neighborCooldownFrames = 3;

        [SerializeField, Tooltip("每幀最多遞減的冷卻 cell 數（總量）")]
        private int cooldownDecayBudgetPerFrame = 8000;

        // 建立/移除對應 chunk 的冷卻表
        public void EnsureCooldown(Vector2Int chunk, int chunkSize)
        {
            if (_cooldowns.ContainsKey(chunk)) return;
            int n = Mathf.Max(1, chunkSize) * Mathf.Max(1, chunkSize);
            _cooldowns[chunk] = new byte[n]; // 預設全 0
            _coolCursor[chunk] = 0;
        }
        public void RemoveCooldown(Vector2Int chunk)
        {
            _cooldowns.Remove(chunk);
            _coolCursor.Remove(chunk);
        }

        // 讀寫單格；無表/越界則視為 0
        public byte GetCooldown(Vector2Int chunk, int x, int y)
        {
            if (!_grids.TryGetValue(chunk, out var g)) return 0;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return 0;
            if (!_cooldowns.TryGetValue(chunk, out var arr)) return 0;
            return arr[y * g.size + x];
        }
        public void TouchCooldown(Vector2Int chunk, int x, int y, byte frames)
        {
            if (!_grids.TryGetValue(chunk, out var g)) return;
            if ((uint)x >= (uint)g.size || (uint)y >= (uint)g.size) return;
            if (!_cooldowns.TryGetValue(chunk, out var arr)) return;
            int idx = y * g.size + x;
            byte f = frames;
            if (f == 0) f = 1;
            if (arr[idx] < f) arr[idx] = f; // 取較大者
        }

        // 逐幀按總預算遞減（游標循環）
        public void DecaySomeCooldowns(int totalBudget)
        {
            int remain = Mathf.Max(0, totalBudget);
            if (remain == 0 || _cooldowns.Count == 0) return;

            // 粗略均攤：一輪輪掃每個 chunk 少量 cell，直到耗完預算
            var keys = _cooldowns.Keys;
            while (remain > 0)
            {
                foreach (var key in keys)
                {
                    if (remain <= 0) break;
                    if (!_cooldowns.TryGetValue(key, out var arr)) continue;
                    if (!_grids.TryGetValue(key, out var g)) continue;
                    int N = g.size * g.size;
                    if (N == 0) continue;

                    int cursor = _coolCursor.TryGetValue(key, out var c) ? c : 0;
                    // 每次處理固定小批（例如 128）
                    int batch = Mathf.Min(128, remain);
                    for (int i = 0; i < batch; i++)
                    {
                        if (arr[cursor] > 0) arr[cursor]--;
                        cursor++; if (cursor >= N) cursor = 0;
                    }
                    _coolCursor[key] = cursor;
                    remain -= batch;
                }
            }
        }
    }
}