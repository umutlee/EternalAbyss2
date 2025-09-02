using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep.Managers
{
    // 與現有 CreepManager 同 namespace / 同類別名 → partial 擴充
    public sealed partial class CreepManager : MonoBehaviour
    {
        [Header("Creep Grid (Dev-Minimal)")]
        [SerializeField] private float cellSize = 1f;

        // Scene 視窗可視化顏色（只在編輯器/除錯用）
        [SerializeField] private Color gizmoColor = new Color(0.3f, 1f, 0.3f, 0.25f);

        // 簡單的佔用集合：存「哪一些格子有菌毯」
        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

        /// <summary>查詢世界座標是否在菌毯上。</summary>
        public bool IsOnCreep(Vector3 worldPos)
        {
            return _occupiedCells.Contains(WorldToCell(worldPos));
        }

        /// <summary>把世界座標換成格子座標（XZ 平面）。</summary>
        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            int cx = Mathf.FloorToInt(worldPos.x / Mathf.Max(0.0001f, cellSize));
            int cz = Mathf.FloorToInt(worldPos.z / Mathf.Max(0.0001f, cellSize));
            return new Vector2Int(cx, cz);
        }

        /// <summary>把格子中心換回世界座標（Y=0）。</summary>
        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize);
        }

        /// <summary>在格子上加菌毯，回傳是否真的有新增。</summary>
        public bool AddCreep(Vector2Int cell) => _occupiedCells.Add(cell);

        /// <summary>移除格子的菌毯，回傳是否真的有移除。</summary>
        public bool RemoveCreep(Vector2Int cell) => _occupiedCells.Remove(cell);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 在編輯器 / 開發版畫格子，直觀看配置
        private void OnDrawGizmosSelected()
        {
            if (_occupiedCells == null || _occupiedCells.Count == 0) return;

            var prev = Gizmos.color;
            Gizmos.color = gizmoColor;

            Vector3 size = new Vector3(cellSize, 0.02f, cellSize);
            foreach (var cell in _occupiedCells)
            {
                Gizmos.DrawCube(CellToWorldCenter(cell) + Vector3.up * 0.01f, size);
            }

            Gizmos.color = prev;
        }
#endif
    }
}
