using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Interfaces;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器 - 查询部分（委托模式）
    /// 所有方法委托给 TerrainQueryService
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形查询 - 委托给服务
        /// <summary>
        /// 获取指定世界坐标处的地形块
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块接口</returns>
        public ITerrainChunk GetChunkAt(Vector3 worldPosition)
        {
            return _queryService?.GetChunkAt(worldPosition);
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
            return _queryService?.GetTerrainTypeAt(worldPosition) ?? TerrainType.Unknown;
        }
        
        /// <summary>
        /// 获取指定世界坐标处的高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>高度值</returns>
        public float GetHeightAt(Vector3 worldPosition)
        {
            return _queryService?.GetHeightAt(worldPosition) ?? 0f;
        }
        
        /// <summary>
        /// 获取指定世界坐标处的菌毯密度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="ownerId">输出参数，菌毯所有者ID</param>
        /// <returns>菌毯密度值（0-1）</returns>
        public float GetCreepDensityAt(Vector3 worldPosition, out int ownerId)
        {
            if (_queryService != null)
            {
                return _queryService.GetCreepDensityAt(worldPosition, out ownerId);
            }
            
            ownerId = 0;
            return 0f;
        }
        
        #endregion
    }
}