using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using CreepSourceType = DeepAbyssHive.Creep.Data.CreepSourceType;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯模拟服务实现
    /// 提供菌毯生长、衰减、修改等功能
    /// </summary>
    public class CreepSimulationService : ICreepSimulationService, IService
    {
        #region 属性

        public string ServiceName => "CreepSimulationService";
        public bool IsInitialized { get; private set; }
        public bool IsCommandAvailable => IsInitialized && !_simulationPaused;

        #endregion

        #region 私有字段
        
        private ICreepGridService _gridService;
        private ICreepSourceService _sourceService;
        private ICreepExpansionService _expansionService;
        private ICreepNetworkService _networkService;
        
        private bool _simulationPaused = false;
        private float _globalGrowthMultiplier = 1f;
        private float _globalDecayMultiplier = 1f;
        
        #endregion

        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CreepSimulationService(
            ICreepGridService gridService,
            ICreepSourceService sourceService,
            ICreepExpansionService expansionService,
            ICreepNetworkService networkService)
        {
            _gridService = gridService;
            _sourceService = sourceService;
            _expansionService = expansionService;
            _networkService = networkService;
        }
        
        #endregion

        #region ICreepSimulationService 实现

        /// <summary>
        /// 添加菌毯源点
        /// </summary>
        public int AddCreepSource(Vector3 position, int playerId, float strength = 1f, float radius = 10f)
        {
            if (!IsInitialized || _simulationPaused)
                return -1;

            var sourceData = new CreepSource
            {
                Position = position,
                NetworkId = playerId,
                Strength = Mathf.Clamp01(strength),
                Radius = Mathf.Max(0f, radius),
                IsActive = true,
                SourceType = CreepSourceType.Manual
            };

            return _sourceService.CreateCreepSource(sourceData.Position, sourceData.NetworkId, sourceData.SourceType, sourceData.Strength);
        }

        /// <summary>
        /// 移除菌毯源点
        /// </summary>
        public bool RemoveCreepSource(int sourceId)
        {
            if (!IsInitialized)
                return false;

            return _sourceService.RemoveCreepSource(sourceId);
        }

        /// <summary>
        /// 修改菌毯源点属性
        /// </summary>
        public bool ModifyCreepSource(int sourceId, float? strength = null, float? radius = null)
        {
            if (!IsInitialized)
                return false;

            var source = _sourceService.GetCreepSource(sourceId);
            
            if (strength.HasValue)
                _sourceService.UpdateSourceStrength(sourceId, Mathf.Clamp01(strength.Value));
            
            if (radius.HasValue)
            {
                // Note: 新介面沒有直接更新半徑的方法，半徑由強度和類型決定
                Debug.LogWarning("UpdateSource: 半徑更新已改為由強度和類型自動計算");
            }

            return true;
        }

        /// <summary>
        /// 强制菌毯生长
        /// </summary>
        public bool ForceCreepGrowth(Vector3 position, float radius, float strength, int playerId)
        {
            if (!IsInitialized || _simulationPaused)
                return false;

            var gridPos = _gridService.WorldToGridPosition(position);
            var gridRadius = Mathf.CeilToInt(radius / _gridService.GridCellSize);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int z = -gridRadius; z <= gridRadius; z++)
                {
                    var targetPos = gridPos + new Vector2Int(x, z);
                    var distance = Vector2.Distance(gridPos, targetPos) * _gridService.GridCellSize;
                    
                    if (distance <= radius)
                    {
                        var falloff = 1f - (distance / radius);
                        var finalStrength = strength * falloff;
                        
                        var cell = _gridService.GetGridCell(targetPos);
                        if (cell != null)
                        {
                            cell.Strength = Mathf.Max(cell.Strength, finalStrength);
                            cell.NetworkId = playerId;
                            cell.IsActive = true;
                            _gridService.SetGridCell(targetPos, cell);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 强制菌毯衰减
        /// </summary>
        public bool ForceCreepDecay(Vector3 position, float radius, float decayRate = 1f)
        {
            if (!IsInitialized || _simulationPaused)
                return false;

            var gridPos = _gridService.WorldToGridPosition(position);
            var gridRadius = Mathf.CeilToInt(radius / _gridService.GridCellSize);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int z = -gridRadius; z <= gridRadius; z++)
                {
                    var targetPos = gridPos + new Vector2Int(x, z);
                    var distance = Vector2.Distance(gridPos, targetPos) * _gridService.GridCellSize;
                    
                    if (distance <= radius)
                    {
                        var falloff = 1f - (distance / radius);
                        var finalDecayRate = decayRate * falloff;
                        
                        var cell = _gridService.GetGridCell(targetPos);
                        if (cell != null && cell.IsActive)
                        {
                            cell.Strength = Mathf.Max(0f, cell.Strength - finalDecayRate * Time.deltaTime);
                            if (cell.Strength <= 0.01f)
                            {
                                cell.IsActive = false;
                                cell.Strength = 0f;
                            }
                            _gridService.SetGridCell(targetPos, cell);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 清除指定区域的菌毯
        /// </summary>
        public bool ClearCreep(Vector3 position, float radius)
        {
            if (!IsInitialized)
                return false;

            var gridPos = _gridService.WorldToGridPosition(position);
            var gridRadius = Mathf.CeilToInt(radius / _gridService.GridCellSize);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int z = -gridRadius; z <= gridRadius; z++)
                {
                    var targetPos = gridPos + new Vector2Int(x, z);
                    var distance = Vector2.Distance(gridPos, targetPos) * _gridService.GridCellSize;
                    
                    if (distance <= radius)
                    {
                        var cell = _gridService.GetGridCell(targetPos);
                        if (cell != null)
                        {
                            cell.IsActive = false;
                            cell.Strength = 0f;
                            cell.Density = 0f;
                            _gridService.SetGridCell(targetPos, cell);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 设置菌毯强度
        /// </summary>
        public bool SetCreepStrength(Vector3 position, float radius, float strength, int playerId)
        {
            if (!IsInitialized)
                return false;

            var gridPos = _gridService.WorldToGridPosition(position);
            var gridRadius = Mathf.CeilToInt(radius / _gridService.GridCellSize);
            var clampedStrength = Mathf.Clamp01(strength);

            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int z = -gridRadius; z <= gridRadius; z++)
                {
                    var targetPos = gridPos + new Vector2Int(x, z);
                    var distance = Vector2.Distance(gridPos, targetPos) * _gridService.GridCellSize;
                    
                    if (distance <= radius)
                    {
                        var cell = _gridService.GetGridCell(targetPos);
                        if (cell != null)
                        {
                            cell.Strength = clampedStrength;
                            cell.NetworkId = playerId;
                            cell.IsActive = clampedStrength > 0.01f;
                            _gridService.SetGridCell(targetPos, cell);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 连接菌毯网络
        /// </summary>
        public bool ConnectCreepNetworks(Vector3 position1, Vector3 position2, int playerId, float strength = 0.5f)
        {
            if (!IsInitialized || _simulationPaused)
                return false;

            // 简单的直线连接实现
            var distance = Vector3.Distance(position1, position2);
            var steps = Mathf.CeilToInt(distance / _gridService.GridCellSize);
            
            for (int i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var currentPos = Vector3.Lerp(position1, position2, t);
                var gridPos = _gridService.WorldToGridPosition(currentPos);
                
                var cell = _gridService.GetGridCell(gridPos);
                if (cell != null)
                {
                    cell.Strength = Mathf.Max(cell.Strength, strength);
                    cell.NetworkId = playerId;
                    cell.IsActive = true;
                    _gridService.SetGridCell(gridPos, cell);
                }
            }

            return true;
        }

        /// <summary>
        /// 分割菌毯网络
        /// </summary>
        public bool SplitCreepNetwork(Vector3 position, float radius)
        {
            if (!IsInitialized)
                return false;

            // 清除指定区域来分割网络
            return ClearCreep(position, radius);
        }

        /// <summary>
        /// 合并菌毯网络
        /// </summary>
        public bool MergeCreepNetworks(int networkId1, int networkId2)
        {
            if (!IsInitialized)
                return false;

            return _networkService.MergeNetworks(networkId1, networkId2);
        }

        /// <summary>
        /// 设置菌毯生长速度
        /// </summary>
        public void SetCreepGrowthSpeed(int playerId, float speedMultiplier)
        {
            if (!IsInitialized)
                return;

            _globalGrowthMultiplier = Mathf.Max(0f, speedMultiplier);
            // 注意：这里假设 ICreepExpansionService 有这个方法，如果没有需要移除或修改
            // _expansionService.SetGrowthSpeedMultiplier(playerId, speedMultiplier);
        }

        /// <summary>
        /// 设置菌毯衰减速度
        /// </summary>
        public void SetCreepDecaySpeed(int playerId, float decayMultiplier)
        {
            if (!IsInitialized)
                return;

            _globalDecayMultiplier = Mathf.Max(0f, decayMultiplier);
        }

        /// <summary>
        /// 暂停/恢复菌毯模拟
        /// </summary>
        public void SetSimulationPaused(bool paused)
        {
            _simulationPaused = paused;
            _expansionService?.SetPaused(paused);
        }

        /// <summary>
        /// 设置暂停状态（ICreepSimulationService 接口要求）
        /// </summary>
        public void SetPaused(bool paused)
        {
            SetSimulationPaused(paused);
        }

        /// <summary>
        /// 重置菌毯模拟
        /// </summary>
        public void ResetCreepSimulation(int playerId = -1)
        {
            if (!IsInitialized)
                return;

            if (playerId == -1)
            {
                // 重置所有玩家的菌毯
                _gridService.ClearGrid();
                // 注意：这里假设服务有这些方法，如果没有需要移除或修改
                // _sourceService.ClearAllSources();
                // _networkService.ClearAllNetworks();
            }
            else
            {
                // 重置指定玩家的菌毯
                // 注意：这里假设服务有这些方法，如果没有需要移除或修改
                // _sourceService.ClearPlayerSources(playerId);
                // _networkService.ClearPlayerNetworks(playerId);
                
                // 清除该玩家的网格数据
                for (int x = 0; x < _gridService.GridWidth; x++)
                {
                    for (int z = 0; z < _gridService.GridHeight; z++)
                    {
                        var cell = _gridService.GetGridCell(new Vector2Int(x, z));
                        if (cell != null && cell.NetworkId == playerId)
                        {
                            cell.IsActive = false;
                            cell.Strength = 0f;
                            cell.Density = 0f;
                            _gridService.SetGridCell(new Vector2Int(x, z), cell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 保存菌毯状态
        /// </summary>
        public bool SaveCreepState(string filePath)
        {
            if (!IsInitialized)
                return false;

            try
            {
                // TODO: 实现菌毯状态序列化和保存
                Debug.LogWarning("[CreepSimulationService] SaveCreepState 功能待实现");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreepSimulationService] 保存菌毯状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载菌毯状态
        /// </summary>
        public bool LoadCreepState(string filePath)
        {
            if (!IsInitialized)
                return false;

            try
            {
                // TODO: 实现菌毯状态反序列化和加载
                Debug.LogWarning("[CreepSimulationService] LoadCreepState 功能待实现");
                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreepSimulationService] 加载菌毯状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 优化菌毯网络
        /// </summary>
        public void OptimizeCreepNetworks(int playerId)
        {
            if (!IsInitialized)
                return;

            _networkService.OptimizeNetworkStructure(playerId);
        }

        #endregion

        #region IService 实现

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                return;

            Debug.Log("[CreepSimulationService] 初始化菌毯模拟服务");
            
            IsInitialized = true;
            
            Debug.Log("[CreepSimulationService] 菌毯模拟服务初始化完成");
        }

        /// <summary>
        /// 更新服务
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsInitialized || _simulationPaused)
                return;

            // 模拟服务主要协调其他服务，本身不需要太多更新逻辑
        }

        /// <summary>
        /// 清理服务
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized)
                return;

            Debug.Log("[CreepSimulationService] 清理菌毯模拟服务");
            
            _simulationPaused = false;
            _globalGrowthMultiplier = 1f;
            _globalDecayMultiplier = 1f;
            
            IsInitialized = false;
            
            Debug.Log("[CreepSimulationService] 菌毯模拟服务清理完成");
        }

        #endregion
    }

    public partial class CreepSimulationService
    {
        public CreepSimulationService()
            : this(DeepAbyssHive.Core.Services.ServiceLocator.Get<DeepAbyssHive.Creep.Services.ICreepGridService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<DeepAbyssHive.Creep.Interfaces.ICreepSourceService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<DeepAbyssHive.Creep.Services.ICreepExpansionService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<DeepAbyssHive.Creep.Services.ICreepNetworkService>()) { }
    }
}