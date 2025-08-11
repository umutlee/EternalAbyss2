using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Core.Managers
{
    /// <summary>
    /// 游戏总管理器，负责协调所有子系统
    /// </summary>
    public class GameManager : MonoBehaviour, IGameManager
    {
        #region 私有字段
        [Header("系统配置")]
        [SerializeField] private bool _enableMultiThreading = true;
        [SerializeField] private bool _enableGPUInstancing = true;
        [SerializeField] private int _maxUnitsPerPlayer = 1000;
        [SerializeField] private float _gameSpeed = 1.0f;
        
        private Dictionary<string, IManager> _managers = new Dictionary<string, IManager>();
        private Dictionary<string, ISystem> _systems = new Dictionary<string, ISystem>();
        private List<IManager> _updateableManagers = new List<IManager>();
        private List<ISystem> _updateableSystems = new List<ISystem>();
        
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private bool _isGameRunning = false;
        private string _managerName = "GameManager";
        
        // 子系统引用
        private IUnitManager _unitManager;
        private IBuildingManager _buildingManager;
        private ITerrainManager _terrainManager;
        private ICreepManager _creepManager;
        private ISpatialIndex<object> _spatialIndex;
        
        // 游戏状态
        private float _gameTime = 0f;
        private int _currentTick = 0;
        private float _tickRate = 20f; // 每秒20次逻辑更新
        private float _tickTimer = 0f;
        
        // 性能监控
        private float _frameTime = 0f;
        private float _updateTime = 0f;
        private int _frameCount = 0;
        private float _fpsTimer = 0f;
        private float _currentFPS = 0f;
        #endregion

        #region Unity生命周期
        /// <summary>
        /// Unity Awake方法
        /// </summary>
        private void Awake()
        {
            // 确保GameManager是单例
            if (FindObjectsOfType<GameManager>().Length > 1)
            {
                Debug.LogError($"[{_managerName}] 检测到多个GameManager实例，销毁重复实例");
                Destroy(gameObject);
                return;
            }
            
            // 设置为不销毁对象
            DontDestroyOnLoad(gameObject);
            
            Debug.Log($"[{_managerName}] GameManager已创建");
        }

        /// <summary>
        /// Unity Start方法
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Unity Update方法
        /// </summary>
        private void Update()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            float deltaTime = Time.deltaTime * _gameSpeed;
            float startTime = Time.realtimeSinceStartup;
            
            // 更新游戏时间
            _gameTime += deltaTime;
            
            // 更新Tick计时器
            _tickTimer += deltaTime;
            if (_tickTimer >= 1f / _tickRate)
            {
                _tickTimer -= 1f / _tickRate;
                _currentTick++;
                
                // 执行固定频率的逻辑更新
                FixedLogicUpdate(1f / _tickRate);
            }
            
            // 更新所有管理器
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器更新异常: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
            
            // 更新所有系统
            foreach (var system in _updateableSystems)
            {
                try
                {
                    system.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 系统更新异常: {system.GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            // 计算更新时间
            _updateTime = Time.realtimeSinceStartup - startTime;
            
            // 更新性能统计
            UpdatePerformanceStats();
        }

        /// <summary>
        /// Unity FixedUpdate方法
        /// </summary>
        private void FixedUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            float fixedDeltaTime = Time.fixedDeltaTime * _gameSpeed;
            
            // 更新所有管理器的固定更新
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.FixedUpdate(fixedDeltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器固定更新异常: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unity LateUpdate方法
        /// </summary>
        public void LateUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 更新所有管理器的后更新
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.LateUpdate();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器后更新异常: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unity OnDestroy方法
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// Unity OnApplicationFocus方法
        /// </summary>
        /// <param name="hasFocus">是否有焦点</param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }
        #endregion

        #region IGameManager接口实现
        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="manager">管理器实例</param>
        public void RegisterManager(IManager manager)
        {
            if (manager == null)
            {
                Debug.LogError($"[{_managerName}] 尝试注册空的管理器");
                return;
            }
            
            string managerName = manager.GetManagerName();
            
            if (_managers.ContainsKey(managerName))
            {
                Debug.LogWarning($"[{_managerName}] 管理器已存在，将被替换: {managerName}");
                
                // 清理旧管理器
                IManager oldManager = _managers[managerName];
                _updateableManagers.Remove(oldManager);
                oldManager.Cleanup();
            }
            
            _managers[managerName] = manager;
            _updateableManagers.Add(manager);
            
            // 如果游戏已初始化，立即初始化新管理器
            if (_isInitialized)
            {
                try
                {
                    manager.Initialize();
                    Debug.Log($"[{_managerName}] 管理器注册并初始化成功: {managerName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器初始化失败: {managerName}, 错误: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[{_managerName}] 管理器注册成功: {managerName}");
            }
        }

        /// <summary>
        /// 注册系统
        /// </summary>
        /// <param name="system">系统实例</param>
        public void RegisterSystem(ISystem system)
        {
            if (system == null)
            {
                Debug.LogError($"[{_managerName}] 尝试注册空的系统");
                return;
            }
            
            string systemName = system.GetType().Name;
            
            if (_systems.ContainsKey(systemName))
            {
                Debug.LogWarning($"[{_managerName}] 系统已存在，将被替换: {systemName}");
                
                // 清理旧系统
                ISystem oldSystem = _systems[systemName];
                _updateableSystems.Remove(oldSystem);
                oldSystem.Cleanup();
            }
            
            _systems[systemName] = system;
            _updateableSystems.Add(system);
            
            // 如果游戏已初始化，立即初始化新系统
            if (_isInitialized)
            {
                try
                {
                    system.Initialize();
                    Debug.Log($"[{_managerName}] 系统注册并初始化成功: {systemName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 系统初始化失败: {systemName}, 错误: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[{_managerName}] 系统注册成功: {systemName}");
            }
        }

        /// <summary>
        /// 获取管理器
        /// </summary>
        /// <typeparam name="T">管理器类型</typeparam>
        /// <returns>管理器实例</returns>
        T IGameManager.GetManager<T>()
        {
            string typeName = typeof(T).Name;
            
            // 尝试通过接口名称查找
            if (_managers.TryGetValue(typeName, out IManager manager))
            {
                return (T)manager;
            }
            
            // 尝试通过实现类名称查找
            foreach (var pair in _managers)
            {
                if (pair.Value is T)
                {
                    return (T)pair.Value;
                }
            }
            
            Debug.LogWarning($"[{_managerName}] 未找到管理器: {typeName}");
            return default(T);
        }

        /// <summary>
        /// 获取系统
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        /// <returns>系统实例</returns>
        T IGameManager.GetSystem<T>()
        {
            string typeName = typeof(T).Name;
            
            if (_systems.TryGetValue(typeName, out ISystem system))
            {
                return (T)system;
            }
            
            // 尝试通过实现类名称查找
            foreach (var pair in _systems)
            {
                if (pair.Value is T)
                {
                    return (T)pair.Value;
                }
            }
            
            Debug.LogWarning($"[{_managerName}] 未找到系统: {typeName}");
            return default(T);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (!_isInitialized)
            {
                Debug.LogError($"[{_managerName}] 游戏未初始化，无法开始游戏");
                return;
            }
            
            if (_isGameRunning)
            {
                Debug.LogWarning($"[{_managerName}] 游戏已在运行中");
                return;
            }
            
            _isGameRunning = true;
            _isPaused = false;
            _gameTime = 0f;
            _currentTick = 0;
            _tickTimer = 0f;
            
            Debug.Log($"[{_managerName}] 游戏开始");
            
            // 通知所有管理器游戏开始
            foreach (var manager in _updateableManagers)
            {
                // 在实际实现中，可以添加OnGameStart方法到IManager接口
                Debug.Log($"[{_managerName}] 通知管理器游戏开始: {manager.GetManagerName()}");
            }
        }

        /// <summary>
        /// 停止游戏
        /// </summary>
        public void StopGame()
        {
            if (!_isGameRunning)
            {
                Debug.LogWarning($"[{_managerName}] 游戏未在运行中");
                return;
            }
            
            _isGameRunning = false;
            _isPaused = false;
            
            Debug.Log($"[{_managerName}] 游戏停止");
            
            // 通知所有管理器游戏停止
            foreach (var manager in _updateableManagers)
            {
                // 在实际实现中，可以添加OnGameStop方法到IManager接口
                Debug.Log($"[{_managerName}] 通知管理器游戏停止: {manager.GetManagerName()}");
            }
        }

        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame()
        {
            Debug.Log($"[{_managerName}] 重启游戏");
            
            StopGame();
            
            // 清理所有管理器状态
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.Cleanup();
                    manager.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器重启失败: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
            
            // 清理所有系统状态
            foreach (var system in _updateableSystems)
            {
                try
                {
                    system.Cleanup();
                    system.Initialize();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 系统重启失败: {system.GetType().Name}, 错误: {ex.Message}");
                }
            }
            
            StartGame();
        }

        /// <summary>
        /// 设置游戏速度
        /// </summary>
        /// <param name="speed">游戏速度倍率</param>
        public void SetGameSpeed(float speed)
        {
            _gameSpeed = Mathf.Clamp(speed, 0.1f, 5.0f);
            Debug.Log($"[{_managerName}] 设置游戏速度: {_gameSpeed}x");
        }

        /// <summary>
        /// 获取游戏速度
        /// </summary>
        /// <returns>游戏速度倍率</returns>
        public float GetGameSpeed()
        {
            return _gameSpeed;
        }

        /// <summary>
        /// 获取游戏时间
        /// </summary>
        /// <returns>游戏时间（秒）</returns>
        public float GetGameTime()
        {
            return _gameTime;
        }

        /// <summary>
        /// 获取当前Tick
        /// </summary>
        /// <returns>当前Tick数</returns>
        public int GetCurrentTick()
        {
            return _currentTick;
        }

        /// <summary>
        /// 是否游戏运行中
        /// </summary>
        /// <returns>是否运行中</returns>
        public bool IsGameRunning()
        {
            return _isGameRunning;
        }

        /// <summary>
        /// 注销管理器
        /// </summary>
        /// <param name="managerName">管理器名称</param>
        public void UnregisterManager(string managerName)
        {
            if (_managers.TryGetValue(managerName, out IManager manager))
            {
                _updateableManagers.Remove(manager);
                manager.Cleanup();
                _managers.Remove(managerName);
                Debug.Log($"[{_managerName}] 注销管理器: {managerName}");
            }
            else
            {
                Debug.LogWarning($"[{_managerName}] 尝试注销不存在的管理器: {managerName}");
            }
        }

        /// <summary>
        /// 获取所有管理器
        /// </summary>
        /// <returns>管理器列表</returns>
        public List<IManager> GetAllManagers()
        {
            return new List<IManager>(_managers.Values);
        }

        /// <summary>
        /// 注销系统
        /// </summary>
        /// <param name="systemName">系统名称</param>
        public void UnregisterSystem(string systemName)
        {
            if (_systems.TryGetValue(systemName, out ISystem system))
            {
                _updateableSystems.Remove(system);
                system.Cleanup();
                _systems.Remove(systemName);
                Debug.Log($"[{_managerName}] 注销系统: {systemName}");
            }
            else
            {
                Debug.LogWarning($"[{_managerName}] 尝试注销不存在的系统: {systemName}");
            }
        }

        /// <summary>
        /// 获取所有系统
        /// </summary>
        /// <returns>系统列表</returns>
        public List<ISystem> GetAllSystems()
        {
            return new List<ISystem>(_systems.Values);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (!_isGameRunning)
            {
                Debug.LogWarning($"[{_managerName}] 游戏未在运行中，无法暂停");
                return;
            }

            Pause();
            Debug.Log($"[{_managerName}] 游戏已暂停");
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!_isGameRunning)
            {
                Debug.LogWarning($"[{_managerName}] 游戏未在运行中，无法恢复");
                return;
            }

            Resume();
            Debug.Log($"[{_managerName}] 游戏已恢复");
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log($"[{_managerName}] 退出游戏");
            
            StopGame();
            Cleanup();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion

        #region IManager接口实现
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 初始化游戏管理器");
            
            try
            {
                // 初始化所有已注册的管理器
                foreach (var manager in _updateableManagers)
                {
                    try
                    {
                        manager.Initialize();
                        Debug.Log($"[{_managerName}] 管理器初始化成功: {manager.GetManagerName()}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{_managerName}] 管理器初始化失败: {manager.GetManagerName()}, 错误: {ex.Message}");
                    }
                }
                
                // 初始化所有已注册的系统
                foreach (var system in _updateableSystems)
                {
                    try
                    {
                        system.Initialize();
                        Debug.Log($"[{_managerName}] 系统初始化成功: {system.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{_managerName}] 系统初始化失败: {system.GetType().Name}, 错误: {ex.Message}");
                    }
                }
                
                _isInitialized = true;
                Debug.Log($"[{_managerName}] 游戏管理器初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{_managerName}] 游戏管理器初始化异常: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            // 在Unity的Update中已经处理了更新逻辑
            // 这里保持空实现以满足接口要求
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 在Unity的FixedUpdate中已经处理了固定更新逻辑
            // 这里保持空实现以满足接口要求
        }


        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            if (!_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 清理游戏管理器");
            
            try
            {
                // 停止游戏
                if (_isGameRunning)
                {
                    StopGame();
                }
                
                // 清理所有管理器
                foreach (var manager in _updateableManagers)
                {
                    try
                    {
                        manager.Cleanup();
                        Debug.Log($"[{_managerName}] 管理器清理成功: {manager.GetManagerName()}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{_managerName}] 管理器清理失败: {manager.GetManagerName()}, 错误: {ex.Message}");
                    }
                }
                
                // 清理所有系统
                foreach (var system in _updateableSystems)
                {
                    try
                    {
                        system.Cleanup();
                        Debug.Log($"[{_managerName}] 系统清理成功: {system.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{_managerName}] 系统清理失败: {system.GetType().Name}, 错误: {ex.Message}");
                    }
                }
                
                // 清理集合
                _managers.Clear();
                _systems.Clear();
                _updateableManagers.Clear();
                _updateableSystems.Clear();
                
                _isInitialized = false;
                _isGameRunning = false;
                _isPaused = false;
                
                Debug.Log($"[{_managerName}] 游戏管理器清理完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{_managerName}] 游戏管理器清理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            if (_isPaused)
                return;
                
            _isPaused = true;
            
            // 暂停所有管理器
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.Pause();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器暂停失败: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
            
            Debug.Log($"[{_managerName}] 游戏管理器已暂停");
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
                return;
                
            _isPaused = false;
            
            // 恢复所有管理器
            foreach (var manager in _updateableManagers)
            {
                try
                {
                    manager.Resume();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{_managerName}] 管理器恢复失败: {manager.GetManagerName()}, 错误: {ex.Message}");
                }
            }
            
            Debug.Log($"[{_managerName}] 游戏管理器已恢复");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return _managerName;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 固定频率逻辑更新
        /// </summary>
        /// <param name="tickDeltaTime">Tick时间间隔</param>
        private void FixedLogicUpdate(float tickDeltaTime)
        {
            // 在这里执行固定频率的游戏逻辑
            // 例如：AI决策、物理模拟、网络同步等
            
            // 更新单位管理器的逻辑
            if (_unitManager != null)
            {
                // 在实际实现中，可以添加FixedLogicUpdate方法到管理器接口
            }
            
            // 更新建筑管理器的逻辑
            if (_buildingManager != null)
            {
                // 在实际实现中，可以添加FixedLogicUpdate方法到管理器接口
            }
        }

        /// <summary>
        /// 更新性能统计
        /// </summary>
        private void UpdatePerformanceStats()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            _frameTime = Time.unscaledDeltaTime;
            
            if (_fpsTimer >= 1.0f)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0f;
                
                // 可以在这里输出性能统计信息
                if (_currentFPS < 30f)
                {
                    Debug.LogWarning($"[{_managerName}] 性能警告: FPS={_currentFPS:F1}, 更新时间={_updateTime * 1000f:F2}ms");
                }
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计字符串</returns>
        public string GetPerformanceStats()
        {
            return $"FPS: {_currentFPS:F1}, 帧时间: {_frameTime * 1000f:F2}ms, 更新时间: {_updateTime * 1000f:F2}ms, Tick: {_currentTick}";
        }
        #endregion

        #region 公共属性
        /// <summary>
        /// 当前FPS
        /// </summary>
        public float CurrentFPS => _currentFPS;
        
        /// <summary>
        /// 帧时间
        /// </summary>
        public float FrameTime => _frameTime;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        public float UpdateTime => _updateTime;
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool IsPaused => _isPaused;
        #endregion
    }
}