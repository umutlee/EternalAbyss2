using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯查询服务实现
    /// 提供所有菌毯相关的只读查询功能
    /// </summary>
    public class CreepQueryService : ICreepQueryService, IService, IQueryService
    {
        #region 私有字段

        private ICreepGridService _gridService;
        private ICreepSourceService _sourceService;
        private ICreepNetworkService _networkService;

        #endregion

        #region 属性

        public string ServiceName => "CreepQueryService";
        public bool IsInitialized { get; private set; }
        public bool IsQueryAvailable => IsInitialized;

        #endregion

        #region 构造函数

        public CreepQueryService(
            ICreepGridService gridService,
            ICreepSourceService sourceService,
            ICreepNetworkService networkService)
        {
            _gridService = gridService;
            _sourceService = sourceService;
            _networkService = networkService;
        }

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;
            IsInitialized = false;
        }

        #endregion

        #region ICreepQueryService 实现

        public bool IsPositionCovered(Vector3 position)
        {
            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            return _gridService.HasCreepAt(gridPos);
        }

        public float GetCreepStrength(Vector3 position)
        {
            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            CreepData data = _gridService.GetGridCell(gridPos);
            return data.Strength;
        }

        public float GetCreepDensity(Vector3 position)
        {
            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            CreepData data = _gridService.GetGridCell(gridPos);
            return data.Density;
        }

        public float GetCreepCoverageInRange(Vector3 center, float radius)
        {
            Vector2Int centerGrid = _gridService.WorldToGridPosition(center);
            int gridRadius = Mathf.CeilToInt(radius / _gridService.GridCellSize);
            
            var positions = _gridService.GetGridPositionsInRange(centerGrid, gridRadius);
            int coveredCount = 0;
            
            for (int i = 0; i < positions.Length; i++)
            {
                if (_gridService.HasCreepAt(positions[i]))
                {
                    coveredCount++;
                }
            }
            
            positions.Dispose();
            return positions.Length > 0 ? (float)coveredCount / positions.Length : 0f;
        }

        public CreepGridCell GetCreepGrid(int gridX, int gridZ)
        {
            Vector2Int gridPos = new Vector2Int(gridX, gridZ);
            CreepData data = _gridService.GetGridCell(gridPos);
            
            return new CreepGridCell
            {
                Position = gridPos,
                Data = data,
                IsActive = _gridService.HasCreepAt(gridPos)
            };
        }

        public NativeArray<CreepGridCell> GetCreepGridRange(int minX, int minZ, int maxX, int maxZ)
        {
            var cells = new List<CreepGridCell>();
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    if (_gridService.IsValidGridPosition(gridPos))
                    {
                        CreepData data = _gridService.GetGridCell(gridPos);
                        cells.Add(new CreepGridCell
                        {
                            Position = gridPos,
                            Data = data,
                            IsActive = _gridService.HasCreepAt(gridPos)
                        });
                    }
                }
            }
            
            var result = new NativeArray<CreepGridCell>(cells.Count, Allocator.Temp);
            for (int i = 0; i < cells.Count; i++)
            {
                result[i] = cells[i];
            }
            
            return result;
        }

        public Vector3 GetNearestCreepEdge(Vector3 position, float maxDistance = 50f)
        {
            Vector2Int startGrid = _gridService.WorldToGridPosition(position);
            int maxGridDistance = Mathf.CeilToInt(maxDistance / _gridService.GridCellSize);
            
            // 螺旋搜索最近的菌毯边缘
            for (int radius = 1; radius <= maxGridDistance; radius++)
            {
                var positions = _gridService.GetGridPositionsInRange(startGrid, radius);
                
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector2Int gridPos = positions[i];
                    if (_gridService.HasCreepAt(gridPos))
                    {
                        // 检查是否是边缘（至少有一个邻居没有菌毯）
                        var neighbors = _gridService.GetNeighborPositions(gridPos, false);
                        foreach (var neighbor in neighbors)
                        {
                            if (!_gridService.HasCreepAt(neighbor))
                            {
                                positions.Dispose();
                                return _gridService.GridToWorldPosition(gridPos);
                            }
                        }
                    }
                }
                
                positions.Dispose();
            }
            
            return position; // 未找到边缘，返回原位置
        }

        public bool IsCreepConnected(Vector3 start, Vector3 end, float minStrength = 0.1f)
        {
            return _networkService.IsConnected(start, end, minStrength);
        }

        public CreepNetworkInfo GetCreepNetwork(Vector3 position)
        {
            return _networkService.GetNetworkInfo(position);
        }

        public NativeArray<CreepSource> GetCreepSources(int playerId = -1)
        {
            if (playerId == -1)
            {
                // 获取所有玩家的源点 - 需要实现
                return new NativeArray<CreepSource>(0, Allocator.Temp);
            }
            else
            {
                return _sourceService.GetPlayerCreepSources(playerId);
            }
        }

        public NativeArray<Vector3> GetCreepExpansionFront(int playerId)
        {
            // 获取扩张前沿 - 需要从扩张服务获取
            return new NativeArray<Vector3>(0, Allocator.Temp);
        }

        public Vector3[] CalculateCreepExpansionPath(Vector3 from, Vector3 to, int playerId)
        {
            // 计算扩张路径 - 需要从扩张服务获取
            return new Vector3[0];
        }

        public CreepStatistics GetCreepStatistics(int playerId)
        {
            var gridStats = _gridService.GetGridStatistics();
            var sourceStats = _sourceService.GetSourceStatistics(playerId);
            var networkStats = _networkService.GetNetworkStatistics(playerId);
            
            return new CreepStatistics
            {
                TotalArea = gridStats.TotalCoverage,
                AverageStrength = gridStats.AverageStrength,
                AverageDensity = gridStats.AverageStrength, // 简化实现
                NetworkCount = networkStats.TotalNetworks,
                SourceCount = sourceStats.TotalSources,
                GrowthRate = 1.0f, // 需要从配置获取
                DecayRate = 0.05f   // 需要从配置获取
            };
        }

        public bool IsSuitableForCreepGrowth(Vector3 position)
        {
            // 检查地形、障碍物等因素
            // 简化实现
            return true;
        }

        public float GetCreepGrowthRate(Vector3 position)
        {
            // 根据环境因素计算生长速度
            // 简化实现
            return 1.0f;
        }

        #endregion
    }

    /// <summary>
    /// 菌毯网格单元格
    /// </summary>
    public struct CreepGridCell
    {
        public Vector2Int Position;
        public CreepData Data;
        public bool IsActive;
    }
}