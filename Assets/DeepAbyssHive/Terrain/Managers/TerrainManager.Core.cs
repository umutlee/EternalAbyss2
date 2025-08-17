using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;
using DeepAbyssHive.Terrain.Services;
using DeepAbyssHive.Terrain.Interfaces;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器核心 - 服務容器和API適配器
    /// 職責：
    /// - 作為服務容器，持有 ITerrainQueryService、ITerrainModificationService 和 ITerrainGenerationService
    /// - 提供向後兼容的公共API，內部委託給服務處理
    /// - 管理MonoBehaviour生命週期和IManager接口實現
    /// </summary>
    public partial class TerrainManager : MonoBehaviour, ITerrainManager
    {
        // 服務引用
        private ITerrainQueryService _queryService;
        private ITerrainModificationService _modificationService;
        private ITerrainGenerationService _generationService;
        
        // 配置系統
        private TerrainConfigSO _config;
        
        // 數據容器（由服務共享）
        private readonly Dictionary<Vector2Int, TerrainChunk> _terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
        private readonly Queue<TerrainModification> _modificationQueue = new Queue<TerrainModification>();
        private readonly List<TerrainModification> _modificationHistory = new List<TerrainModification>();
        
        // 配置參數
        [SerializeField] private int _chunkSize = 32;
        [SerializeField] private float _heightScale = 10f;
        [SerializeField] private float _noiseScale = 0.1f;
        [SerializeField] private int _loadRadius = 3;
        [SerializeField] private float _modificationProcessInterval = 0.1f;
        
        private float _modificationTimer = 0f;
        private int _maxModificationsPerFrame = 5;
        private string _managerName = "TerrainManager";
        private bool _isPaused = false;

        /// <summary>
        /// 初始化配置系統
        /// </summary>
        private void InitializeConfig()
        {
            _config = ConfigManager.GetConfig<TerrainConfigSO>("TerrainConfig");
            
            if (_config != null)
            {
                // 從配置加載參數
                _chunkSize = _config.chunkSize;
                _heightScale = _config.heightScale;
                _noiseScale = _config.noiseScale;
                _loadRadius = _config.loadRadius;
                _modificationProcessInterval = _config.modificationProcessInterval;
                _maxModificationsPerFrame = _config.maxModificationsPerFrame;
                
                Debug.Log($"[{_managerName}] 配置加載成功: {_config.ConfigName}");
            }
            else
            {
                Debug.LogWarning($"[{_managerName}] 配置文件未找到，使用默認值");
            }
        }

        /// <summary>
        /// 初始化服務和配置
        /// </summary>
        public void Initialize()
        {
            // 1. 首先初始化配置系統
            InitializeConfig();
            
            // 2. 初始化服務
            InitializeServices();
            
            Debug.Log($"[{_managerName}] 服務化初始化完成");
        }

        /// <summary>
        /// 初始化服務實例
        /// </summary>
        private void InitializeServices()
        {
            // 創建查詢服務
            _queryService = new TerrainQueryService(
                _terrainChunks,
                _chunkSize,
                _heightScale
            );

            // 創建修改服務
            _modificationService = new TerrainModificationService(
                _terrainChunks,
                _modificationQueue,
                _modificationHistory
            );

            // 創建生成服務
            _generationService = new TerrainGenerationService(
                _chunkSize,
                _heightScale,
                _noiseScale
            );

            Debug.Log($"[{_managerName}] 服務初始化完成");
        }

        /// <summary>
        /// 清理資源和服務
        /// </summary>
        public void Cleanup()
        {
            // 清理數據
            _terrainChunks.Clear();
            _modificationQueue.Clear();
            _modificationHistory.Clear();
            
            // 清理服務引用
            _queryService = null;
            _modificationService = null;
            _generationService = null;
            
            Debug.Log($"[{_managerName}] 服務化清理完成");
        }

        /// <summary>
        /// 獲取服務實例
        /// </summary>
        /// <typeparam name="T">服務接口類型</typeparam>
        /// <returns>服務實例，如果不存在則返回null</returns>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(ITerrainQueryService))
                return _queryService as T;
            if (typeof(T) == typeof(ITerrainModificationService))
                return _modificationService as T;
            if (typeof(T) == typeof(ITerrainGenerationService))
                return _generationService as T;
            
            return null;
        }

        /// <summary>
        /// 更新管理器 - 委託給服務處理
        /// </summary>
        /// <param name="deltaTime">時間增量</param>
        public void Update(float deltaTime)
        {
            if (_isPaused) return;

            // 處理地形修改隊列
            _modificationTimer += deltaTime;
            if (_modificationTimer >= _modificationProcessInterval)
            {
                ProcessModificationQueue();
                _modificationTimer = 0f;
            }
        }

        /// <summary>
        /// 處理地形修改隊列
        /// </summary>
        private void ProcessModificationQueue()
        {
            int modificationsProcessed = 0;
            
            while (_modificationQueue.Count > 0 && modificationsProcessed < _maxModificationsPerFrame)
            {
                var modification = _modificationQueue.Dequeue();
                
                // 委託給修改服務處理
                _modificationService?.ApplyModification(modification);
                
                // 記錄到歷史
                _modificationHistory.Add(modification);
                
                modificationsProcessed++;
            }
        }

        /// <summary>
        /// 加載地形
        /// </summary>
        /// <param name="centerPosition">中心位置</param>
        public void LoadTerrain(Vector3 centerPosition)
        {
            Vector2Int centerChunk = WorldToChunkCoord(centerPosition);
            
            // 加載周圍的地形塊
            for (int x = -_loadRadius; x <= _loadRadius; x++)
            {
                for (int z = -_loadRadius; z <= _loadRadius; z++)
                {
                    Vector2Int chunkCoord = centerChunk + new Vector2Int(x, z);
                    LoadChunk(chunkCoord);
                }
            }
        }

        /// <summary>
        /// 加載地形塊
        /// </summary>
        /// <param name="chunkCoord">地形塊坐標</param>
        private void LoadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.ContainsKey(chunkCoord))
                return;

            // 委託給生成服務創建地形塊
            var terrainData = _generationService?.GenerateChunkTerrain(chunkCoord);
            if (terrainData != null)
            {
                var chunk = new TerrainChunk(chunkCoord, terrainData, _chunkSize);
                _terrainChunks[chunkCoord] = chunk;
            }
        }

        /// <summary>
        /// 卸載地形塊
        /// </summary>
        /// <param name="chunkCoord">地形塊坐標</param>
        private void UnloadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.TryGetValue(chunkCoord, out var chunk))
            {
                chunk.Dispose();
                _terrainChunks.Remove(chunkCoord);
            }
        }

        /// <summary>
        /// 世界坐標轉地形塊坐標
        /// </summary>
        /// <param name="worldPosition">世界坐標</param>
        /// <returns>地形塊坐標</returns>
        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / _chunkSize),
                Mathf.FloorToInt(worldPosition.z / _chunkSize)
            );
        }

        // IManager 接口實現
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新邏輯
        }

        public void LateUpdate(float deltaTime)
        {
            // 延遲更新邏輯
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public string GetManagerName()
        {
            return _managerName;
        }

        // Unity 生命週期
        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
