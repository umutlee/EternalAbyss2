using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.Units.Config;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Units.Services;
using IUnitManager = DeepAbyssHive.Units.Interfaces.IUnitManager;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器核心部分 - 作为服务容器和API适配器
    /// </summary>
    public partial class UnitManager : IUnitManager, IManager
    {
        #region 服务依赖
        private IUnitQueryService _queryService;
        private IUnitCommandService _commandService;
        #endregion

        #region 私有字段
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private string _managerName = "UnitManager";
        #endregion

        #region IManager属性实现
        public string ManagerName => _managerName;
        public bool IsInitialized => _isInitialized;
        public bool IsPaused => _isPaused;
        #endregion

        #region IManager接口实现
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 初始化单位管理器");
            
            // 创建服务实例
            _queryService = new UnitQueryService();
            _commandService = new UnitCommandService();
            
            // 初始化服务
            _queryService.Initialize();
            _commandService.Initialize();
            
            _isInitialized = true;
            Debug.Log($"[{_managerName}] 单位管理器初始化完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 委托给命令服务处理更新
            _commandService?.Update(deltaTime);
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 委托给命令服务处理固定更新
            _commandService?.FixedUpdate(fixedDeltaTime);
        }

        /// <summary>
        /// 后更新管理器（带参数）
        /// </summary>
        public void LateUpdate(float deltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 委托给命令服务处理后更新
            _commandService?.LateUpdate(deltaTime);
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_managerName}] 清理单位管理器");
            
            // 清理服务
            _commandService?.Cleanup();
            _queryService?.Cleanup();
            
            _queryService = null;
            _commandService = null;
            _isInitialized = false;
            
            Debug.Log($"[{_managerName}] 单位管理器清理完成");
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            if (_isPaused)
                return;
                
            _isPaused = true;
            _commandService?.SetPaused(true);
            Debug.Log($"[{_managerName}] 单位管理器已暂停");
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
                return;
                
            _isPaused = false;
            _commandService?.SetPaused(false);
            Debug.Log($"[{_managerName}] 单位管理器已恢复");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return _managerName;
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IUnitQueryService))
                return _queryService as T;
            if (typeof(T) == typeof(IUnitCommandService))
                return _commandService as T;
            
            return null;
        }
        #endregion

        #region API 兼容性方法 - 委托给服务处理
        /// <summary>
        /// 创建单位 - 委托给命令服务
        /// </summary>
        public int CreateUnit(UnitType type, Vector3 position, int ownerId)
        {
            return _commandService?.CreateUnit(type, position, ownerId) ?? -1;
        }

        /// <summary>
        /// 销毁单位 - 委托给命令服务
        /// </summary>
        public void DestroyUnit(int unitId)
        {
            _commandService?.DestroyUnit(unitId);
        }

        /// <summary>
        /// 移动单位 - 委托给命令服务
        /// </summary>
        public void MoveUnit(int unitId, Vector3 targetPosition)
        {
            _commandService?.MoveUnit(unitId, targetPosition);
        }

        /// <summary>
        /// 攻击目标 - 委托给命令服务
        /// </summary>
        public void AttackTarget(int unitId, int targetId)
        {
            _commandService?.AttackTarget(unitId, targetId);
        }

        /// <summary>
        /// 获取范围内的单位 - 委托给查询服务
        /// </summary>
        public NativeArray<int> GetUnitsInRange(Vector3 position, float radius)
        {
            return _queryService?.GetUnitsInRange(position, radius) ?? new NativeArray<int>(0, Allocator.Temp);
        }

        /// <summary>
        /// 获取指定类型的单位 - 委托给查询服务
        /// </summary>
        public NativeArray<int> GetUnitsOfType(UnitType type, int ownerId)
        {
            return _queryService?.GetUnitsOfType(type, ownerId) ?? new NativeArray<int>(0, Allocator.Temp);
        }

        /// <summary>
        /// 进化单位 - 委托给命令服务
        /// </summary>
        public bool EvolveUnit(int unitId, UnitType targetType)
        {
            return _commandService?.EvolveUnit(unitId, targetType) ?? false;
        }

        /// <summary>
        /// 获取单位数据 - 委托给查询服务
        /// </summary>
        public UnitData GetUnit(int unitId)
        {
            return _queryService?.GetUnit(unitId);
        }

        /// <summary>
        /// 获取范围内单位 - 委托给查询服务
        /// </summary>
        public NativeArray<UnitData> GetUnitsInRange(Vector3 center, float radius, Allocator allocator = Allocator.Temp)
        {
            return _queryService?.GetUnitsInRange(center, radius, allocator) ?? new NativeArray<UnitData>(0, allocator);
        }

        /// <summary>
        /// 获取指定类型单位 - 委托给查询服务
        /// </summary>
        public NativeArray<UnitData> GetUnitsOfType(UnitType unitType, Allocator allocator = Allocator.Temp)
        {
            return _queryService?.GetUnitsOfType(unitType, allocator) ?? new NativeArray<UnitData>(0, allocator);
        }

        /// <summary>
        /// 获取所有单位 - 委托给查询服务
        /// </summary>
        public NativeArray<UnitData> GetAllUnits(Allocator allocator = Allocator.Temp)
        {
            return _queryService?.GetAllUnits(allocator) ?? new NativeArray<UnitData>(0, allocator);
        }

        /// <summary>
        /// 获取单位总数 - 委托给查询服务
        /// </summary>
        public int GetUnitCount()
        {
            return _queryService?.GetUnitCount() ?? 0;
        }

        /// <summary>
        /// 检查单位是否存在 - 委托给查询服务
        /// </summary>
        public bool HasUnit(int unitId)
        {
            return _queryService?.HasUnit(unitId) ?? false;
        }
        #endregion
    }
}