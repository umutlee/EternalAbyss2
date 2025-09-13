using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯网格服务实现
    /// 负责菌毯网格数据的管理和操作
    /// </summary>
    public class CreepGridService : ICreepGridService
    {
        #region 私有字段

        private readonly Dictionary<Vector2Int, CreepData> _creepGrid;
        private readonly HashSet<Vector2Int> _activePositions;
        private ISpatialIndex _spatialIndex;

        private float _gridCellSize = 1f;
        private int _gridWidth = 100;
        private int _gridHeight = 100;

        #endregion

        #region 属性

        public string ServiceName => "CreepGridService";
        public float GridCellSize => _gridCellSize;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public bool IsInitialized { get; private set; }
        public bool IsCommandAvailable => IsInitialized;
        private bool _isPaused;

        #endregion

        #region 构造函数

        public CreepGridService()
        {
            _creepGrid = new Dictionary<Vector2Int, CreepData>();
            _activePositions = new HashSet<Vector2Int>();
        }

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;

            _creepGrid.Clear();
            _activePositions.Clear();
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            _creepGrid.Clear();
            _activePositions.Clear();
            
            IsInitialized = false;
        }

        #endregion

        #region ICreepGridService 实现

        public void InitializeGrid(int width, int height, float cellSize)
        {
            _gridWidth = width;
            _gridHeight = height;
            _gridCellSize = cellSize;
            
            _creepGrid.Clear();
            _activePositions.Clear();
        }

        public void ClearGrid()
        {
            _creepGrid.Clear();
            _activePositions.Clear();
        }

        public void SetGridCell(Vector2Int gridPosition, CreepData data)
        {
            if (!IsValidGridPosition(gridPosition)) return;

            _creepGrid[gridPosition] = data;
            _activePositions.Add(gridPosition);

            // 更新空间索引
            if (_spatialIndex != null)
            {
                Vector3 worldPos = GridToWorldPosition(gridPosition);
                Bounds bounds = new Bounds(worldPos, new Vector3(_gridCellSize, _gridCellSize, _gridCellSize));
                _spatialIndex.Insert(data, worldPos, bounds.extents);
            }
        }

        public CreepData GetGridCell(Vector2Int gridPosition)
        {
            _creepGrid.TryGetValue(gridPosition, out CreepData data);
            return data;
        }

        public void RemoveGridCell(Vector2Int gridPosition)
        {
            if (_creepGrid.TryGetValue(gridPosition, out CreepData data))
            {
                _creepGrid.Remove(gridPosition);
                _activePositions.Remove(gridPosition);

                // 从空间索引移除
                if (_spatialIndex != null)
                {
                    Vector3 worldPos = GridToWorldPosition(gridPosition);
                    Bounds bounds = new Bounds(worldPos, new Vector3(_gridCellSize, _gridCellSize, _gridCellSize));
                    _spatialIndex.Remove(data, worldPos, bounds.extents);
                }
            }
        }

        public bool IsValidGridPosition(Vector2Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < _gridWidth &&
                   gridPosition.y >= 0 && gridPosition.y < _gridHeight;
        }

        public bool HasCreepAt(Vector2Int gridPosition)
        {
            return _creepGrid.ContainsKey(gridPosition);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / _gridCellSize),
                Mathf.RoundToInt(worldPosition.z / _gridCellSize)
            );
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * _gridCellSize,
                0f,
                gridPosition.y * _gridCellSize
            );
        }

        public Vector2Int[] GetNeighborPositions(Vector2Int gridPosition, bool includeDiagonal = true)
        {
            var neighbors = new List<Vector2Int>();

            // 四方向邻居
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                Vector2Int neighbor = gridPosition + dir;
                if (IsValidGridPosition(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            // 对角线邻居
            if (includeDiagonal)
            {
                Vector2Int[] diagonals = {
                    new Vector2Int(1, 1), new Vector2Int(-1, 1),
                    new Vector2Int(1, -1), new Vector2Int(-1, -1)
                };

                foreach (var diag in diagonals)
                {
                    Vector2Int neighbor = gridPosition + diag;
                    if (IsValidGridPosition(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors.ToArray();
        }

        public NativeArray<Vector2Int> GetGridPositionsInRange(Vector2Int center, int radius)
        {
            var positions = new List<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsValidGridPosition(pos))
                    {
                        // 将Vector2Int转换为Vector2后再计算距离
                        float distance = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(pos.x, pos.y));
                        if (distance <= radius)
                        {
                            positions.Add(pos);
                        }
                    }
                }
            }

            var result = new NativeArray<Vector2Int>(positions.Count, Allocator.Temp);
            for (int i = 0; i < positions.Count; i++)
            {
                result[i] = positions[i];
            }

            return result;
        }

        public NativeArray<Vector2Int> GetActiveCreepPositions()
        {
            var result = new NativeArray<Vector2Int>(_activePositions.Count, Allocator.Temp);
            int index = 0;
            foreach (var pos in _activePositions)
            {
                result[index++] = pos;
            }
            return result;
        }

        public void BatchUpdateGrid(NativeArray<CreepGridUpdate> updates)
        {
            for (int i = 0; i < updates.Length; i++)
            {
                var update = updates[i];
                if (update.Remove)
                {
                    RemoveGridCell(update.GridPosition);
                }
                else
                {
                    SetGridCell(update.GridPosition, update.Data);
                }
            }
        }

        public CreepGridStatistics GetGridStatistics()
        {
            var stats = new CreepGridStatistics();
            stats.TotalCells = _gridWidth * _gridHeight;
            stats.ActiveCells = _activePositions.Count;

            float totalStrength = 0f;
            int networkCount = 0;

            foreach (var kvp in _creepGrid)
            {
                totalStrength += kvp.Value.Strength;
            }

            stats.AverageStrength = stats.ActiveCells > 0 ? totalStrength / stats.ActiveCells : 0f;
            stats.TotalCoverage = stats.ActiveCells * _gridCellSize * _gridCellSize;
            stats.NetworkCount = networkCount; // 需要从网络服务获取

            return stats;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置空间索引引用
        /// </summary>
        /// <param name="spatialIndex">空间索引</param>
        public void SetSpatialIndex(ISpatialIndex spatialIndex)
        {
            _spatialIndex = spatialIndex;
        }

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="paused">是否暂停</param>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            DAHLog.Info(LogCategory.SERVICE, $"[CreepGridService] 服务已{(paused ? "暂停" : "恢复")}");
        }

        #endregion
    }
}