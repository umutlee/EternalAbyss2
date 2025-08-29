using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Interfaces;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，负责管理分块地形系统 - 查询部分
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形查询
        /// <summary>
        /// 获取指定世界坐标处的地形块
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块接口</returns>
        public ITerrainChunk GetChunkAt(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                return _terrainChunks[chunkCoord];
            }
            
            return null;
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
            ownerId = 0; // 默认所有者ID
            
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                return _terrainChunks[chunkCoord].GetCreepDensity(localCoord);
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 获取地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        public TerrainType GetTerrainType(Vector3 worldPosition)
        {
            Vector2Int chunkCoord = WorldToChunkCoord(worldPosition);
            Vector2Int localCoord = WorldToLocalCoord(worldPosition);
            
            // 检查区块是否已加载
            if (!_chunkTerrainData.ContainsKey(chunkCoord))
            {
                // 如果区块未加载，返回默认地形类型
                Debug.LogWarning($"[{_managerName}] 尝试获取未加载区块的地形类型: {chunkCoord}");
                return TerrainType.Normal;
            }
            
            // 检查本地坐标是否在有效范围内
            if (localCoord.x < 0 || localCoord.x >= _chunkSize || localCoord.y < 0 || localCoord.y >= _chunkSize)
            {
                Debug.LogWarning($"[{_managerName}] 本地坐标超出范围: {localCoord}");
                return TerrainType.Normal;
            }
            
            return _chunkTerrainData[chunkCoord][localCoord.x, localCoord.y];
        }

        /// <summary>
        /// 获取地形高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形高度</returns>
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

        /// <summary>
        /// 获取地形法线
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形法线</returns>
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

        /// <summary>
        /// 检查位置是否可通行
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>是否可通行</returns>
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
                case TerrainType.Void:
                    return false; // 虚空不可通行
                default:
                    return true; // 其他地形可通行
            }
        }

        /// <summary>
        /// 查找从起点到终点的路径
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <returns>路径点列表</returns>
        public List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            // 简单实现，实际项目中可能需要使用A*等寻路算法
            List<Vector3> path = new List<Vector3>();
            
            // 检查起点和终点是否可通行
            if (!IsPassable(start) || !IsPassable(end))
            {
                Debug.LogWarning($"[{_managerName}] 起点或终点不可通行");
                return path;
            }
            
            // 添加起点
            path.Add(start);
            
            // 添加终点
            path.Add(end);
            
            return path;
        }

        /// <summary>
        /// 获取区域内的地形类型分布
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>地形类型分布字典</returns>
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

        /// <summary>
        /// 获取区域内的平均高度
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>平均高度</returns>
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
        #endregion
    }
}