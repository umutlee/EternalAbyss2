using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，负责管理分块地形系统
    /// </summary>
    public class TerrainManager : ITerrainManager
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
        private int _chunkSize = 64; // 每个地形块的大小
        private float _tileSize = 1.0f; // 每个地形瓦片的大小
        private int _loadRadius = 3; // 加载半径（以地形块为单位）
        private Vector2Int _currentCenterChunk = Vector2Int.zero;
        
        // 地形生成参数
        private float _noiseScale = 0.1f;
        private float _heightScale = 10.0f;
        private int _seed = 12345;
        
        // 性能优化
        private int _maxModificationsPerFrame = 10;
        private float _modificationProcessTimer = 0f;
        private float _modificationProcessInterval = 0.1f; // 每0.1秒处理一批修改
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public TerrainManager()
        {
            // 初始化随机种子
            UnityEngine.Random.InitState(_seed);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="chunkSize">地形块大小</param>
        /// <param name="tileSize">瓦片大小</param>
        /// <param name="loadRadius">加载半径</param>
        public TerrainManager(int chunkSize, float tileSize, int loadRadius)
        {
            _chunkSize = chunkSize;
            _tileSize = tileSize;
            _loadRadius = loadRadius;
            UnityEngine.Random.InitState(_seed);
        }
        #endregion

        #region ITerrainManager接口实现
        /// <summary>
        /// 获取指定世界坐标处的地形块
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块接口</returns>
        public ITerrainChunk GetChunkAt(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            
            if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
            {
                return chunk;
            }
            
            // 如果地形块不存在，尝试加载
            LoadChunk(chunkCoord);
            return _terrainChunks.GetValueOrDefault(chunkCoord);
        }

        /// <summary>
        /// 更新指定位置周围的地形块
        /// </summary>
        /// <param name="centerPosition">中心位置</param>
        public void UpdateChunksAroundPosition(Vector3 centerPosition)
        {
            LoadTerrain(centerPosition);
        }

        /// <summary>
        /// 获取指定世界坐标处的地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        public TerrainType GetTerrainTypeAt(Vector3 worldPosition)
        {
            return GetTerrainType(worldPosition);
        }

        /// <summary>
        /// 获取指定世界坐标处的高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>高度值</returns>
        public float GetHeightAt(Vector3 worldPosition)
        {
            return GetTerrainHeight(worldPosition);
        }

        /// <summary>
        /// 获取指定世界坐标处的菌毯密度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="ownerId">输出参数，菌毯所有者ID</param>
        /// <returns>菌毯密度值（0-1）</returns>
        public float GetCreepDensityAt(Vector3 worldPosition, out int ownerId)
        {
            // 简化实现，实际项目中需要与CreepManager集成
            ownerId = -1;
            return 0f;
        }

        /// <summary>
        /// 修改指定世界坐标处的地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="modification">地形修改数据</param>
        public void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification)
        {
            modification.Position = worldPosition;
            ApplyTerrainModification(modification);
        }

        /// <summary>
        /// 获取指定位置的地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        public TerrainType GetTerrainType(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            if (!_chunkTerrainData.TryGetValue(chunkCoord, out TerrainType[,] chunkData))
            {
                // 如果地形块不存在，生成默认地形
                return GenerateTerrainTypeAtPosition(worldPosition);
            }
            
            // 检查坐标是否在有效范围内
            if (localCoord.x < 0 || localCoord.x >= _chunkSize || 
                localCoord.y < 0 || localCoord.y >= _chunkSize)
            {
                return TerrainType.Rock;
            }
            
            return chunkData[localCoord.x, localCoord.y];
        }

        /// <summary>
        /// 设置指定位置的地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="terrainType">地形类型</param>
        public void SetTerrainType(Vector3 worldPosition, TerrainType terrainType)
        {
            TerrainModification modification = new TerrainModification
            {
                Position = worldPosition,
                NewTerrainType = terrainType,
                Timestamp = Time.time,
                ModificationId = Guid.NewGuid().ToString()
            };
            
            _pendingModifications.Enqueue(modification);
            
            Debug.Log($"[{_managerName}] 添加地形修改: 位置={worldPosition}, 类型={terrainType}");
        }

        /// <summary>
        /// 获取指定区域的地形高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形高度</returns>
        public float GetTerrainHeight(Vector3 worldPosition)
        {
            TerrainType terrainType = GetTerrainType(worldPosition);
            
            // 根据地形类型返回不同的高度
            switch (terrainType)
            {
                case TerrainType.Rock:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale;
                case TerrainType.Dirt:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale * 0.8f;
                case TerrainType.Sand:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale * 0.6f;
                case TerrainType.Water:
                    return 0f;
                case TerrainType.Lava:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale * 0.4f;
                case TerrainType.Ice:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale * 1.2f;
                case TerrainType.Acid:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale * 0.3f;
                default:
                    return GenerateHeightAtPosition(worldPosition) * _heightScale;
            }
        }

        /// <summary>
        /// 检查指定位置是否可通行
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>是否可通行</returns>
        public bool IsPassable(Vector3 worldPosition)
        {
            TerrainType terrainType = GetTerrainType(worldPosition);
            
            switch (terrainType)
            {
                case TerrainType.Rock:
                    return false; // 岩石不可通行
                case TerrainType.Dirt:
                    return true;
                case TerrainType.Sand:
                    return true;
                case TerrainType.Water:
                    return false; // 水不可通行（除非是水生单位）
                case TerrainType.Lava:
                    return false; // 熔岩不可通行（除非有特殊适应性）
                case TerrainType.Ice:
                    return true; // 冰面可通行但可能有特殊效果
                case TerrainType.Acid:
                    return false; // 酸液不可通行（除非有特殊适应性）
                default:
                    return true;
            }
        }

        /// <summary>
        /// 获取指定位置的移动速度修正
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>移动速度修正（1.0为正常速度）</returns>
        public float GetMovementSpeedModifier(Vector3 worldPosition)
        {
            TerrainType terrainType = GetTerrainType(worldPosition);
            
            switch (terrainType)
            {
                case TerrainType.Rock:
                    return 0f; // 岩石无法移动
                case TerrainType.Dirt:
                    return 1.0f; // 正常速度
                case TerrainType.Sand:
                    return 0.8f; // 沙地稍慢
                case TerrainType.Water:
                    return 0f; // 水中无法移动（普通单位）
                case TerrainType.Lava:
                    return 0f; // 熔岩中无法移动
                case TerrainType.Ice:
                    return 1.2f; // 冰面稍快但可能滑倒
                case TerrainType.Acid:
                    return 0f; // 酸液中无法移动
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 加载指定中心点周围的地形块
        /// </summary>
        /// <param name="centerPosition">中心世界坐标</param>
        public void LoadTerrain(Vector3 centerPosition)
        {
            Vector2Int centerChunk = WorldToChunkCoord(centerPosition);
            
            if (centerChunk == _currentCenterChunk)
                return; // 中心块没有变化，无需重新加载
                
            _currentCenterChunk = centerChunk;
            
            // 计算需要加载的地形块范围
            HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
            for (int x = -_loadRadius; x <= _loadRadius; x++)
            {
                for (int y = -_loadRadius; y <= _loadRadius; y++)
                {
                    Vector2Int chunkCoord = centerChunk + new Vector2Int(x, y);
                    requiredChunks.Add(chunkCoord);
                }
            }
            
            // 卸载不需要的地形块
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var chunkCoord in _terrainChunks.Keys)
            {
                if (!requiredChunks.Contains(chunkCoord))
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }
            
            foreach (var chunkCoord in chunksToUnload)
            {
                UnloadChunk(chunkCoord);
            }
            
            // 加载新的地形块
            foreach (var chunkCoord in requiredChunks)
            {
                if (!_terrainChunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
            
            Debug.Log($"[{_managerName}] 地形加载完成: 中心块={centerChunk}, 加载块数={requiredChunks.Count}");
        }

        /// <summary>
        /// 应用地形修改
        /// </summary>
        /// <param name="modification">地形修改数据</param>
        public void ApplyTerrainModification(TerrainModification modification)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(modification.Position);
            Vector2Int localCoord = WorldToLocalCoord(modification.Position);

            // 確保地形塊已載入
            if (!_chunkTerrainData.ContainsKey(chunkCoord))
            {
                LoadChunk(chunkCoord);
            }

            TerrainType[,] chunkData = _chunkTerrainData[chunkCoord];

            // 範圍檢查
            if (localCoord.x < 0 || localCoord.x >= _chunkSize ||
                localCoord.y < 0 || localCoord.y >= _chunkSize)
            {
                Debug.LogWarning($"[{_managerName}] 地形修改坐標超出範圍: {localCoord}");
                return;
            }

            // 記錄原始類型
            modification.OriginalTerrainType = chunkData[localCoord.x, localCoord.y];

            // 寫入資料
            chunkData[localCoord.x, localCoord.y] = modification.NewTerrainType;

            // 更新地形塊（用介面正式簽名）
            if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
            {
                chunk.SetTerrainType(localCoord, modification.NewTerrainType);
            }

            // 紀錄歷史
            _modificationHistory.Add(modification);

            Debug.Log($"[{_managerName}] 應用地形修改: 位置={modification.Position}, {modification.OriginalTerrainType} -> {modification.NewTerrainType}");
        }

        /// <summary>
        /// 获取地形修改历史
        /// </summary>
        /// <returns>地形修改历史列表</returns>
        public NativeArray<TerrainModification> GetTerrainModificationHistory()
        {
            NativeArray<TerrainModification> history = new NativeArray<TerrainModification>(_modificationHistory.Count, Allocator.Temp);
            for (int i = 0; i < _modificationHistory.Count; i++)
            {
                history[i] = _modificationHistory[i];
            }
            return history;
        }

        /// <summary>
        /// 清除地形修改历史
        /// </summary>
        public void ClearTerrainModificationHistory()
        {
            _modificationHistory.Clear();
            Debug.Log($"[{_managerName}] 地形修改历史已清除");
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
        /// 地形块大小
        /// </summary>
        public int ChunkSize => _chunkSize;

        /// <summary>
        /// 最大LOD级别
        /// </summary>
        public int MaxLODLevels => 4; // 简化实现，返回固定值

        /// <summary>
        /// 视距
        /// </summary>
        public float ViewDistance 
        { 
            get => _loadRadius * _chunkSize * _tileSize;
            set => _loadRadius = Mathf.Max(1, Mathf.RoundToInt(value / (_chunkSize * _tileSize)));
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 初始化地形生成参数
        /// </summary>
        private void InitializeTerrainGeneration()
        {
            // 在实际实现中，这些参数可以从配置文件或编辑器中加载
            _noiseScale = 0.1f;
            _heightScale = 10.0f;
            _seed = 12345;
            
            UnityEngine.Random.InitState(_seed);
            
            Debug.Log($"[{_managerName}] 地形生成参数初始化完成: 噪声缩放={_noiseScale}, 高度缩放={_heightScale}, 种子={_seed}");
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
            
            return new SimpleTerrainChunk(chunkCoord, _chunkSize, _tileSize, terrainData, chunkObject);
        }

        /// <summary>
        /// 生成地形块的地形数据
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <returns>地形数据</returns>
        private TerrainType[,] GenerateChunkTerrain(Vector2Int chunkCoord)
        {
            TerrainType[,] terrainData = new TerrainType[_chunkSize, _chunkSize];
            
            Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
            
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int y = 0; y < _chunkSize; y++)
                {
                    Vector3 worldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                    terrainData[x, y] = GenerateTerrainTypeAtPosition(worldPos);
                }
            }
            
            return terrainData;
        }

        /// <summary>
        /// 在指定位置生成地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        private TerrainType GenerateTerrainTypeAtPosition(Vector3 worldPosition)
        {
            // 使用柏林噪声生成地形
            float noiseValue = Mathf.PerlinNoise(worldPosition.x * _noiseScale, worldPosition.z * _noiseScale);
            
            // 根据噪声值确定地形类型
            if (noiseValue < 0.2f)
            {
                return TerrainType.Water;
            }
            else if (noiseValue < 0.3f)
            {
                return TerrainType.Sand;
            }
            else if (noiseValue < 0.7f)
            {
                return TerrainType.Dirt;
            }
            else if (noiseValue < 0.9f)
            {
                return TerrainType.Rock;
            }
            else
            {
                // 特殊地形类型，根据位置和额外噪声确定
                float specialNoise = Mathf.PerlinNoise(worldPosition.x * _noiseScale * 2, worldPosition.z * _noiseScale * 2);
                if (specialNoise < 0.3f)
                {
                    return TerrainType.Lava;
                }
                else if (specialNoise < 0.6f)
                {
                    return TerrainType.Ice;
                }
                else
                {
                    return TerrainType.Acid;
                }
            }
        }

        /// <summary>
        /// 在指定位置生成地形高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形高度（0-1范围）</returns>
        private float GenerateHeightAtPosition(Vector3 worldPosition)
        {
            // 使用多层柏林噪声生成地形高度
            float height = 0f;
            float amplitude = 1f;
            float frequency = _noiseScale;
            
            // 添加多个噪声层
            for (int i = 0; i < 4; i++)
            {
                height += Mathf.PerlinNoise(worldPosition.x * frequency, worldPosition.z * frequency) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            
            return Mathf.Clamp01(height);
        }

        /// <summary>
        /// 处理待处理的地形修改
        /// </summary>
        private void ProcessPendingModifications()
        {
            int processedCount = 0;
            while (_pendingModifications.Count > 0 && processedCount < _maxModificationsPerFrame)
            {
                TerrainModification modification = _pendingModifications.Dequeue();
                ApplyTerrainModification(modification);
                processedCount++;
            }
        }

        /// <summary>
        /// 世界坐标转地形块坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块坐标</returns>
        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
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
            int localX = Mathf.FloorToInt(localPos.x / _tileSize);
            int localY = Mathf.FloorToInt(localPos.z / _tileSize);
            
            return new Vector2Int(localX, localY);
        }

        /// <summary>
        /// 地形块坐标转世界坐标
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <returns>世界坐标</returns>
        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0, worldZ);
        }
        #endregion

        #region 内部类
        /// <summary>
        /// 简单地形块实现
        /// </summary>
        private class SimpleTerrainChunk : ITerrainChunk
        {
            private Vector2Int _chunkCoord;
            private int _chunkSize;
            private float _tileSize;
            private TerrainType[,] _terrainData;
            private float[,] _heightMap;
            private float[,] _creepDensity;
            private int[,] _creepOwners;
            private GameObject _chunkObject;
            private bool _isLoaded;
            private int _currentLODLevel;
            
            public SimpleTerrainChunk(Vector2Int chunkCoord, int chunkSize, float tileSize, TerrainType[,] terrainData, GameObject chunkObject)
            {
                _chunkCoord = chunkCoord;
                _chunkSize = chunkSize;
                _tileSize = tileSize;
                _terrainData = terrainData;
                _chunkObject = chunkObject;
                _isLoaded = false;
                _currentLODLevel = 0;
                
                // 初始化高度图和菌毯数据
                _heightMap = new float[_chunkSize, _chunkSize];
                _creepDensity = new float[_chunkSize, _chunkSize];
                _creepOwners = new int[_chunkSize, _chunkSize];
                
                // 初始化数据
                for (int x = 0; x < _chunkSize; x++)
                {
                    for (int y = 0; y < _chunkSize; y++)
                    {
                        _heightMap[x, y] = 0f;
                        _creepDensity[x, y] = 0f;
                        _creepOwners[x, y] = -1;
                    }
                }
            }
            
            #region ITerrainChunk接口实现
            /// <summary>
            /// 地形块坐标
            /// </summary>
            public Vector2Int Coordinates => _chunkCoord;

            /// <summary>
            /// 地形块边界
            /// </summary>
            public Bounds Bounds
            {
                get
                {
                    Vector3 center = new Vector3(_chunkCoord.x * _chunkSize * _tileSize + (_chunkSize * _tileSize) * 0.5f, 0, 
                                                _chunkCoord.y * _chunkSize * _tileSize + (_chunkSize * _tileSize) * 0.5f);
                    Vector3 size = new Vector3(_chunkSize * _tileSize, 10f, _chunkSize * _tileSize);
                    return new Bounds(center, size);
                }
            }

            /// <summary>
            /// 地形类型数据
            /// </summary>
            public TerrainType[,] TerrainTypes => _terrainData;

            /// <summary>
            /// 高度图数据
            /// </summary>
            public float[,] HeightMap => _heightMap;

            /// <summary>
            /// 地形块是否已加载
            /// </summary>
            public bool IsLoaded => _isLoaded;

            /// <summary>
            /// 当前LOD级别
            /// </summary>
            public int CurrentLODLevel => _currentLODLevel;

            // 批次刷新相關（逐幀節流）
            private bool _dirty = false;
            private float _timeSinceDirty = 0f;
            private const float _rebuildInterval = 0.1f; // 累積到 0.1s 再重建

            /// <summary>
            /// 加载地形块
            /// </summary>
            public void Load()
            {
                if (_isLoaded) return;
                
                if (_chunkObject != null)
                {
                    _chunkObject.SetActive(true);
                }
                
                _isLoaded = true;
            }

            /// <summary>
            /// 卸载地形块
            /// </summary>
            public void Unload()
            {
                if (!_isLoaded) return;
                
                if (_chunkObject != null)
                {
                    _chunkObject.SetActive(false);
                }
                
                _isLoaded = false;
            }

            /// <summary>
            /// 修改地形高度
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            /// <param name="height">高度值</param>
            public void ModifyHeight(Vector2Int localPosition, float height)
            {
                if (localPosition.x < 0 || localPosition.x >= _chunkSize ||
                    localPosition.y < 0 || localPosition.y >= _chunkSize)
                {
                    return;
                }
                
                _heightMap[localPosition.x, localPosition.y] = height;
                
                // 更新视觉表现
                UpdateChunkVisuals(localPosition);
                _dirty = true;
            }

            /// <summary>
            /// 设置地形类型
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            /// <param name="type">地形类型</param>
            public void SetTerrainType(Vector2Int localPosition, TerrainType type)
            {
                if (localPosition.x < 0 || localPosition.x >= _chunkSize ||
                    localPosition.y < 0 || localPosition.y >= _chunkSize)
                {
                    return;
                }
                
                _terrainData[localPosition.x, localPosition.y] = type;
                
                // 更新视觉表现
                UpdateChunkVisuals(localPosition);
                _dirty = true;
            }

            /// <summary>
            /// 设置LOD级别
            /// </summary>
            /// <param name="level">LOD级别</param>
            public void SetLODLevel(int level)
            {
                _currentLODLevel = Mathf.Max(0, level);
                
                // 根据LOD级别调整渲染质量
                AdjustRenderQuality(level);
            }

            /// <summary>
            /// 获取菌毯密度
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            /// <returns>菌毯密度值（0-1）</returns>
            public float GetCreepDensity(Vector2Int localPosition)
            {
                if (localPosition.x < 0 || localPosition.x >= _chunkSize ||
                    localPosition.y < 0 || localPosition.y >= _chunkSize)
                {
                    return 0f;
                }
                
                return _creepDensity[localPosition.x, localPosition.y];
            }

            /// <summary>
            /// 设置菌毯密度
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            /// <param name="density">密度值（0-1）</param>
            /// <param name="ownerId">所有者ID</param>
            public void SetCreepDensity(Vector2Int localPosition, float density, int ownerId)
            {
                if (localPosition.x < 0 || localPosition.x >= _chunkSize ||
                    localPosition.y < 0 || localPosition.y >= _chunkSize)
                {
                    return;
                }
                
                _creepDensity[localPosition.x, localPosition.y] = Mathf.Clamp01(density);
                _creepOwners[localPosition.x, localPosition.y] = ownerId;
                
                // 更新菌毯视觉表现
                UpdateCreepVisuals(localPosition, density);
                _dirty = true;
            }
            
            /// <summary>
            /// 更新地形块视觉表现
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            private void UpdateChunkVisuals(Vector2Int localPosition)
            {
                // 实现地形修改的视觉更新逻辑
                if (_chunkObject != null)
                {
                    // 这里可以触发网格重建、材质更新等操作
                    Debug.Log($"更新地形块视觉: 坐标={localPosition}");
                }
            }
            
            /// <summary>
            /// 根据LOD级别调整渲染质量
            /// </summary>
            /// <param name="lodLevel">LOD级别</param>
            private void AdjustRenderQuality(int lodLevel)
            {
                // 实现LOD渲染质量调整逻辑
                if (_chunkObject != null)
                {
                    // 根据LOD级别调整网格细节、材质质量等
                    Debug.Log($"调整渲染质量: LOD级别={lodLevel}");
                }
            }
            
            /// <summary>
            /// 更新菌毯视觉表现
            /// </summary>
            /// <param name="localPosition">本地坐标</param>
            /// <param name="density">菌毯密度</param>
            private void UpdateCreepVisuals(Vector2Int localPosition, float density)
            {
                // 实现菌毯视觉表现更新逻辑
                if (_chunkObject != null)
                {
                    // 这里可以更新菌毯材质、透明度、动画等
                    Debug.Log($"更新菌毯视觉: 坐标={localPosition}, 密度={density}");
                }
            }
            #endregion
            
            #region 兼容性方法（保持向后兼容）
            public Vector2Int GetChunkCoordinate()
            {
                return _chunkCoord;
            }
            
            public TerrainType GetTerrainType(Vector2Int localPosition)
            {
                if (localPosition.x < 0 || localPosition.x >= _chunkSize ||
                    localPosition.y < 0 || localPosition.y >= _chunkSize)
                {
                    return TerrainType.Rock;
                }
                
                return _terrainData[localPosition.x, localPosition.y];
            }
            
            public void UpdateTerrain(Vector2Int localPosition, TerrainType newType)
            {
                SetTerrainType(localPosition, newType);
            }
            
            public void Cleanup()
            {
                Unload();
                
                if (_chunkObject != null)
                {
                    GameObject.DestroyImmediate(_chunkObject);
                }
            }
            // 介面要求：逐幀更新（RTS：做節流後的重建/同步）
            public void UpdateTerrain(float deltaTime)
            {
                if (_dirty)
                {
                    _timeSinceDirty += deltaTime;
                    if (_timeSinceDirty >= _rebuildInterval)
                    {
                        // TODO: 這裡放較重的重建邏輯（網格/碰撞/材質等）
                        if (_chunkObject != null)
                        {
                            // RebuildMesh();
                            // RebuildCollider();
                            // ApplyMaterials();
                            // Debug.Log($"[SimpleTerrainChunk] 批次刷新 chunk={_chunkCoord}, LOD={_currentLODLevel}");
                        }

                        _dirty = false;
                        _timeSinceDirty = 0f;
                    }
                }

                // 如需動態 LOD，可在這裡檢查距離/可見性後調整
                // AdjustRenderQuality(_currentLODLevel);
            }
            #endregion
        }
        #endregion
    }
}