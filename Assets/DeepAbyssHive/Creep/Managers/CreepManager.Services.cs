using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Services;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 服务模块
    /// 负责管理菌毯系统的各个服务组件
    /// </summary>
    public partial class CreepManager
    {
        #region 服务引用

        // 服务引用
        private ICreepGridService _gridService;
        private ICreepExpansionService _expansionService;
        private ICreepSourceService _sourceService;
        private ICreepNetworkService _networkService;
        private ICreepQueryService _queryService;
        private ICreepSimulationService _simulationService;

        #endregion

        #region 服务初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        private void InitializeServices()
        {
            Debug.Log($"[{_managerName}] 初始化菌毯服务");

            // 创建并初始化网格服务
            _gridService = new CreepGridService();
            _gridService.Initialize();
            _gridService.InitializeGrid(_gridWidth, _gridHeight, _gridCellSize);

            // 创建并初始化查询服务
            _queryService = new CreepQueryService(_gridService);
            _queryService.Initialize();

            // 创建并初始化源点服务
            _sourceService = new CreepSourceService(_gridService);
            _sourceService.Initialize();

            // 创建并初始化网络服务
            _networkService = new CreepNetworkService(_gridService);
            _networkService.Initialize();

            // 创建并初始化扩张服务
            _expansionService = new CreepExpansionService(_gridService, _sourceService, _networkService);
            _expansionService.Initialize();
            _expansionService.ExpansionRate = _expansionRate;
            _expansionService.ExpansionThreshold = _expansionThreshold;
            _expansionService.AutoExpansionEnabled = true;

            // 创建并初始化模拟服务
            _simulationService = new CreepSimulationService(_gridService, _sourceService, _expansionService, _networkService);
            _simulationService.Initialize();

            Debug.Log($"[{_managerName}] 菌毯服务初始化完成");
        }

        /// <summary>
        /// 清理服务
        /// </summary>
        private void CleanupServices()
        {
            Debug.Log($"[{_managerName}] 清理菌毯服务");

            // 按依赖关系反向清理
            _simulationService?.Cleanup();
            _expansionService?.Cleanup();
            _networkService?.Cleanup();
            _sourceService?.Cleanup();
            _queryService?.Cleanup();
            _gridService?.Cleanup();

            _simulationService = null;
            _expansionService = null;
            _networkService = null;
            _sourceService = null;
            _queryService = null;
            _gridService = null;

            Debug.Log($"[{_managerName}] 菌毯服务清理完成");
        }

        /// <summary>
        /// 更新服务
        /// </summary>
        private void UpdateServices(float deltaTime)
        {
            if (!IsInitialized) return;
            
            // 按依赖关系顺序更新
            _gridService?.Update(deltaTime);
            _sourceService?.Update(deltaTime);
            _expansionService?.Update(deltaTime);
            _networkService?.Update(deltaTime);
            _simulationService?.Update(deltaTime);
            _queryService?.Update(deltaTime);
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        private void PauseServices()
        {
            if (!IsInitialized) return;
            
            _expansionService?.SetPaused(true);
            _simulationService?.SetPaused(true);
            _networkService?.SetPaused(true);
        }

        /// <summary>
        /// 恢复服务
        /// </summary>
        private void ResumeServices()
        {
            if (!IsInitialized) return;
            
            _expansionService?.SetPaused(false);
            _simulationService?.SetPaused(false);
            _networkService?.SetPaused(false);
        }
        

        #endregion
    }
}