using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建筑建造服务实现
    /// 负责建筑的建造、升级、修理等命令操作
    /// </summary>
    public class BuildingConstructionService : IBuildingConstructionService, ICommandService, IService
    {
        // 建筑数据引用
        private readonly Dictionary<int, BuildingData> _buildings;
        private readonly Dictionary<int, GameObject> _buildingGameObjects;
        private readonly Dictionary<BuildingType, BuildingTemplate> _buildingTemplates;
        private readonly Dictionary<BuildingType, string> _buildingPrefabPaths;
        
        // 建造相关数据
        private readonly Dictionary<int, ConstructionData> _constructions = new Dictionary<int, ConstructionData>();
        private readonly Dictionary<int, UpgradeData> _upgrades = new Dictionary<int, UpgradeData>();
        private int _nextConstructionId = 1;
        
        // 配置参数
        private float _constructionSpeedMultiplier = 1.0f;
        private float _upgradeSpeedMultiplier = 1.0f;
        private float _repairSpeedMultiplier = 1.0f;

        // IService 介面實現
        public string ServiceName => "BuildingConstructionService";
        public bool IsInitialized { get; private set; }
        
        // ICommandService 介面實現
        public bool IsCommandAvailable => IsInitialized;

        public BuildingConstructionService(
            Dictionary<int, BuildingData> buildings,
            Dictionary<int, GameObject> buildingGameObjects,
            Dictionary<BuildingType, BuildingTemplate> buildingTemplates,
            Dictionary<BuildingType, string> buildingPrefabPaths)
        {
            _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            _buildingGameObjects = buildingGameObjects ?? throw new ArgumentNullException(nameof(buildingGameObjects));
            _buildingTemplates = buildingTemplates ?? throw new ArgumentNullException(nameof(buildingTemplates));
            _buildingPrefabPaths = buildingPrefabPaths ?? throw new ArgumentNullException(nameof(buildingPrefabPaths));
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            // 初始化建造相关配置
            _constructionSpeedMultiplier = 1.0f;
            _upgradeSpeedMultiplier = 1.0f;
            _repairSpeedMultiplier = 1.0f;
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized)
                return;

            // 清理所有进行中的建造和升级
            _constructions.Clear();
            _upgrades.Clear();
            _nextConstructionId = 1;
            
            IsInitialized = false;
        }

        public int StartConstruction(BuildingType buildingType, Vector3 position, int playerId, Quaternion? rotation = null)
        {
            if (!_buildingTemplates.TryGetValue(buildingType, out BuildingTemplate template))
            {
                Debug.LogError($"[BuildingConstructionService] 建筑模板不存在: {buildingType}");
                return -1;
            }

            int constructionId = _nextConstructionId++;
            var constructionData = new ConstructionData
            {
                ConstructionId = constructionId,
                BuildingType = buildingType,
                Position = position,
                Rotation = rotation ?? Quaternion.identity,
                PlayerId = playerId,
                StartTime = Time.time,
                TotalTime = template.ConstructionTime,
                Progress = 0f,
                State = ConstructionState.InProgress
            };

            _constructions[constructionId] = constructionData;
            Debug.Log($"[BuildingConstructionService] 开始建造: {buildingType} at {position}");
            return constructionId;
        }

        public bool CancelConstruction(int constructionId)
        {
            if (!_constructions.TryGetValue(constructionId, out ConstructionData construction))
            {
                return false;
            }

            _constructions.Remove(constructionId);
            Debug.Log($"[BuildingConstructionService] 取消建造: {constructionId}");
            return true;
        }

        public int CompleteConstruction(int constructionId)
        {
            if (!_constructions.TryGetValue(constructionId, out ConstructionData construction))
            {
                Debug.LogError($"[BuildingConstructionService] 建造不存在: {constructionId}");
                return -1;
            }

            if (!_buildingTemplates.TryGetValue(construction.BuildingType, out BuildingTemplate template))
            {
                Debug.LogError($"[BuildingConstructionService] 建筑模板不存在: {construction.BuildingType}");
                return -1;
            }

            // 创建建筑数据
            int buildingId = GenerateNewBuildingId();
            var buildingData = new BuildingData
            {
                BuildingId = buildingId,
                Type = construction.BuildingType,
                Position = construction.Position,
                Rotation = construction.Rotation,
                // PlayerId 已移除，由 IBuildingManager 管理归属关系
                Level = 1,
                Health = template.MaxHealth,
                MaxHealth = template.MaxHealth,
                State = DeepAbyssHive.Buildings.Compat.BuildingStateCompat.Active,
                ConstructionTime = construction.TotalTime,
                LastUpdateTime = Time.time
            };

            _buildings[buildingId] = buildingData;

            // 创建游戏对象
            GameObject buildingObject = CreateBuildingGameObject(buildingData);
            if (buildingObject != null)
            {
                _buildingGameObjects[buildingId] = buildingObject;
            }

            // 移除建造数据
            _constructions.Remove(constructionId);

            Debug.Log($"[BuildingConstructionService] 完成建造: {construction.BuildingType} -> BuildingId={buildingId}");
            return buildingId;
        }

        public bool UpgradeBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return false;
            }

            if (!_buildingTemplates.TryGetValue(building.Type, out BuildingTemplate template))
            {
                return false;
            }

            if (building.Level >= template.MaxLevel)
            {
                Debug.LogWarning($"[BuildingConstructionService] 建筑已达最高等级: {buildingId}");
                return false;
            }

            if (_upgrades.ContainsKey(buildingId))
            {
                Debug.LogWarning($"[BuildingConstructionService] 建筑正在升级中: {buildingId}");
                return false;
            }

            var upgradeData = new UpgradeData
            {
                BuildingId = buildingId,
                FromLevel = building.Level,
                ToLevel = building.Level + 1,
                StartTime = Time.time,
                TotalTime = template.ConstructionTime * 0.8f, // 升级时间为建造时间的80%
                Progress = 0f,
                State = UpgradeState.InProgress
            };

            _upgrades[buildingId] = upgradeData;
            Debug.Log($"[BuildingConstructionService] 开始升级建筑: {buildingId} Level {building.Level} -> {building.Level + 1}");
            return true;
        }

        public bool CancelUpgrade(int buildingId)
        {
            if (!_upgrades.ContainsKey(buildingId))
            {
                return false;
            }

            _upgrades.Remove(buildingId);
            Debug.Log($"[BuildingConstructionService] 取消升级: {buildingId}");
            return true;
        }

        public bool RepairBuilding(int buildingId, float repairAmount = -1f)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return false;
            }

            if (building.Health >= building.MaxHealth)
            {
                return true; // 已满血
            }

            float actualRepairAmount = repairAmount < 0 ? building.MaxHealth : repairAmount;
            building.Health = Mathf.Min(building.Health + actualRepairAmount, building.MaxHealth);
            
            _buildings[buildingId] = building;
            Debug.Log($"[BuildingConstructionService] 修理建筑: {buildingId} Health={building.Health}/{building.MaxHealth}");
            return true;
        }

        public bool DestroyBuilding(int buildingId)
        {
            if (!_buildings.ContainsKey(buildingId))
            {
                return false;
            }

            // 销毁游戏对象
            if (_buildingGameObjects.TryGetValue(buildingId, out GameObject buildingObject) && buildingObject != null)
            {
                UnityEngine.Object.Destroy(buildingObject);
                _buildingGameObjects.Remove(buildingId);
            }

            // 移除建筑数据
            _buildings.Remove(buildingId);

            // 移除相关升级数据
            _upgrades.Remove(buildingId);

            Debug.Log($"[BuildingConstructionService] 销毁建筑: {buildingId}");
            return true;
        }

        public bool SetBuildingState(int buildingId, BuildingState state)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return false;
            }

            building.State = state;
            _buildings[buildingId] = building;
            Debug.Log($"[BuildingConstructionService] 设置建筑状态: {buildingId} -> {state}");
            return true;
        }

        public bool SetBuildingPaused(int buildingId, bool paused)
        {
            return SetBuildingState(buildingId, paused ? BuildingState.Paused : DeepAbyssHive.Buildings.Compat.BuildingStateCompat.Active);
        }

        public float GetConstructionProgress(int constructionId)
        {
            if (!_constructions.TryGetValue(constructionId, out ConstructionData construction))
            {
                return 0f;
            }

            float elapsed = Time.time - construction.StartTime;
            return Mathf.Clamp01(elapsed / construction.TotalTime);
        }

        public float GetUpgradeProgress(int buildingId)
        {
            if (!_upgrades.TryGetValue(buildingId, out UpgradeData upgrade))
            {
                return 0f;
            }

            float elapsed = Time.time - upgrade.StartTime;
            return Mathf.Clamp01(elapsed / upgrade.TotalTime);
        }

        public bool AccelerateConstruction(int constructionId, float speedMultiplier)
        {
            if (!_constructions.TryGetValue(constructionId, out ConstructionData construction))
            {
                return false;
            }

            construction.TotalTime /= speedMultiplier;
            _constructions[constructionId] = construction;
            return true;
        }

        public bool AccelerateUpgrade(int buildingId, float speedMultiplier)
        {
            if (!_upgrades.TryGetValue(buildingId, out UpgradeData upgrade))
            {
                return false;
            }

            upgrade.TotalTime /= speedMultiplier;
            _upgrades[buildingId] = upgrade;
            return true;
        }

        // 辅助方法
        private int GenerateNewBuildingId()
        {
            int id = 1;
            while (_buildings.ContainsKey(id))
            {
                id++;
            }
            return id;
        }

        private GameObject CreateBuildingGameObject(BuildingData buildingData)
        {
            GameObject buildingObject = new GameObject($"Building_{buildingData.BuildingId}_{buildingData.Type}");
            buildingObject.transform.position = buildingData.Position;
            buildingObject.transform.rotation = buildingData.Rotation;
            
            // 这里可以加载预制体或添加组件
            // 暂时使用简单的立方体表示
            var renderer = buildingObject.AddComponent<MeshRenderer>();
            var filter = buildingObject.AddComponent<MeshFilter>();
            filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            
            return buildingObject;
        }

        // 更新方法（由Manager调用）
        public void Update(float deltaTime)
        {
            UpdateConstructions(deltaTime);
            UpdateUpgrades(deltaTime);
        }

        private void UpdateConstructions(float deltaTime)
        {
            var completedConstructions = new List<int>();
            
            foreach (var kvp in _constructions)
            {
                var construction = kvp.Value;
                float progress = GetConstructionProgress(kvp.Key);
                
                if (progress >= 1.0f)
                {
                    completedConstructions.Add(kvp.Key);
                }
            }

            // 自动完成建造
            foreach (int constructionId in completedConstructions)
            {
                CompleteConstruction(constructionId);
            }
        }

        private void UpdateUpgrades(float deltaTime)
        {
            var completedUpgrades = new List<int>();
            
            foreach (var kvp in _upgrades)
            {
                var upgrade = kvp.Value;
                float progress = GetUpgradeProgress(kvp.Key);
                
                if (progress >= 1.0f)
                {
                    completedUpgrades.Add(kvp.Key);
                }
            }

            // 自动完成升级
            foreach (int buildingId in completedUpgrades)
            {
                CompleteUpgrade(buildingId);
            }
        }

        private void CompleteUpgrade(int buildingId)
        {
            if (!_upgrades.TryGetValue(buildingId, out UpgradeData upgrade))
            {
                return;
            }

            if (!_buildings.TryGetValue(buildingId, out BuildingData building))
            {
                return;
            }

            // 升级建筑
            building.Level = upgrade.ToLevel;
            building.MaxHealth *= 1.2f; // 升级后血量增加20%
            building.Health = building.MaxHealth; // 升级后满血
            _buildings[buildingId] = building;

            // 移除升级数据
            _upgrades.Remove(buildingId);

            Debug.Log($"[BuildingConstructionService] 完成升级: {buildingId} -> Level {building.Level}");
        }
    }

    // 建造数据结构
    public struct ConstructionData
    {
        public int ConstructionId;
        public BuildingType BuildingType;
        public Vector3 Position;
        public Quaternion Rotation;
        public int PlayerId;
        public float StartTime;
        public float TotalTime;
        public float Progress;
        public ConstructionState State;
    }

    // 升级数据结构
    public struct UpgradeData
    {
        public int BuildingId;
        public int FromLevel;
        public int ToLevel;
        public float StartTime;
        public float TotalTime;
        public float Progress;
        public UpgradeState State;
    }

    // 建造状态
    public enum ConstructionState
    {
        InProgress,
        Paused,
        Completed,
        Cancelled
    }

    // 升级状态
    public enum UpgradeState
    {
        InProgress,
        Paused,
        Completed,
        Cancelled
    }
}