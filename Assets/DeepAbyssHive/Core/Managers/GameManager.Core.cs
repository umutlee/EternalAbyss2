using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Core.Logging;
using IBuildingManager = DeepAbyssHive.Buildings.Interfaces.IBuildingManager;

namespace DeepAbyssHive.Core.Managers
{
    /// <summary>
    /// 游戏主管理器 - 核心模块
    /// 负责单例模式、游戏状态管理、生命周期控制
    /// </summary>
    public partial class GameManager : MonoBehaviour, IGameManager
    {
        #region 单例模式
        
        private static GameManager _instance;
        private static readonly object _lock = new object();
        
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<GameManager>();
                            if (_instance == null)
                            {
                                var go = new GameObject("GameManager");
                                _instance = go.AddComponent<GameManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region 游戏状态
        
        [Header("游戏状态")]
        [SerializeField] private bool _isGamePaused = false;
        [SerializeField] private float _gameSpeed = 1.0f;
        [SerializeField] private bool _isInitialized = false;
        [SerializeField] private bool _isQuitting = false;
        
        // 游戏状态事件
        public event Action OnGameInitialized;
        public event Action OnGameStarted;
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action OnGameEnded;
        public event Action OnGameQuitting;
        
        // 游戏状态属性
        public bool IsGamePaused => _isGamePaused;
        public float GameSpeed => _gameSpeed;
        public bool IsInitialized => _isInitialized;
        public bool IsQuitting => _isQuitting;
        
        #endregion
        
        #region 时间管理
        
        [Header("时间设置")]
        [SerializeField] private float _timeScale = 1.0f;
        [SerializeField] private float _maxTimeScale = 5.0f;
        [SerializeField] private float _minTimeScale = 0.1f;
        
        private float _realTimeSinceStartup;
        private float _gameTimeSinceStartup;
        private int _frameCount;
        
        public float RealTimeSinceStartup => _realTimeSinceStartup;
        public float GameTimeSinceStartup => _gameTimeSinceStartup;
        public int FrameCount => _frameCount;
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            // 确保单例
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCore();
            }
            else if (_instance != this)
            {
                DAHLog.Warning(LogCategory.MANAGER, "[GameManager] 检测到重复的GameManager实例，销毁多余实例");
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (_instance == this)
            {
                StartGame();
            }
        }
        
        private void Update()
        {
            if (_isQuitting) return;
            
            UpdateTimeTracking();
            
            if (!_isGamePaused && _isInitialized)
            {
                // 调用IGameManager接口的Update方法
                ((IGameManager)this).Update(Time.deltaTime);
            }
        }
        
        private void FixedUpdate()
        {
            if (_isQuitting) return;
            
            if (!_isGamePaused && _isInitialized)
            {
                // 调用IGameManager接口的FixedUpdate方法
                ((IGameManager)this).FixedUpdate(Time.fixedDeltaTime);
            }
        }
        
        private void LateUpdate()
        {
            if (_isQuitting) return;
            
            if (!_isGamePaused && _isInitialized)
            {
                // 调用IGameManager接口的LateUpdate方法
                ((IGameManager)this).LateUpdate();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
        
        private void OnApplicationQuit()
        {
            _isQuitting = true;
            QuitGame();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                ShutdownCore();
                _instance = null;
            }
        }
        
        #endregion
        
        #region 核心初始化
        
        /// <summary>
        /// 核心系统初始化
        /// </summary>
        private void InitializeCore()
        {
            DAHLog.Info(LogCategory.MANAGER, "[GameManager] 开始核心系统初始化...");
            
            try
            {
                // 初始化时间跟踪
                _realTimeSinceStartup = Time.realtimeSinceStartup;
                _gameTimeSinceStartup = 0f;
                _frameCount = 0;
                
                // 设置时间缩放
                Time.timeScale = _timeScale;
                
                DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 核心系统初始化完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 核心系统初始化失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新时间跟踪
        /// </summary>
        private void UpdateTimeTracking()
        {
            _realTimeSinceStartup = Time.realtimeSinceStartup;
            _gameTimeSinceStartup += Time.deltaTime;
            _frameCount++;
        }
        
        #endregion
        
        #region 游戏控制
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (!_isInitialized)
            {
                // 如果还没初始化，先初始化
                Initialize();
            }
            
            if (!_isInitialized)
            {
                DAHLog.Warning(LogCategory.MANAGER, "[GameManager] 游戏尚未初始化完成，无法开始");
                return;
            }
            
            _isGamePaused = false;
            Time.timeScale = _gameSpeed;
            
            OnGameStarted?.Invoke();
            DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 游戏开始");
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (_isGamePaused) return;
            
            _isGamePaused = true;
            Time.timeScale = 0f;
            
            OnGamePaused?.Invoke();
            DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 游戏暂停");
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!_isGamePaused) return;
            
            _isGamePaused = false;
            Time.timeScale = _gameSpeed;
            
            OnGameResumed?.Invoke();
            DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 游戏恢复");
        }
        
        /// <summary>
        /// 设置游戏速度
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            _gameSpeed = Mathf.Clamp(speed, _minTimeScale, _maxTimeScale);
            if (!_isGamePaused)
            {
                Time.timeScale = _gameSpeed;
            }
            
            DAHLog.Info(LogCategory.SYSTEM, $"[GameManager] 游戏速度设置为: {_gameSpeed}");
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            _isGamePaused = true;
            Time.timeScale = 0f;
            
            OnGameEnded?.Invoke();
            DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 游戏结束");
        }
        
        #endregion
        
        #region 核心关闭
        
        /// <summary>
        /// 关闭核心系统
        /// </summary>
        private void ShutdownCore()
        {
            DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 开始关闭核心系统...");
            
            try
            {
                // 重置时间缩放
                Time.timeScale = 1f;
                
                // 清理事件
                OnGameInitialized = null;
                OnGameStarted = null;
                OnGamePaused = null;
                OnGameResumed = null;
                OnGameEnded = null;
                OnGameQuitting = null;
                
                DAHLog.Info(LogCategory.SYSTEM, "[GameManager] 核心系统关闭完成");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[GameManager] 核心系统关闭失败: {ex.Message}");
            }
        }
        
        #endregion
    }
}