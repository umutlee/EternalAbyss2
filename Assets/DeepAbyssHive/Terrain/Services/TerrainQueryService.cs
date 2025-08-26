using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形查询服务实现
    /// 负责地形数据查询、路径查找和地形分析
    /// </summary>
    public class TerrainQueryService : ITerrainQueryService
    {
        #region 属性

        public string ServiceName => "TerrainQueryService";
        public bool IsInitialized { get; private set; }
        public bool IsQueryAvailable => IsInitialized;

        // ITerrainManager 属性实现
        public int ChunkSize => _chunkSize;
        public int MaxLODLevels { get; private set; } = 4;
        public float ViewDistance { get; set; } = 100f;

        #endregion

        #region 私有字段
        private TerrainConfigSO _config;
        private Dictionary<Vector2Int, TerrainType[,]> _chunkTerrainData;
        private Dictionary<Vector2Int, ITerrainChunk> _terrainChunks;
        
        private int _chunkSize;
        private float _tileSize;
        #endregion

        #region 构造函数
        public TerrainQueryService(TerrainConfigSO config,
            Dictionary<Vector2Int, TerrainType[,]> chunkTerrainData,
            Dictionary<Vector2Int, ITerrainChunk> terrainChunks)
        {
            _config = config;
            _chunkTerrainData = chunkTerrainData;
            _terrainChunks = terrainChunks;
            
            InitializeParameters();
        }
        #endregion

        #region ITerrainQueryService 实现
       
        public DeepAbyssHive.Terrain.Data.TerrainChunk GetChunkAt(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            
            if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
            {
                // 優先以介面處理，必要時安全 cast
                if (chunk is DeepAbyssHive.Terrain.Data.TerrainChunk terrainChunk)
                {
                    return terrainChunk;
                }
                
                // 如果不是具體類型，創建一個包裝或返回默認值
                // 這裡可以根據需要實現介面到具體類型的轉換邏輯
            }
            
            return default;
        }

        public TerrainType GetTerrainTypeAt(Vector3 worldPosition)
        {
            return GetTerrainType(worldPosition);
        }

        public float GetHeightAt(Vector3 worldPosition)
        {
            return GetTerrainHeight(worldPosition);
        }

        public float GetCreepDensityAt(Vector3 worldPosition, out int ownerId)
        {
            ownerId = 0; // 默认所有者ID
            
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                return _terrainChunks[chunkCoord].GetCreepDensity(localCoord);
            }
            
            return 0f;
        }

        public TerrainType GetTerrainType(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            // 检查区块是否已加载
            if (!_chunkTerrainData.ContainsKey(chunkCoord))
            {
                // 如果区块未加载，返回默认地形类型
                Debug.LogWarning($"[{ServiceName}] 尝试获取未加载区块的地形类型: {chunkCoord}");
                return TerrainType.Normal;
            }
            
            // 检查本地坐标是否在有效范围内
            if (localCoord.x < 0 || localCoord.x >= _chunkSize || localCoord.y < 0 || localCoord.y >= _chunkSize)
            {
                Debug.LogWarning($"[{ServiceName}] 本地坐标超出范围: {localCoord}");
                return TerrainType.Normal;
            }
            
            return _chunkTerrainData[chunkCoord][localCoord.x, localCoord.y];
        }

        public float GetTerrainHeight(Vector3 worldPosition)
        {
            TerrainType terrainType = GetTerrainType(worldPosition);
            
            // 根据地形类型返回高度
            switch (terrainType)
            {
                case TerrainType.Water:
                    return 0.0f;
                case TerrainType.Sand:
                    return 0.5f;
                case TerrainType.Normal:
                    return 1.0f;
                case TerrainType.Rock:
                    return 5.0f;
                default:
                    return 0.0f;
            }
        }

        public Vector3 GetTerrainNormal(Vector3 worldPosition)
        {
            // 采样周围点的高度来计算法线
            float spacing = _tileSize;
            float heightCenter = GetTerrainHeight(worldPosition);
            float heightRight = GetTerrainHeight(worldPosition + new Vector3(spacing, 0, 0));
            float heightForward = GetTerrainHeight(worldPosition + new Vector3(0, 0, spacing));
            
            // 计算法线
            Vector3 tangentRight = new Vector3(spacing, heightRight - heightCenter, 0).normalized;
            Vector3 tangentForward = new Vector3(0, heightForward - heightCenter, spacing).normalized;
            
            return Vector3.Cross(tangentRight, tangentForward).normalized;
        }

        public bool IsPassable(Vector3 worldPosition)
        {
            TerrainType terrainType = GetTerrainType(worldPosition);
            
            // 根据地形类型判断是否可通行
            switch (terrainType)
            {
                case TerrainType.Water:
                    return false; // 水域不可通行
                case TerrainType.Rock:
                    return false; // 岩石不可通行
                case TerrainType.Lava:
                    return false; // 岩浆不可通行
                default:
                    return true; // 其他地形可通行
            }
        }

        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            // 简单实现，实际项目中可能需要使用A*等寻路算法
            List<Vector3> path = new List<Vector3>();
            
            // 检查起点和终点是否可通行
            if (!IsPassable(start) || !IsPassable(end))
            {
                Debug.LogWarning($"[{ServiceName}] 起點或終點不可通行");
                return path;
            }
            
            // 添加起点
            path.Add(start);
            
            // 简单的直线路径（实际应该使用寻路算法）
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            float stepSize = _tileSize;
            
            for (float d = stepSize; d < distance; d += stepSize)
            {
                Vector3 waypoint = start + direction * d;
                if (IsPassable(waypoint))
                {
                    path.Add(waypoint);
                }
                else
                {
                    // 如果遇到不可通行的地形，停止路径查找
                    Debug.LogWarning($"[{ServiceName}] 路径被阻挡在 {waypoint}");
                    break;
                }
            }
            
            // 添加终点
            path.Add(end);
            
            return path;
        }

        public Dictionary<TerrainType, int> GetTerrainDistribution(Vector3 center, float radius)
        {
            Dictionary<TerrainType, int> distribution = new Dictionary<TerrainType, int>();
            
            // 初始化字典
            foreach (TerrainType type in Enum.GetValues(typeof(TerrainType)))
            {
                distribution[type] = 0;
            }
            
            // 计算采样范围
            Vector2Int centerChunkCoord = WorldToChunkCoord(center);
            int chunkRadius = Mathf.CeilToInt(radius / (_chunkSize * _tileSize));
            float radiusSquared = radius * radius;
            
            // 遍历范围内的区块
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果区块已加载，则统计地形分布
                    if (_chunkTerrainData.ContainsKey(chunkCoord))
                    {
                        TerrainType[,] terrainData = _chunkTerrainData[chunkCoord];
                        Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
                        
                        for (int x = 0; x < _chunkSize; x++)
                        {
                            for (int y = 0; y < _chunkSize; y++)
                            {
                                // 计算当前瓦片的世界坐标
                                Vector3 tileWorldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                                
                                // 计算与中心点的距离
                                float distanceSquared = Vector3.SqrMagnitude(new Vector3(tileWorldPos.x, 0, tileWorldPos.z) - 
                                                                           new Vector3(center.x, 0, center.z));
                                
                                // 如果在半径内，则统计
                                if (distanceSquared <= radiusSquared)
                                {
                                    TerrainType type = terrainData[x, y];
                                    distribution[type]++;
                                }
                            }
                        }
                    }
                }
            }
            
            return distribution;
        }

        public float GetAverageHeight(Vector3 center, float radius)
        {
            float totalHeight = 0f;
            int count = 0;
            
            // 计算采样范围
            Vector2Int centerChunkCoord = WorldToChunkCoord(center);
            int chunkRadius = Mathf.CeilToInt(radius / (_chunkSize * _tileSize));
            float radiusSquared = radius * radius;
            
            // 遍历范围内的区块
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果区块已加载，则计算高度
                    if (_chunkTerrainData.ContainsKey(chunkCoord))
                    {
                        Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
                        
                        for (int x = 0; x < _chunkSize; x += 4) // 采样间隔为4，提高性能
                        {
                            for (int y = 0; y < _chunkSize; y += 4)
                            {
                                // 计算当前瓦片的世界坐标
                                Vector3 tileWorldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                                
                                // 计算与中心点的距离
                                float distanceSquared = Vector3.SqrMagnitude(new Vector3(tileWorldPos.x, 0, tileWorldPos.z) - 
                                                                           new Vector3(center.x, 0, center.z));
                                
                                // 如果在半径内，则计算高度
                                if (distanceSquared <= radiusSquared)
                                {
                                    totalHeight += GetTerrainHeight(tileWorldPos);
                                    count++;
                                }
                            }
                        }
                    }
                }
            }
            
            if (count == 0)
                return 0f;
                
            return totalHeight / count;
        }

        public bool IsAreaPassable(Vector3 center, float radius)
        {
            // 计算采样范围
            Vector2Int centerChunkCoord = WorldToChunkCoord(center);
            int chunkRadius = Mathf.CeilToInt(radius / (_chunkSize * _tileSize));
            float radiusSquared = radius * radius;
            
            // 遍历范围内的区块
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果区块已加载，则检查可通行性
                    if (_chunkTerrainData.ContainsKey(chunkCoord))
                    {
                        Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
                        
                        for (int x = 0; x < _chunkSize; x += 2) // 采样间隔为2
                        {
                            for (int y = 0; y < _chunkSize; y += 2)
                            {
                                // 计算当前瓦片的世界坐标
                                Vector3 tileWorldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                                
                                // 计算与中心点的距离
                                float distanceSquared = Vector3.SqrMagnitude(new Vector3(tileWorldPos.x, 0, tileWorldPos.z) - 
                                                                           new Vector3(center.x, 0, center.z));
                                
                                // 如果在半径内且不可通行，返回false
                                if (distanceSquared <= radiusSquared && !IsPassable(tileWorldPos))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            
            return true;
        }

        public List<Vector3> GetPassablePositionsInArea(Vector3 center, float radius, int maxCount = 100)
        {
            List<Vector3> passablePositions = new List<Vector3>();
            
            // 计算采样范围
            Vector2Int centerChunkCoord = WorldToChunkCoord(center);
            int chunkRadius = Mathf.CeilToInt(radius / (_chunkSize * _tileSize));
            float radiusSquared = radius * radius;
            
            // 遍历范围内的区块
            for (int cx = -chunkRadius; cx <= chunkRadius && passablePositions.Count < maxCount; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius && passablePositions.Count < maxCount; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果区块已加载，则查找可通行位置
                    if (_chunkTerrainData.ContainsKey(chunkCoord))
                    {
                        Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
                        
                        for (int x = 0; x < _chunkSize && passablePositions.Count < maxCount; x += 2)
                        {
                            for (int y = 0; y < _chunkSize && passablePositions.Count < maxCount; y += 2)
                            {
                                // 计算当前瓦片的世界坐标
                                Vector3 tileWorldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                                
                                // 计算与中心点的距离
                                float distanceSquared = Vector3.SqrMagnitude(new Vector3(tileWorldPos.x, 0, tileWorldPos.z) - 
                                                                           new Vector3(center.x, 0, center.z));
                                
                                // 如果在半径内且可通行，添加到列表
                                if (distanceSquared <= radiusSquared && IsPassable(tileWorldPos))
                                {
                                    passablePositions.Add(tileWorldPos);
                                }
                            }
                        }
                    }
                }
            }
            
            return passablePositions;
        }

        // 新增的介面方法實現
        public DeepAbyssHive.Terrain.Data.TerrainChunk GetChunk(int chunkX, int chunkZ)
        {
            Vector2Int chunkCoord = new Vector2Int(chunkX, chunkZ);
            if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk) && chunk is ITerrainChunk terrainChunk)
            {
                return terrainChunk;
            }
            return default;
        }

        public float GetHeight(Vector3 position)
        {
            return GetTerrainHeight(position);
        }

        public float GetMovementSpeedModifier(Vector3 position)
        {
            TerrainType terrainType = GetTerrainType(position);
            
            // 根據地形類型返回移動速度修正
            switch (terrainType)
            {
                case TerrainType.Water:
                    return 0.0f; // 水域無法移動
                case TerrainType.Sand:
                    return 0.8f; // 沙地減速
                case TerrainType.Normal:
                    return 1.0f; // 普通地形正常速度
                case TerrainType.Rock:
                    return 0.0f; // 岩石無法移動
                case TerrainType.Lava:
                    return 0.0f; // 岩浆無法移動
                default:
                    return 1.0f;
            }
        }

        public NativeArray<DeepAbyssHive.Terrain.Data.TerrainChunk> GetChunksInRange(Vector3 center, float radius)
        {
            List<DeepAbyssHive.Terrain.Data.TerrainChunk> chunks = new List<DeepAbyssHive.Terrain.Data.TerrainChunk>();
            
            Vector2Int centerChunkCoord = WorldToChunkCoord(center);
            int chunkRadius = Mathf.CeilToInt(radius / (_chunkSize * _tileSize));
            
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
                    {
                        if (chunk is DeepAbyssHive.Terrain.Data.TerrainChunk terrainChunk)
                        {
                            chunks.Add(terrainChunk);
                        }
                    }
                }
            }
            
            var nativeArray = new NativeArray<DeepAbyssHive.Terrain.Data.TerrainChunk>(chunks.Count, Allocator.Temp);
            for (int i = 0; i < chunks.Count; i++)
            {
                nativeArray[i] = chunks[i];
            }
            
            return nativeArray;
        }

        public bool IsAreaFlat(Vector3 center, Vector2 size, float maxHeightDifference = 1f)
        {
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            
            Vector3 halfSize = new Vector3(size.x / 2f, 0, size.y / 2f);
            Vector3 min = center - halfSize;
            Vector3 max = center + halfSize;
            
            // 採樣區域內的高度
            for (float x = min.x; x <= max.x; x += _tileSize)
            {
                for (float z = min.z; z <= max.z; z += _tileSize)
                {
                    Vector3 samplePos = new Vector3(x, 0, z);
                    float height = GetHeight(samplePos);
                    
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                }
            }
            
            return (maxHeight - minHeight) <= maxHeightDifference;
        }

        public Vector3 GetNearestTerrainOfType(Vector3 position, TerrainType terrainType, float maxDistance = 100f)
        {
            float searchRadius = _tileSize;
            
            while (searchRadius <= maxDistance)
            {
                // 螺旋搜索
                for (float angle = 0; angle < 360f; angle += 45f)
                {
                    float radians = angle * Mathf.Deg2Rad;
                    Vector3 searchPos = position + new Vector3(
                        Mathf.Cos(radians) * searchRadius,
                        0,
                        Mathf.Sin(radians) * searchRadius
                    );
                    
                    if (GetTerrainType(searchPos) == terrainType)
                    {
                        return searchPos;
                    }
                }
                
                searchRadius += _tileSize * 2f;
            }
            
            return Vector3.zero; // 未找到
        }

        public bool IsWithinBounds(Vector3 position)
        {
            // 簡化實現：檢查是否有對應的地形塊
            Vector2Int chunkCoord = WorldToChunkCoord(position);
            return _chunkTerrainData.ContainsKey(chunkCoord);
        }

        public Bounds GetTerrainBounds()
        {
            if (_chunkTerrainData.Count == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
            
            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
            
            foreach (var chunkCoord in _chunkTerrainData.Keys)
            {
                min.x = Mathf.Min(min.x, chunkCoord.x);
                min.y = Mathf.Min(min.y, chunkCoord.y);
                max.x = Mathf.Max(max.x, chunkCoord.x);
                max.y = Mathf.Max(max.y, chunkCoord.y);
            }
            
            float chunkWorldSize = _chunkSize * _tileSize;
            Vector3 minWorld = new Vector3(min.x * chunkWorldSize, 0, min.y * chunkWorldSize);
            Vector3 maxWorld = new Vector3((max.x + 1) * chunkWorldSize, 0, (max.y + 1) * chunkWorldSize);
            
            Vector3 center = (minWorld + maxWorld) / 2f;
            Vector3 size = maxWorld - minWorld;
            
            return new Bounds(center, size);
        }

        public Vector3 GetNormal(Vector3 position)
        {
            return GetTerrainNormal(position);
        }

        public float GetSlope(Vector3 position)
        {
            Vector3 normal = GetNormal(position);
            float angle = Vector3.Angle(normal, Vector3.up);
            return angle;
        }

        public bool IsPathClear(Vector3 start, Vector3 end, float unitRadius = 0.5f)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            float stepSize = _tileSize / 2f; // 更細的採樣
            
            for (float d = 0; d <= distance; d += stepSize)
            {
                Vector3 checkPos = start + direction * d;
                
                // 檢查中心點
                if (!IsPassable(checkPos))
                {
                    return false;
                }
                
                // 檢查單位半徑範圍內的點
                if (unitRadius > 0)
                {
                    Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                    
                    if (!IsPassable(checkPos + perpendicular * unitRadius) ||
                        !IsPassable(checkPos - perpendicular * unitRadius))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// 修改指定世界坐标处的地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="modification">地形修改数据</param>
        public void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            // 检查区块是否已加载
            if (!_chunkTerrainData.ContainsKey(chunkCoord))
            {
                Debug.LogWarning($"[{ServiceName}] 尝试修改未加载区块的地形: {chunkCoord}");
                return;
            }
            
            // 检查本地坐标是否在有效范围内
            if (localCoord.x < 0 || localCoord.x >= _chunkSize || localCoord.y < 0 || localCoord.y >= _chunkSize)
            {
                Debug.LogWarning($"[{ServiceName}] 本地坐标超出范围: {localCoord}");
                return;
            }
            
            // 应用地形修改
            if (modification.changeTerrainType)
            {
                _chunkTerrainData[chunkCoord][localCoord.x, localCoord.y] = modification.newTerrainType;
            }
            
            // 通知地形块更新
            if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
            {
                chunk.UpdateTerrainData(_chunkTerrainData[chunkCoord]);
            }
        }

        /// <summary>
        /// 更新指定位置周围的地形块
        /// </summary>
        /// <param name="centerPosition">中心位置</param>
        public void UpdateChunksAroundPosition(Vector3 centerPosition)
        {
            Vector2Int centerChunkCoord = WorldToChunkCoord(centerPosition);
            int updateRadius = Mathf.CeilToInt(ViewDistance / (_chunkSize * _tileSize));
            
            for (int cx = -updateRadius; cx <= updateRadius; cx++)
            {
                for (int cy = -updateRadius; cy <= updateRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果地形块已加载，确保其处于活跃状态
                    if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk chunk))
                    {
                        // 这里可以添加LOD更新逻辑
                        float distance = Vector3.Distance(centerPosition, ChunkToWorldPosition(chunkCoord));
                        int lodLevel = Mathf.Clamp(Mathf.FloorToInt(distance / (ViewDistance / MaxLODLevels)), 0, MaxLODLevels - 1);
                        
                        // 更新LOD级别（如果地形块支持）
                        if (chunk is ITerrainChunk terrainChunk)
                        {
                            // terrainChunk.SetLODLevel(lodLevel);
                        }
                    }
                }
            }
        }
        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;

            InitializeParameters();
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            _chunkTerrainData?.Clear();
            _terrainChunks?.Clear();
            IsInitialized = false;
        }

        #endregion

        #region 私有方法
        private void InitializeParameters()
        {
            if (_config != null)
            {
                _chunkSize = _config.chunkSize;
                _tileSize = _config.tileSize;
            }
            else
            {
                // 默认值
                _chunkSize = 64;
                _tileSize = 1f;
            }
        }

        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);
            return new Vector2Int(chunkX, chunkZ);
        }

        private Vector2Int WorldToLocalCoord(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            float chunkWorldSize = _chunkSize * _tileSize;
            
            float localX = worldPosition.x - (chunkCoord.x * chunkWorldSize);
            float localZ = worldPosition.z - (chunkCoord.y * chunkWorldSize);
            
            int tileX = Mathf.FloorToInt(localX / _tileSize);
            int tileZ = Mathf.FloorToInt(localZ / _tileSize);
            
            return new Vector2Int(tileX, tileZ);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0, worldZ);
        }
        #endregion
    }

}
