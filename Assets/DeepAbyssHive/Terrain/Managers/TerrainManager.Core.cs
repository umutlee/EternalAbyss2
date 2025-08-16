using System;
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

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，负责管理分块地形系统 - 核心部分
    /// </summary>
    public partial class TerrainManager
    {
        #region 私有字段
        private Dictionary<Vector2Int, ITerrainChunk> _terrainChunks = new Dictionary<Vector2Int, ITerrainChunk>();
        private Dictionary<Vector2Int, TerrainType[,]> _chunkTerrainData = new Dictionary<Vector2Int, TerrainType[,]>();
        private Queue<TerrainModification> _pendingModifications = new Queue<TerrainModification>();
        private List<TerrainModification> _modificationHistory = new List<TerrainModification>();
        
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private string _managerName = "TerrainManager";
        
        // 地形配置
        // 配置引用
        private TerrainConfigSO _config;
        private Vector2Int _currentCenterChunk = Vector2Int.zero;
        
        // 性能优化
        private float _modificationProcessTimer = 0f;
        private float _modificationProcessInterval = 0.1f; // 每0.1秒处理一批修改
        
        // 配置属性访问器（向后兼容）
        private int ConfigChunkSize => _config?.chunkSize ?? 64;
        private float ConfigTileSize => _config?.tileSize ?? 1.0f;
        private int ConfigLoadRadius => _config?.loadRadius ?? 3;
        private float ConfigNoiseScale => _config?.noiseScale ?? 0.1f;
        private float ConfigHeightScale => _config?.heightScale ?? 10.0f;
        private int ConfigSeed => _config?.seed ?? 12345;
        private int ConfigMaxModificationsPerFrame => _config?.maxModificationsPerFrame ?? 10;
        #endregion

        #region 构造函数
        /// <summary>
        /// <summary>
        /// 构造函数
        /// </summary>
        public TerrainManager()
        {
            // 加载配置
            LoadConfiguration();
            
            // 初始化随机种子
            UnityEngine.Random.InitState(ConfigSeed);
        }

        /// <summary>
        /// 构造函数（向后兼容）
        /// </summary>
        /// <param name="chunkSize">地形块大小</param>
        /// <param name="tileSize">瓦片大小</param>
        /// <param name="loadRadius">加载半径</param>
        public TerrainManager(int chunkSize, float tileSize, int loadRadius)
        {
            // 加载配置
            LoadConfiguration();
            
            // 如果传入了参数，覆盖配置值（向后兼容）
            if (_config != null)
            {
                _config.chunkSize = chunkSize;
                _config.tileSize = tileSize;
                _config.loadRadius = loadRadius;
            }
            
            UnityEngine.Random.InitState(ConfigSeed);
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
                
            Debug.Log($"[{_managerName}] 初始化地形管理器");
            
            // 初始化地形生成参数
            InitializeTerrainGeneration();
            
            // 加载初始地形
            LoadTerrain(Vector3.zero);
            
            _isInitialized = true;
            Debug.Log($"[{_managerName}] 地形管理器初始化完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        public void UpdateManager()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 处理待处理的地形修改
            ProcessPendingModifications();
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_managerName}] 清理地形管理器");
            
            // 卸载所有地形块
            List<Vector2Int> allChunks = new List<Vector2Int>(_terrainChunks.Keys);
            foreach (var chunkCoord in allChunks)
            {
                UnloadChunk(chunkCoord);
            }
            
            _terrainChunks.Clear();
            _chunkTerrainData.Clear();
            _pendingModifications.Clear();
            _modificationHistory.Clear();
            
            _isInitialized = false;
            
            Debug.Log($"[{_managerName}] 地形管理器清理完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            UpdateManager();

            // 每幀驅動所有已載入的 chunk 做逐幀更新（含節流的批次刷新/LOD 維護等）
            foreach (var kv in _terrainChunks)
            {
                kv.Value.UpdateTerrain(deltaTime);
            }
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新逻辑
        }

        /// <summary>
        /// 后更新管理器
        /// </summary>
        public void LateUpdate()
        {
            // 后更新逻辑
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
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
        /// <summary>
        /// 地形块大小
        /// </summary>
        public int ChunkSize => ConfigChunkSize;

        /// <summary>
        /// 最大LOD级别
        /// </summary>
        public int MaxLODLevels => 4; // 简化实现，返回固定值

        /// <summary>
        /// 视距
        /// </summary>
        public float ViewDistance 
        { 
            get => ConfigLoadRadius * ConfigChunkSize * ConfigTileSize;
            set 
            {
                if (_config != null)
                {
                    _config.loadRadius = Mathf.Max(1, Mathf.RoundToInt(value / (ConfigChunkSize * ConfigTileSize)));
                }
            }
        }
        #endregion

        /// <summary>
        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<TerrainConfigSO>("TerrainConfig");
            
            if (_config == null)
            {
                Debug.LogWarning($"[{_managerName}] 未找到TerrainConfig配置文件，使用默认值");
            }
            else
            {
                Debug.Log($"[{_managerName}] 成功加载地形配置: {_config.name}");
            }
        }

        /// <summary>
        /// 初始化地形生成参数
        /// </summary>
        private void InitializeTerrainGeneration()
        {
            // 从配置加载参数
            UnityEngine.Random.InitState(ConfigSeed);
            
            Debug.Log($"[{_managerName}] 地形生成参数初始化完成: 噪声缩放={ConfigNoiseScale}, 高度缩放={ConfigHeightScale}, 种子={ConfigSeed}");
        }

        /// <summary>
        /// 加载地形块
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        private void LoadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                Debug.LogWarning($"[{_managerName}] 尝试加载已存在的地形块: {chunkCoord}");
                return;
            }
            
            // 生成地形数据
            TerrainType[,] terrainData = GenerateChunkTerrain(chunkCoord);
            _chunkTerrainData[chunkCoord] = terrainData;
            
            // 创建地形块对象
            ITerrainChunk chunk = CreateTerrainChunk(chunkCoord, terrainData);
            _terrainChunks[chunkCoord] = chunk;
            
            Debug.Log($"[{_managerName}] 加载地形块: {chunkCoord}");
        }

        /// <summary>
        /// 卸载地形块
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        private void UnloadChunk(Vector2Int chunkCoord)
        {
            if (!_terrainChunks.ContainsKey(chunkCoord))
            {
                Debug.LogWarning($"[{_managerName}] 尝试卸载不存在的地形块: {chunkCoord}");
                return;
            }
            
            // 销毁地形块对象
            ITerrainChunk chunk = _terrainChunks[chunkCoord];
            chunk.Cleanup();
            
            _terrainChunks.Remove(chunkCoord);
            _chunkTerrainData.Remove(chunkCoord);
            
            Debug.Log($"[{_managerName}] 卸载地形块: {chunkCoord}");
        }

        /// <summary>
        /// 创建地形块
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <param name="terrainData">地形数据</param>
        /// <returns>地形块实例</returns>
        private ITerrainChunk CreateTerrainChunk(Vector2Int chunkCoord, TerrainType[,] terrainData)
        {
            Vector3 worldPosition = ChunkToWorldPosition(chunkCoord);
            
            // 创建地形块游戏对象
            GameObject chunkObject = new GameObject($"TerrainChunk_{chunkCoord.x}_{chunkCoord.y}");
            chunkObject.transform.position = worldPosition;
            
            // TODO: 需要实现TerrainChunk类或使用现有的地形块实现
            // return new TerrainChunk(chunkCoord, _chunkSize, _tileSize, terrainData, chunkObject);
            
            // 临时返回null，等待TerrainChunk类实现
            Debug.LogWarning($"[{_managerName}] TerrainChunk类型未找到，返回null");
            return null;
        }

        /// <summary>
        /// <summary>
        /// 世界坐标转地形块坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块坐标</returns>
        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int chunkY = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);
            return new Vector2Int(chunkX, chunkY);
        }

        /// <summary>
        /// 世界坐标转地形块内本地坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>本地坐标</returns>
        private Vector2Int WorldToLocalCoord(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
            
            Vector3 localPos = worldPosition - chunkWorldPos;
            int localX = Mathf.FloorToInt(localPos.x / ConfigTileSize);
            int localY = Mathf.FloorToInt(localPos.z / ConfigTileSize);
            
            return new Vector2Int(localX, localY);
        }

        /// <summary>
        /// 地形块坐标转世界坐标
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <returns>世界坐标</returns>
        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = ConfigChunkSize * ConfigTileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0, worldZ);
        }
    }
}