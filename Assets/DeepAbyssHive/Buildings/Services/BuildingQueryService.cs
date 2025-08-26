using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建筑查询服务实现
    /// 负责所有建筑相关的只读查询操作
    /// </summary>
    public class BuildingQueryService : IBuildingQueryService, IQueryService, IService
    {
        // 建筑数据引用
        private readonly Dictionary<int, BuildingData> _buildings;
        private readonly Dictionary<int, GameObject> _buildingGameObjects;
        private readonly Dictionary<BuildingType, BuildingTemplate> _buildingTemplates;
        
        // 查询缓存
        private readonly Dictionary<int, List<BuildingData>> _playerBuildingsCache = new Dictionary<int, List<BuildingData>>();
        private readonly Dictionary<BuildingType, List<BuildingData>> _typeBuildingsCache = new Dictionary<BuildingType, List<BuildingData>>();
        private float _lastCacheUpdateTime = 0f;
        private const float CACHE_UPDATE_INTERVAL = 1.0f;

        // IService 介面實現
        public string ServiceName => "BuildingQueryService";
        public bool IsInitialized { get; private set; }
        
        // IQueryService 介面實現
        public bool IsQueryAvailable => IsInitialized;

        public BuildingQueryService(
            Dictionary<int, BuildingData> buildings,
            Dictionary<int, GameObject> buildingGameObjects,
            Dictionary<BuildingType, BuildingTemplate> buildingTemplates)
        {
            _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            _buildingGameObjects = buildingGameObjects ?? throw new ArgumentNullException(nameof(buildingGameObjects));
            _buildingTemplates = buildingTemplates ?? throw new ArgumentNullException(nameof(buildingTemplates));
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            // 初始化查询缓存
            _playerBuildingsCache.Clear();
            _typeBuildingsCache.Clear();
            _lastCacheUpdateTime = 0f;
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized)
                return;

            // 清理所有缓存
            _playerBuildingsCache.Clear();
            _typeBuildingsCache.Clear();
            _lastCacheUpdateTime = 0f;
            
            IsInitialized = false;
        }

        public List<BuildingData> GetBuildingsInRange(Vector3 center, float radius, int playerId = -1)
        {
            var result = new List<BuildingData>();
            float radiusSquared = radius * radius;

            foreach (var building in _buildings.Values)
            {
                if (playerId != -1 && GetBuildingPlayerId(building) != playerId)
                    continue;

                float distanceSquared = (building.Position - center).sqrMagnitude;
                if (distanceSquared <= radiusSquared)
                {
                    result.Add(building);
                }
            }

            return result;
        }

        public List<BuildingData> GetBuildingsOfType(BuildingType buildingType, int playerId = -1)
        {
            UpdateCacheIfNeeded();

            if (_typeBuildingsCache.TryGetValue(buildingType, out List<BuildingData> cachedBuildings))
            {
                if (playerId == -1)
                {
                    return new List<BuildingData>(cachedBuildings);
                }
                else
                {
                    return cachedBuildings.Where(b => GetBuildingPlayerId(b) == playerId).ToList();
                }
            }

            return new List<BuildingData>();
        }

        public List<BuildingData> GetPlayerBuildings(int playerId)
        {
            UpdateCacheIfNeeded();

            if (_playerBuildingsCache.TryGetValue(playerId, out List<BuildingData> cachedBuildings))
            {
                return new List<BuildingData>(cachedBuildings);
            }

            return new List<BuildingData>();
        }

        public BuildingData? GetBuildingData(int buildingId)
        {
            if (_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return building;
            }
            return null;
        }

        public bool BuildingExists(int buildingId)
        {
            return _buildings.ContainsKey(buildingId);
        }

        public Dictionary<BuildingType, int> GetBuildingCounts(int playerId)
        {
            var counts = new Dictionary<BuildingType, int>();

            foreach (var building in _buildings.Values)
            {
                if (GetBuildingPlayerId(building) == playerId)
                {
                    if (counts.ContainsKey(building.Type))
                    {
                        counts[building.Type]++;
                    }
                    else
                    {
                        counts[building.Type] = 1;
                    }
                }
            }

            return counts;
        }

        public bool CanPlaceBuilding(BuildingType buildingType, Vector3 position, int playerId)
        {
            var result = ValidateBuildingPlacement(buildingType, position, playerId);
            return result.IsValid;
        }

        public PlacementValidationResult ValidateBuildingPlacement(BuildingType buildingType, Vector3 position, int playerId)
        {
            // 获取建筑模板
            if (!_buildingTemplates.TryGetValue(buildingType, out BuildingTemplate template))
            {
                return new PlacementValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "建筑类型不存在",
                    ErrorType = PlacementError.RequiredTechMissing
                };
            }

            // 检查地形适宜性
            if (!IsTerrainSuitable(position, template.Size))
            {
                return new PlacementValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "地形不适合建造",
                    ErrorType = PlacementError.TerrainNotSuitable
                };
            }

            // 检查是否有障碍物
            if (HasObstacles(position, template.Size))
            {
                return new PlacementValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "位置被障碍物阻挡",
                    ErrorType = PlacementError.ObstacleBlocking
                };
            }

            // 检查与其他建筑的距离
            float minDistance = GetMinimumBuildingDistance(buildingType);
            var nearbyBuildings = GetBuildingsInRange(position, minDistance, playerId);
            if (nearbyBuildings.Count > 0)
            {
                return new PlacementValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "距离其他建筑太近",
                    ErrorType = PlacementError.TooCloseToEnemy
                };
            }

            // 检查菌毯覆盖（某些建筑需要菌毯）
            if (RequiresCreepCoverage(buildingType) && !HasCreepCoverage(position))
            {
                return new PlacementValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "需要菌毯覆盖",
                    ErrorType = PlacementError.NoCreepCoverage
                };
            }

            return new PlacementValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty,
                ErrorType = PlacementError.None
            };
        }

        public int GetNearestBuilding(Vector3 position, BuildingType buildingType, int playerId, float maxDistance = float.MaxValue)
        {
            int nearestId = -1;
            float nearestDistanceSquared = maxDistance * maxDistance;

            foreach (var building in _buildings.Values)
            {
                if (building.BuildingType != buildingType || GetBuildingPlayerId(building) != playerId)
                    continue;

                float distanceSquared = (building.Position - position).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestId = building.BuildingId;
                }
            }

            return nearestId;
        }

        public List<ProductionItem> GetProductionQueue(int buildingId)
        {
            // 简化实现，返回空队列
            // 实际实现中应该从建筑数据中获取生产队列
            return new List<ProductionItem>();
        }

        public BuildingState GetBuildingState(int buildingId)
        {
            if (_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return building.State;
            }
            return BuildingState.Destroyed;
        }

        public BuildingTemplate GetBuildingTemplate(BuildingType buildingType)
        {
            _buildingTemplates.TryGetValue(buildingType, out BuildingTemplate template);
            return template;
        }

        public float GetBuildingInfluenceRadius(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return 0f;
            }

            if (!_buildingTemplates.TryGetValue(building.Type, out BuildingTemplate template))
            {
                return 0f;
            }

            // 根据建筑类型和等级计算影响范围
            float baseRadius = Mathf.Max(template.Size.x, template.Size.y) * 0.5f;
            float levelMultiplier = 1.0f + (building.Level - 1) * 0.2f;
            
            return baseRadius * levelMultiplier;
        }

        // 缓存更新
        private void UpdateCacheIfNeeded()
        {
            if (Time.time - _lastCacheUpdateTime < CACHE_UPDATE_INTERVAL)
                return;

            UpdateCache();
            _lastCacheUpdateTime = Time.time;
        }

        private void UpdateCache()
        {
            _playerBuildingsCache.Clear();
            _typeBuildingsCache.Clear();

            foreach (var building in _buildings.Values)
            {
                // 按玩家缓存
                if (!_playerBuildingsCache.ContainsKey(GetBuildingPlayerId(building)))
                {
                    _playerBuildingsCache[GetBuildingPlayerId(building)] = new List<BuildingData>();
                }
                _playerBuildingsCache[GetBuildingPlayerId(building)].Add(building);

                // 按类型缓存
                if (!_typeBuildingsCache.ContainsKey(building.Type))
                {
                    _typeBuildingsCache[building.Type] = new List<BuildingData>();
                }
                _typeBuildingsCache[building.Type].Add(building);
            }
        }

        // 辅助方法
        private bool IsTerrainSuitable(Vector3 position, Vector2Int size)
        {
            // 简化实现：检查地面高度变化是否过大
            float maxHeightDifference = 2.0f;
            float centerHeight = GetTerrainHeight(position);
            
            for (int x = -size.x/2; x <= size.x/2; x++)
            {
                for (int z = -size.y/2; z <= size.y/2; z++)
                {
                    Vector3 checkPos = position + new Vector3(x, 0, z);
                    float height = GetTerrainHeight(checkPos);
                    if (Mathf.Abs(height - centerHeight) > maxHeightDifference)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        private bool HasObstacles(Vector3 position, Vector2Int size)
        {
            // 简化实现：使用物理检测
            Vector3 boxSize = new Vector3(size.x, 5f, size.y);
            Collider[] obstacles = Physics.OverlapBox(position, boxSize * 0.5f);
            
            return obstacles.Length > 0;
        }

        private float GetMinimumBuildingDistance(BuildingType buildingType)
        {
            // 根据建筑类型返回最小距离
            switch (buildingType)
            {
                case BuildingType.BioEnergyCore:
                    return 10f;
                case BuildingType.DefenseTower:
                    return 5f;
                default:
                    return 3f;
            }
        }

        private bool RequiresCreepCoverage(BuildingType buildingType)
        {
            // 某些建筑需要菌毯覆盖
            switch (buildingType)
            {
                case BuildingType.BioEnergyCore:
                case BuildingType.DefenseTower:
                    return false; // 这些建筑可以在任何地方建造
                default:
                    return true; // 其他建筑需要菌毯
            }
        }

        private bool HasCreepCoverage(Vector3 position)
        {
            // 简化实现：假设有菌毯覆盖
            // 实际实现中应该查询CreepManager
            return true;
        }

        private static int GetBuildingPlayerId(BuildingData b) => b.OwnerId;

        private float GetTerrainHeight(Vector3 position)
        {
            // 简化实现：使用射线检测地面高度
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
            {
                return hit.point.y;
            }
            return 0f;
        }
    }
}