using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Managers;
using DeepAbyssHive.Core.Logging;
using IBuildingManager = DeepAbyssHive.Buildings.Interfaces.IBuildingManager;

namespace DeepAbyssHive.Core.Managers
{
    /// <summary>
    /// 游戏主管理器 - 管理器模块
    /// 负责管理器和系统的注册、注销、批次初始化与更新桥接
    /// </summary>
    public partial class GameManager
    {
        #region 系统管理器引用
        
        [Header("系统管理器预制体")]
        [SerializeField] private GameObject _buildingManagerPrefab;
        [SerializeField] private GameObject _creepManagerPrefab;
        [SerializeField] private GameObject _unitManagerPrefab;
        [SerializeField] private GameObject _terrainManagerPrefab;
        [SerializeField] private GameObject _resourceManagerPrefab;
        [SerializeField] private GameObject _spatialIndexManagerPrefab;
        
        [Header("系统配置")]
        [SerializeField] private bool _enableMultiThreading = true;
        [SerializeField] private bool _enableGPUInstancing = true;
        [SerializeField] private int _maxUnitsPerPlayer = 1000;
        [SerializeField] private float _tickRate = 20f; // 每秒20次逻辑更新
        
        // 系统管理器实例
        private IBuildingManager _buildingManager;
        private ICreepManager _creepManager;
        private IUnitManager _unitManager;
        private ITerrainManager _terrainManager;
        private IResourceManager _resourceManager;
        private SpatialIndexManager _spatialIndexManager;
        
        // 管理器和系统注册表
        private Dictionary<string, IManager> _registeredManagers = new Dictionary<string, IManager>();
        private Dictionary<string, ISystem> _registeredSystems = new Dictionary<string, ISystem>();
        private List<IManager> _updateableManagers = new List<IManager>();
        private List<ISystem> _updateableSystems = new List<ISystem>();
        
        // Tick系统
        private float _gameTime = 0f;
        private int _currentTick = 0;
        private float _tickTimer = 0f;
        
        #endregion
        
        #region 属性访问器
        
        public IBuildingManager BuildingManager => _buildingManager;
        public ICreepManager CreepManager => _creepManager;
        public IUnitManager UnitManager => _unitManager;
        public ITerrainManager TerrainManager => _terrainManager;
        public IResourceManager ResourceManager => _resourceManager;
        public SpatialIndexManager SpatialIndexManager => _spatialIndexManager;
        
        public bool EnableMultiThreading => _enableMultiThreading;
        public bool EnableGPUInstancing => _enableGPUInstancing;
        public int MaxUnitsPerPlayer => _maxUnitsPerPlayer;
        public float TickRate => _tickRate;
        public float GameTime => _gameTime;
        public int CurrentTick => _currentTick;
        
        #endregion
        
