using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.Buildings.Managers;
using DeepAbyssHive.Creep.Config;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 核心模块
    /// 负责菌毯系统的初始化、清理和基础数据管理
    /// </summary>
    public partial class CreepManager : MonoBehaviour, IManager
    {
        #region IManager 实现
        
        public string ManagerName => _managerName;
        public bool IsInitialized { get; private set; }
        public bool IsPaused { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;

            Debug.Log("[CreepManager] 初始化菌毯管理器");
            
            // 加载配置
            LoadConfiguration();
            
            // 初始化服务
            InitializeServices();
            
            // 获取其他管理器引用
            // TODO: 需要通过依赖注入或其他方式获取BuildingManager引用
            // _buildingManager = GameManager.Instance.GetManager<BuildingManager>();
            Debug.Log("[CreepManager] BuildingManager引用暂时禁用，等待依赖注入实现");
            
            IsInitialized = true;
            Debug.Log("[CreepManager] 菌毯管理器初始化完成");
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            Debug.Log("[CreepManager] 清理菌毯管理器");
            
            // 清理服务
            CleanupServices();
            
            IsInitialized = false;
        }

        public void Pause()
        {
            IsPaused = true;
            PauseServices();
        }

        public void Resume()
        {
            IsPaused = false;
            ResumeServices();
        }

        public void Update(float deltaTime)
        {
            if (!IsInitialized || IsPaused) return;
            
            // 更新服务
            UpdateServices(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新逻辑
        }

        public void LateUpdate(float deltaTime)
        {
            // 延迟更新逻辑
        }

        public string GetManagerName()
        {
            return ManagerName;
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(ICreepGridService))
                return _gridService as T;
            if (typeof(T) == typeof(ICreepQueryService))
                return _queryService as T;
            if (typeof(T) == typeof(ICreepExpansionService))
                return _expansionService as T;
            if (typeof(T) == typeof(ICreepSourceService))
                return _sourceService as T;
            if (typeof(T) == typeof(ICreepNetworkService))
                return _networkService as T;
            
            return null;
        }

        #endregion

        #region 私有字段定义
        
        // 配置参数（从配置加载或使用默认值）
        private float _gridCellSize = 1f;
        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private float _expansionRate = 1f;
        private float _decayRate = 0.05f;
        private float _expansionThreshold = 0.8f;
        private float _minDecayDensity = 0.1f;
        private int _batchSize = 100;
        private float _updateInterval = 0.1f;
        private float _networkCheckInterval = 2f;
        
        private string _managerName = "CreepManager";
        private BuildingManager _buildingManager;
        private CreepConfigSO _config;
        
        // 事件定义
        public System.Action<CreepStatistics> OnStatisticsUpdated;
        
        // 上次更新时间
        private float _lastUpdateTime;
        private float _lastNetworkCheckTime;

        #endregion

        #region 配置加载

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<CreepConfigSO>("CreepConfig");
            
            if (_config != null)
            {
                // 从配置加载参数
                _gridCellSize = _config.gridCellSize;
                _gridWidth = _config.gridWidth;
                _gridHeight = _config.gridHeight;
                _expansionRate = _config.expansionRate;
                _decayRate = _config.decayRate;
                _expansionThreshold = _config.expansionThreshold;
                _minDecayDensity = _config.minDecayDensity;
                _batchSize = _config.batchSize;
                _updateInterval = _config.updateInterval;
                _networkCheckInterval = _config.networkCheckInterval;
                
                Debug.Log($"[{_managerName}] 从配置加载菌毯参数：网格({_gridWidth}x{_gridHeight})，单元格大小({_gridCellSize})");
            }
            else
            {
                // 使用默认值
                _gridCellSize = 1f;
                _gridWidth = 100;
                _gridHeight = 100;
                _expansionRate = 1f;
                _decayRate = 0.05f;
                _expansionThreshold = 0.8f;
                _minDecayDensity = 0.1f;
                _batchSize = 100;
                _updateInterval = 0.1f;
                _networkCheckInterval = 2f;
                
                Debug.LogWarning($"[{_managerName}] 未找到CreepConfig配置，使用默认参数");
            }
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 获取菌毯统计信息
        /// </summary>
        public CreepStatistics GetCreepStatistics()
        {
            if (!IsInitialized) return new CreepStatistics();
            
            // 委托给查询服务
            return _queryService.GetCreepStatistics(-1);
        }


        #endregion
    }

    #region 数据结构定义

    /// <summary>
    /// 菌毯统计信息
    /// </summary>
    [System.Serializable]
    public class CreepStatistics
    {
        public int TotalTiles;
        public int ActiveTiles;
        public float TotalCoverage;
        public float TotalArea;
        public float TotalHealth;
        public float AverageHealth;
        public float TotalResourcesGenerated;
        public int ConnectedRegions;
        
        // 按状态分类
        public int HealthyTiles;
        public int GrowingTiles;
        public int StarvingTiles;
        public int DyingTiles;
        
        // 按类型分类
        public int BasicTiles;
        public int EnhancedTiles;
        public int SpecializedTiles;
    }

    #endregion
}