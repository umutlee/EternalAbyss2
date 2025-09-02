using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;
using DeepAbyssHive.Core.Config;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;
using TerrainTypeData = DeepAbyssHive.Terrain.Data.TerrainType;

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
        private float ConfigTileSize                 => _config != null ? (float)_config.tileSize : 1f;     // 若 SO 為 int 也能轉成 float
        private float ConfigLoadRadius               => _config != null ? _config.loadRadius : 3f;          // 半徑以世界單位/或「塊數×tileSize」為 float
        private float ConfigNoiseScale               => _config != null ? _config.noiseScale : 0.1f;
        private float ConfigHeightScale              => _config != null ? _config.heightScale : 10f;
        private int   ConfigSeed                     => _config != null ? _config.seed : 12345;
        private int   ConfigMaxModificationsPerFrame => _config != null ? _config.maxModificationsPerFrame : 10;
        #endregion

        #region 構造
        public TerrainManager()
        {
            LoadConfiguration();
            UnityEngine.Random.InitState(ConfigSeed);
        }

        /// <summary>向後相容：允許以建構子覆寫部分設定。</summary>
        public TerrainManager(int chunkSize, float tileSize, int loadRadius)
        {
            LoadConfiguration();
            if (_config != null)
            {
                _config.chunkSize = chunkSize;
                _config.tileSize  = Mathf.RoundToInt(tileSize);
                // 若 SO 的 loadRadius 是 float，這裡給 int 也會自動提升為 float
                _config.loadRadius = Mathf.Max(1, loadRadius);
            }
            UnityEngine.Random.InitState(ConfigSeed);
        }
        #endregion

        #region IManager 生命週期
        public void Initialize()
        {
            if (_isInitialized) return;

            Debug.Log($"[{_managerName}] 初始化地形管理器");

            InitializeTerrainGeneration();
            LoadTerrain(Vector3.zero);

            _isInitialized = true;
            Debug.Log($"[{_managerName}] 地形管理器初始化完成");
        }

        public void UpdateManager()
        {
            if (!_isInitialized || _isPaused) return;
            ProcessPendingModifications();
        }

        public void Cleanup()
        {
            Debug.Log($"[{_managerName}] 清理地形管理器");

            var allChunks = new List<Vector2Int>(_terrainChunks.Keys);
            foreach (var key in allChunks)
                UnloadChunk(key);

            _terrainChunks.Clear();
            _chunkTerrainData.Clear();
            _pendingModifications.Clear();
            _modificationHistory.Clear();

            _isInitialized = false;
            Debug.Log($"[{_managerName}] 地形管理器清理完成");
        }

        public void TickUpdate(float deltaTime)
        {
            UpdateManager();

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

        // ── 對外屬性（依 ITerrainManager）
        public int   ChunkSize    => ConfigChunkSize;
        public int   MaxLODLevels => 4;

        /// <summary>可視距離（世界單位）。以 float 回傳。</summary>
        public float ViewDistance
        {
            get => ConfigLoadRadius * ConfigChunkSize * ConfigTileSize;
            set
            {
                if (_config == null) return;

                // 將世界距離回寫為「半徑單位」；保持下限
                float denom = Mathf.Max(1f, ConfigChunkSize * ConfigTileSize);
                float desiredRadius = value / denom;                 // float
                _config.loadRadius = Mathf.Max(1f, desiredRadius);   // SO 欄位若為 float，直接賦值；若為 int 也會四捨五入後賦值
            }
        }
        #endregion

        #region 設定與初始化
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<TerrainConfigSO>();
            if (_config == null)
                Debug.LogWarning($"[{_managerName}] 未找到 TerrainConfig，使用預設值");
            else
                Debug.Log($"[{_managerName}] 成功載入地形配置：{_config.name}");
        }

        private void InitializeTerrainGeneration()
        {
            UnityEngine.Random.InitState(ConfigSeed);
            Debug.Log($"[{_managerName}] 生成參數：noiseScale={ConfigNoiseScale}, heightScale={ConfigHeightScale}, seed={ConfigSeed}");
        }
        #endregion

        #region 分塊載入/卸載
        private void LoadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                Debug.LogWarning($"[{_managerName}] 嘗試重覆載入地形塊：{chunkCoord}");
                return;
            }

            var terrainData = GenerateChunkTerrain(chunkCoord);
            _chunkTerrainData[chunkCoord] = terrainData;

            var chunk = CreateTerrainChunk(chunkCoord, terrainData);
            _terrainChunks[chunkCoord] = chunk;

            Debug.Log($"[{_managerName}] 載入地形塊：{chunkCoord}");
        }

        private void UnloadChunk(Vector2Int chunkCoord)
        {
            if (!_terrainChunks.TryGetValue(chunkCoord, out var chunk))
            {
                Debug.LogWarning($"[{_managerName}] 嘗試卸載不存在的地形塊：{chunkCoord}");
                return;
            }

            chunk?.Cleanup();
            _terrainChunks.Remove(chunkCoord);
            _chunkTerrainData.Remove(chunkCoord);

            Debug.Log($"[{_managerName}] 卸載地形塊：{chunkCoord}");
        }

        private ITerrainChunk CreateTerrainChunk(Vector2Int chunkCoord, TerrainType[,] terrainData)
        {
            var worldPos = ChunkToWorldPosition(chunkCoord);

            var go = new GameObject($"TerrainChunk_{chunkCoord.x}_{chunkCoord.y}");
            go.transform.position = worldPos;

            // TODO: 之後換成實際 TerrainChunk 實作
            Debug.LogWarning($"[{_managerName}] TerrainChunk 實作尚未接入，暫時回傳 null");
            return null;
        }
        #endregion

        #region 座標換算
        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            int   chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int   chunkY = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);
            return new Vector2Int(chunkX, chunkY);
        }

        private Vector2Int WorldToLocalCoord(Vector3 worldPosition)
        {
            var chunkCoord    = WorldToChunkCoord(worldPosition);
            var chunkWorldPos = ChunkToWorldPosition(chunkCoord);

            var local = worldPosition - chunkWorldPos;
            int localX = Mathf.FloorToInt(local.x / ConfigTileSize);
            int localY = Mathf.FloorToInt(local.z / ConfigTileSize);
            return new Vector2Int(localX, localY);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0f, worldZ);
        }
        #endregion
    }
}
