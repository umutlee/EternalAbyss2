using UnityEngine;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Interfaces
{
    /// <summary>
    /// 地形块接口，定义地形块的基本功能
    /// </summary>
    public interface ITerrainChunk
    {
        /// <summary>
        /// 地形块坐标
        /// </summary>
        Vector2Int Coordinates { get; }

        /// <summary>
        /// 地形块边界
        /// </summary>
        Bounds Bounds { get; }

        /// <summary>
        /// 地形类型数据
        /// </summary>
        TerrainType[,] TerrainTypes { get; }

        /// <summary>
        /// 高度图数据
        /// </summary>
        float[,] HeightMap { get; }
        
        /// <summary>
        /// 加载地形块
        /// </summary>
        void Load();
        
        /// <summary>
        /// 卸载地形块
        /// </summary>
        void Unload();
        
        /// <summary>
        /// 地形块是否已加载
        /// </summary>
        bool IsLoaded { get; }
        
        /// <summary>
        /// 修改地形高度
        /// </summary>
        /// <param name="localPosition">本地坐标</param>
        /// <param name="height">高度值</param>
        void ModifyHeight(Vector2Int localPosition, float height);
        
        /// <summary>
        /// 设置地形类型
        /// </summary>
        /// <param name="localPosition">本地坐标</param>
        /// <param name="type">地形类型</param>
        void SetTerrainType(Vector2Int localPosition, TerrainType type);
        
        /// <summary>
        /// 设置LOD级别
        /// </summary>
        /// <param name="level">LOD级别</param>
        void SetLODLevel(int level);
        
        /// <summary>
        /// 当前LOD级别
        /// </summary>
        int CurrentLODLevel { get; }
        
        /// <summary>
        /// 获取菌毯密度
        /// </summary>
        /// <param name="localPosition">本地坐标</param>
        /// <returns>菌毯密度值（0-1）</returns>
        float GetCreepDensity(Vector2Int localPosition);
        
        /// <summary>
        /// 设置菌毯密度
        /// </summary>
        /// <param name="localPosition">本地坐标</param>
        /// <param name="density">密度值（0-1）</param>
        /// <param name="ownerId">所有者ID</param>
        void SetCreepDensity(Vector2Int localPosition, float density, int ownerId);
        
        /// <summary>
        /// 更新地形
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void UpdateTerrain(float deltaTime);
        
        /// <summary>
        /// 清理地形块资源
        /// </summary>
        void Cleanup();
    }
}