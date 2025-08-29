using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;

namespace DeepAbyssHive.Terrain.Interfaces
{
    /// <summary>
    /// 地形管理器接口，负责管理所有地形块
    /// </summary>
    public interface ITerrainManager : IManager
    {
        /// <summary>
        /// 获取指定世界坐标处的地形块
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块接口</returns>
        ITerrainChunk GetChunkAt(Vector3 worldPosition);
        
        /// <summary>
        /// 更新指定位置周围的地形块
        /// </summary>
        /// <param name="centerPosition">中心位置</param>
        void UpdateChunksAroundPosition(Vector3 centerPosition);
        
        /// <summary>
        /// 获取指定世界坐标处的地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        TerrainType GetTerrainTypeAt(Vector3 worldPosition);
        
        /// <summary>
        /// 获取指定世界坐标处的高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>高度值</returns>
        float GetHeightAt(Vector3 worldPosition);
        
        /// <summary>
        /// 获取指定世界坐标处的菌毯密度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="ownerId">输出参数，菌毯所有者ID</param>
        /// <returns>菌毯密度值（0-1）</returns>
        float GetCreepDensityAt(Vector3 worldPosition, out int ownerId);
        
        /// <summary>
        /// 修改指定世界坐标处的地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="modification">地形修改数据</param>
        void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification);
        
        /// <summary>
        /// 地形块大小
        /// </summary>
        int ChunkSize { get; }
        
        /// <summary>
        /// 最大LOD级别
        /// </summary>
        int MaxLODLevels { get; }
        
        /// <summary>
        /// 视距
        /// </summary>
        float ViewDistance { get; set; }
    }
}