        #region 管理器初始化
        
        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        private void InitializeAllManagers()
        {
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 开始初始化所有管理器...");
            
            try
            {
                // 按依赖顺序初始化管理器
                InitializeResourceManager();
                InitializeTerrainManager();
                InitializeSpatialIndexManager();
                InitializeCreepManager();
                InitializeBuildingManager();
                InitializeUnitManager();
                
                // 设置管理器之间的依赖关系
                SetupManagerDependencies();
                
                // 标记初始化完成
                _isInitialized = true;
                OnGameInitialized?.Invoke();
                
                DAHLog.Info(LogCategory.MANAGER, "[GameManager] 所有管理器初始化完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 管理器初始化失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        private void InitializeResourceManager()
        {
            try
            {
                var existingManager = FindObjectOfType<MonoBehaviour>() as IResourceManager;
                if (existingManager == null && _resourceManagerPrefab != null)
                {
                    var go = Instantiate(_resourceManagerPrefab, transform);
                    _resourceManager = go.GetComponent<IResourceManager>();
                }
                else
                {
                    _resourceManager = existingManager;
                }
                
                if (_resourceManager != null)
                {
                    RegisterManager(_resourceManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 资源管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 资源管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化地形管理器
        /// </summary>
        private void InitializeTerrainManager()
        {
            try
            {
                var existingManager = FindObjectOfType<MonoBehaviour>() as ITerrainManager;
                if (existingManager == null && _terrainManagerPrefab != null)
                {
                    var go = Instantiate(_terrainManagerPrefab, transform);
                    _terrainManager = go.GetComponent<ITerrainManager>();
                }
                else
                {
                    _terrainManager = existingManager;
                }
                
                if (_terrainManager != null)
                {
                    RegisterManager(_terrainManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 地形管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 地形管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化空间索引管理器
        /// </summary>
        private void InitializeSpatialIndexManager()
        {
            try
            {
                var existingManager = FindObjectOfType<SpatialIndexManager>();
                if (existingManager == null && _spatialIndexManagerPrefab != null)
                {
                    var go = Instantiate(_spatialIndexManagerPrefab, transform);
                    _spatialIndexManager = go.GetComponent<SpatialIndexManager>();
                }
                else
                {
                    _spatialIndexManager = existingManager;
                }
                
                if (_spatialIndexManager != null)
                {
                    RegisterManager(_spatialIndexManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 空间索引管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 空间索引管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化菌毯管理器
        /// </summary>
        private void InitializeCreepManager()
        {
            try
            {
                var existingManager = FindObjectOfType<MonoBehaviour>() as ICreepManager;
                if (existingManager == null && _creepManagerPrefab != null)
                {
                    var go = Instantiate(_creepManagerPrefab, transform);
                    _creepManager = go.GetComponent<ICreepManager>();
                }
                else
                {
                    _creepManager = existingManager;
                }
                
                if (_creepManager != null)
                {
                    RegisterManager(_creepManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 菌毯管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 菌毯管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化建筑管理器
        /// </summary>
        private void InitializeBuildingManager()
        {
            try
            {
                var existingManager = FindObjectOfType<MonoBehaviour>() as IBuildingManager;
                if (existingManager == null && _buildingManagerPrefab != null)
                {
                    var go = Instantiate(_buildingManagerPrefab, transform);
                    _buildingManager = go.GetComponent<IBuildingManager>();
                }
                else
                {
                    _buildingManager = existingManager;
                }
                
                if (_buildingManager != null)
                {
                    RegisterManager(_buildingManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 建筑管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 建筑管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化单位管理器
        /// </summary>
        private void InitializeUnitManager()
        {
            try
            {
                var existingManager = FindObjectOfType<MonoBehaviour>() as IUnitManager;
                if (existingManager == null && _unitManagerPrefab != null)
                {
                    var go = Instantiate(_unitManagerPrefab, transform);
                    _unitManager = go.GetComponent<IUnitManager>();
                }
                else
                {
                    _unitManager = existingManager;
                }
                
                if (_unitManager != null)
                {
                    RegisterManager(_unitManager);
                    DAHLog.Info(LogCategory.MANAGER, "[GameManager] 单位管理器初始化完成");
                }
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 单位管理器初始化失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 设置管理器之间的依赖关系
        /// </summary>
        private void SetupManagerDependencies()
        {
            try
            {
                // 设置空间索引依赖
                if (_unitManager != null && _spatialIndexManager != null)
                {
                    // UnitManager需要SpatialIndexManager
                }
                
                if (_buildingManager != null && _creepManager != null)
                {
                    // BuildingManager需要CreepManager
                }
                
                if (_creepManager != null && _spatialIndexManager != null)
                {
                    // CreepManager需要SpatialIndexManager
                }
                
                DAHLog.Info(LogCategory.MANAGER, "[GameManager] 管理器依赖关系设置完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 设置管理器依赖关系失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region IGameManager接口实现
        
        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                DAHLog.Warning(LogCategory.MANAGER, "[GameManager] 游戏管理器已经初始化");
                return;
            }
            
            InitializeAllManagers();
        }
        
        /// <summary>
        /// 注册管理器
        /// </summary>
        public void RegisterManager(IManager manager)
        {
            if (manager == null)
            {
                DAHLog.Warning(LogCategory.MANAGER, "[GameManager] 尝试注册空的管理器");
                return;
            }
            
            string managerName = manager.GetType().Name;
            _registeredManagers[managerName] = manager;
            
            // 如果管理器支持更新，添加到更新列表
            if (manager is IUpdatable)
            {
                if (!_updateableManagers.Contains(manager))
                {
                    _updateableManagers.Add(manager);
                }
            }
            
            DAHLog.Info(LogCategory.MANAGER, $"[GameManager] 注册管理器: {managerName}");
        }
        
        /// <summary>
        /// 注销管理器
        /// </summary>
        public void UnregisterManager(string managerName)
        {
            if (_registeredManagers.TryGetValue(managerName, out var manager))
            {
                _registeredManagers.Remove(managerName);
                _updateableManagers.Remove(manager);
                DAHLog.Info(LogCategory.MANAGER, $"[GameManager] 注销管理器: {managerName}");
            }
        }
        
        /// <summary>
        /// 获取管理器
        /// </summary>
        public T GetManager<T>() where T : IManager
        {
            var managerType = typeof(T);
            var manager = _registeredManagers.Values.FirstOrDefault(m => managerType.IsAssignableFrom(m.GetType()));
            return (T)manager;
        }
        
        /// <summary>
        /// 获取所有管理器
        /// </summary>
        public List<IManager> GetAllManagers()
        {
            return _registeredManagers.Values.ToList();
        }
        
        /// <summary>
        /// 注册系统
        /// </summary>
        public void RegisterSystem(ISystem system)
        {
            if (system == null)
            {
                DAHLog.Warning(LogCategory.MANAGER, "[GameManager] 尝试注册空的系统");
                return;
            }
            
            string systemName = system.GetType().Name;
            _registeredSystems[systemName] = system;
            
            // 如果系统支持更新，添加到更新列表
            if (system is IUpdatable)
            {
                if (!_updateableSystems.Contains(system))
                {
                    _updateableSystems.Add(system);
                }
            }
            
            DAHLog.Info(LogCategory.MANAGER, $"[GameManager] 注册系统: {systemName}");
        }
        
        /// <summary>
        /// 注销系统
        /// </summary>
        public void UnregisterSystem(string systemName)
        {
            if (_registeredSystems.TryGetValue(systemName, out var system))
            {
                _registeredSystems.Remove(systemName);
                _updateableSystems.Remove(system);
                DAHLog.Info(LogCategory.MANAGER, $"[GameManager] 注销系统: {systemName}");
            }
        }
        
        /// <summary>
        /// 获取系统
        /// </summary>
        public T GetSystem<T>() where T : ISystem
        {
            var systemType = typeof(T);
            var system = _registeredSystems.Values.FirstOrDefault(s => systemType.IsAssignableFrom(s.GetType()));
            return (T)system;
        }
        
        /// <summary>
        /// 获取所有系统
        /// </summary>
        public List<ISystem> GetAllSystems()
        {
            return _registeredSystems.Values.ToList();
        }
        
        /// <summary>
        /// 更新游戏
        /// </summary>
        void IGameManager.Update(float deltaTime)
        {
            _gameTime += deltaTime;
            _tickTimer += deltaTime;
            
            // Tick更新
            if (_tickTimer >= 1f / _tickRate)
            {
                _currentTick++;
                _tickTimer = 0f;
                
                // 执行Tick更新
                UpdateTick();
            }
            
            // 更新所有注册的管理器
            for (int i = 0; i < _updateableManagers.Count; i++)
            {
                try
                {
                    if (_updateableManagers[i] is IUpdatable updatable)
                    {
                        updatable.Update(deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 管理器更新失败: {_updateableManagers[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            // 更新所有注册的系统
            for (int i = 0; i < _updateableSystems.Count; i++)
            {
                try
                {
                    if (_updateableSystems[i] is IUpdatable updatable)
                    {
                        updatable.Update(deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 系统更新失败: {_updateableSystems[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 固定更新游戏
        /// </summary>
        void IGameManager.FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新所有注册的管理器
            for (int i = 0; i < _updateableManagers.Count; i++)
            {
                try
                {
                    if (_updateableManagers[i] is IFixedUpdatable fixedUpdatable)
                    {
                        fixedUpdatable.FixedUpdate(fixedDeltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 管理器固定更新失败: {_updateableManagers[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            // 固定更新所有注册的系统
            for (int i = 0; i < _updateableSystems.Count; i++)
            {
                try
                {
                    if (_updateableSystems[i] is IFixedUpdatable fixedUpdatable)
                    {
                        fixedUpdatable.FixedUpdate(fixedDeltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 系统固定更新失败: {_updateableSystems[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 后更新游戏
        /// </summary>
        void IGameManager.LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            // 后更新所有注册的管理器
            for (int i = 0; i < _updateableManagers.Count; i++)
            {
                try
                {
                    if (_updateableManagers[i] is ILateUpdatable lateUpdatable)
                    {
                        lateUpdatable.LateUpdate(deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 管理器后更新失败: {_updateableManagers[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            // 后更新所有注册的系统
            for (int i = 0; i < _updateableSystems.Count; i++)
            {
                try
                {
                    if (_updateableSystems[i] is ILateUpdatable lateUpdatable)
                    {
                        lateUpdatable.LateUpdate(deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 系统后更新失败: {_updateableSystems[i].GetType().Name}, 错误: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 退出游戏");
            
            _isQuitting = true;
            OnGameQuitting?.Invoke();
            
            // 保存游戏数据
            // SaveGameData();
            
            // 清理资源
            ShutdownAllManagers();
            
            // 退出应用程序
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        #endregion
        
        #region Tick系统
        
        /// <summary>
        /// 执行Tick更新
        /// </summary>
        private void UpdateTick()
        {
            // 这里可以执行需要固定频率更新的逻辑
            // 例如：AI决策、资源生成、建筑生产等
        }
        
        #endregion
        
        #region 管理器关闭
        
        /// <summary>
        /// 关闭所有管理器
        /// </summary>
        private void ShutdownAllManagers()
        {
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 开始关闭所有管理器...");
            
            try
            {
                // 清理管理器引用
                _buildingManager = null;
                _creepManager = null;
                _unitManager = null;
                _terrainManager = null;
                _resourceManager = null;
                _spatialIndexManager = null;
                
                // 清理注册表
                _registeredManagers.Clear();
                _registeredSystems.Clear();
                _updateableManagers.Clear();
                _updateableSystems.Clear();
                
                DAHLog.Info(LogCategory.MANAGER, "[GameManager] 所有管理器关闭完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 关闭管理器失败: {ex.Message}");
            }
        }
        
        #endregion
    }
}