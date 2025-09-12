using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;
using DeepAbyssHive.Terrain.Chunks;
using DeepAbyssHive.Core.Logging;

using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;
using TerrainTypeData = DeepAbyssHive.Terrain.Data.TerrainType;

// Unity 2022.3.62f1 / Targets: PC/Android/iOS/MacOS
namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，負責管理分塊地形系統 - 核心部分
    /// </summary>
    public partial class TerrainManager
    {
        #region 私有字段
        private readonly Dictionary<Vector2Int, ITerrainChunk> _terrainChunks = new Dictionary<Vector2Int, ITerrainChunk>();
        private readonly Dictionary<Vector2Int, TerrainType[,]> _chunkTerrainData = new Dictionary<Vector2Int, TerrainType[,]>();
        private readonly Queue<TerrainModification> _pendingModifications = new Queue<TerrainModification>();
        private readonly List<TerrainModification> _modificationHistory = new List<TerrainModification>();

        private bool _isInitialized = false;
        private bool _isPaused = false;
        private readonly string _managerName = "TerrainManager";

        // 配置
        private TerrainConfigSO _config;
        private Vector2Int _currentCenterChunk = Vector2Int.zero;

        // 性能：每 0.1 秒處理一批修改
        private float _modificationProcessTimer = 0f;
        private float _modificationProcessInterval = 0.1f;

        // 透過屬性包一層（向後相容 & 型別一致）
        private int   ConfigChunkSize                => _config != null ? _config.chunkSize : 64;
        private float ConfigTileSize                 => _config != null ? (float)_config.tileSize : 1f;
        private float ConfigLoadRadius               => _config != null ? _config.loadRadius : 3f;
        private float ConfigNoiseScale               => _config != null ? _config.noiseScale : 0.1f;
        private float ConfigHeightScale              => _config != null ? _config.heightScale : 10f;
        private int   ConfigSeed                     => _config != null ? _config.seed : 12345;
        private int   ConfigMaxModificationsPerFrame => _config != null ? _config.maxModificationsPerFrame : 10;
        private int   ConfigMaxLODLevels            => _config != null ? _config.maxLODLevels : 1;
        private float ConfigViewDistance            => _config != null ? _config.viewDistance : 0f;
        #endregion

        #region ITerrainManager 對外屬性（M1-T01）
        public int   ChunkSize    => _config != null ? Mathf.Max(1, _config.chunkSize) : 64;
        public int   MaxLODLevels => _config != null ? _config.maxLODLevels : 1;
        public float ViewDistance
        {
            get => _config != null ? _config.viewDistance : 0f;
            set { if (_config != null) _config.viewDistance = Mathf.Max(0f, value); }
        }
        #endregion

        #region 構造
        public TerrainManager()
        {
            LoadConfiguration();
            // UnityEngine.Random.InitState 移到 Awake/Start 中呼叫
        }

        /// <summary>向後相容：允許以建構子覆寫部分設定。</summary>
        public TerrainManager(int chunkSize, float tileSize, int loadRadius)
        {
            LoadConfiguration();
            if (_config != null)
            {
                _config.chunkSize = chunkSize;
                _config.tileSize  = Mathf.RoundToInt(tileSize);
                _config.loadRadius = Mathf.Max(1, loadRadius);
            }
            // UnityEngine.Random.InitState 移到 Awake/Start 中呼叫
        }
        #endregion

        #region IManager 生命週期
        public void Initialize()
        {
            if (_isInitialized) return;

            DAHLog.Info(LogCategory.TERRAIN, $"[{_managerName}] 初始化地形管理器");

            InitializeTerrainGeneration();
            LoadTerrain(Vector3.zero);

            // 套用配置（EA-M1-T01）
            var __cfg = Resources.Load<TerrainConfigSO>("Configs/TerrainConfig");
            ApplyConfig(__cfg);

            _isInitialized = true;
            DAHLog.Info(LogCategory.TERRAIN, $"[{_managerName}] 地形管理器初始化完成");
        }

        public void UpdateManager()
        {
            if (!_isInitialized || _isPaused) return;
            ProcessPendingModifications();
        }

        public void Cleanup()
        {
            DAHLog.Info(LogCategory.TERRAIN, $"[{_managerName}] 清理地形管理器");

            var allChunks = new List<Vector2Int>(_terrainChunks.Keys);
            foreach (var key in allChunks)
                UnloadChunk(key);

            _terrainChunks.Clear();
            _chunkTerrainData.Clear();
            _pendingModifications.Clear();
            _modificationHistory.Clear();

            _isInitialized = false;
            DAHLog.Info(LogCategory.TERRAIN, $"[{_managerName}] 地形管理器清理完成");
        }

        public void TickUpdate(float deltaTime)
        {
            UpdateManager();
            TickStreaming(deltaTime);

            // 逐幀驅動 chunk（避免 null）
            foreach (var kv in _terrainChunks)
                kv.Value?.UpdateTerrain(deltaTime);
        }

        public void TickFixedUpdate(float fixedDeltaTime)
        {
            // 固定更新（如需要）
        }

        public void TickLateUpdate(float deltaTime)
        {
            // 後更新（如需要）
        }

        public void Pause()  => _isPaused = true;
        public void Resume() => _isPaused = false;

        public string GetManagerName() => _managerName;
        #endregion

        #region 配置應用
        /// <summary>
        /// 套用 TerrainConfigSO 到管理器（接線 ChunkSize/MaxLODLevels/ViewDistance），並輸出 Dev 訊息
        /// </summary>
        public void ApplyConfig(TerrainConfigSO cfg)
        {
            int   oldChunkSize  = _config != null ? _config.chunkSize  : 0;
            float oldTileSize   = _config != null ? _config.tileSize   : 0f;
            int   oldLoadRadius = _config != null ? Mathf.RoundToInt(_config.loadRadius) : 0;

            // 來源優先：參數 > Resources > 保持原值
            var byPath = Resources.Load<TerrainConfigSO>("Configs/TerrainConfig");
            _config = cfg != null ? cfg : (byPath != null ? byPath : _config);
            if (_config == null)
                _config = ScriptableObject.CreateInstance<TerrainConfigSO>();

            // Dev 訊息（驗收需要）- 每次 ApplyConfig 都顯示
            DAHLog.Dev($"[DEV HUD] Terrain: chunkSize={_config.chunkSize}, LOD={_config.maxLODLevels}, view={_config.viewDistance}");

            // 若關鍵維度變更，後續重建（佔位，不阻擋）
            bool changed = (oldChunkSize != _config.chunkSize)
                        || !Mathf.Approximately(oldTileSize, _config.tileSize)
                        || (oldLoadRadius != Mathf.RoundToInt(_config.loadRadius));
            if (changed)
                RegenerateAllChunks(); // TODO：M1-T02/M1-T03 實作
        }


        #endregion

        #region 設定與初始化
        private void LoadConfiguration()
        {
            // 若有更多初始化配置，可在此擴充
        }

        private void InitializeTerrainGeneration()
        {
            // UnityEngine.Random.InitState 已移到 Start() 中呼叫
            // 初始化噪聲、快取等其他內容...
        }
        #endregion

        #region 工具/座標換算（原有內容保持）
        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            int   chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int   chunkY = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);
            return new Vector2Int(chunkX, chunkY);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0f, worldZ);
        }
        #endregion

        #region 缺失方法實現

        /// <summary>
        /// 創建地形塊實例
        /// </summary>
        /// <param name="chunkCoord">塊座標</param>
        /// <param name="terrainData">地形數據</param>
        /// <returns>地形塊實例</returns>
        private ITerrainChunk CreateTerrainChunk(Vector2Int chunkCoord, TerrainType[,] terrainData)
        {
            var worldPos = ChunkToWorldPosition(chunkCoord);
            var go = new GameObject($"TerrainChunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.SetParent(this.transform, worldPositionStays: true);
            go.transform.position = worldPos;

            // 實際掛上 Runtime 版 Chunk（產 Mesh+Collider）
            var runtime = go.AddComponent<DeepAbyssHive.Terrain.Chunks.TerrainChunkRuntime>();
            runtime.Initialize(
                chunkCoord,
                ChunkSize,
                ConfigTileSize,
                ConfigSeed,
                ConfigNoiseScale,
                ConfigHeightScale
            );

            // 讓 chunk 吃進初始地形資料（目前以噪聲生成）
            runtime.UpdateTerrainData(terrainData);
            return runtime;
        }

        /// <summary>
        /// 世界座標轉本地座標（在所屬分塊內的座標）
        /// </summary>
        private Vector2Int WorldToLocalCoord(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
            Vector3 localPos = worldPosition - chunkWorldPos;
            
            int localX = Mathf.FloorToInt(localPos.x / ConfigTileSize);
            int localZ = Mathf.FloorToInt(localPos.z / ConfigTileSize);
            
            return new Vector2Int(localX, localZ);
        }

        #endregion
    }
}