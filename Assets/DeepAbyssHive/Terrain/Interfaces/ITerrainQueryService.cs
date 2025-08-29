using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;

namespace DeepAbyssHive.Terrain.Interfaces
{
    /// <summary>
    /// 地形查询服务接口
    /// 负责地形数据查询、路径查找和地形分析
    /// </summary>
    public interface ITerrainQueryService
    {
        /// <summary>
        /// 获取指定世界坐标处的地形块
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形块</returns>
        DeepAbyssHive.Terrain.Data.TerrainChunk GetChunkAt(Vector3 worldPosition);

        /// <summary>
        /// 获取指定坐标的地形块
        /// </summary>
        /// <param name="chunkX">X坐标</param>
        /// <param name="chunkZ">Z坐标</param>
        /// <returns>地形块</returns>
        DeepAbyssHive.Terrain.Data.TerrainChunk GetChunk(int chunkX, int chunkZ);

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
        /// 获取指定位置的地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形类型</returns>
        TerrainType GetTerrainType(Vector3 worldPosition);

        /// <summary>
        /// 获取指定位置的地形高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>高度值</returns>
        float GetTerrainHeight(Vector3 worldPosition);

        /// <summary>
        /// 获取指定位置的地形法线
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>法线向量</returns>
        Vector3 GetTerrainNormal(Vector3 worldPosition);

        /// <summary>
        /// 判断指定位置是否可通行
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>是否可通行</returns>
        bool IsPassable(Vector3 worldPosition);

        /// <summary>
        /// 寻找从起点到终点的路径
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <returns>路径点列表</returns>
        List<Vector3> FindPath(Vector3 start, Vector3 end);

        /// <summary>
        /// 获取指定范围内的地形分布
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>地形类型分布字典</returns>
        Dictionary<TerrainType, int> GetTerrainDistribution(Vector3 center, float radius);

        /// <summary>
        /// 获取指定范围内的平均高度
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>平均高度</returns>
        float GetAverageHeight(Vector3 center, float radius);

        /// <summary>
        /// 判断指定区域是否可通行
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>是否可通行</returns>
        bool IsAreaPassable(Vector3 center, float radius);

        /// <summary>
        /// 获取指定区域内的可通行位置
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="maxCount">最大数量</param>
        /// <returns>可通行位置列表</returns>
        List<Vector3> GetPassablePositionsInArea(Vector3 center, float radius, int maxCount = 100);

        /// <summary>
        /// 获取指定位置的移动速度修正
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>移动速度修正系数</returns>
        float GetMovementSpeedModifier(Vector3 position);

        /// <summary>
        /// 获取指定范围内的地形块
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>地形块数组</returns>
        NativeArray<TerrainChunk> GetChunksInRange(Vector3 center, float radius);

        /// <summary>
        /// 判断指定区域是否平坦
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="size">尺寸</param>
        /// <param name="maxHeightDifference">最大高度差</param>
        /// <returns>是否平坦</returns>
        bool IsAreaFlat(Vector3 center, Vector2 size, float maxHeightDifference = 1f);

        /// <summary>
        /// 获取最近的指定类型地形
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="terrainType">地形类型</param>
        /// <param name="maxDistance">最大搜索距离</param>
        /// <returns>最近的地形位置</returns>
        Vector3 GetNearestTerrainOfType(Vector3 position, TerrainType terrainType, float maxDistance = 100f);

        /// <summary>
        /// 判断位置是否在地形边界内
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在边界内</returns>
        bool IsWithinBounds(Vector3 position);

        /// <summary>
        /// 获取地形边界
        /// </summary>
        /// <returns>边界</returns>
        Bounds GetTerrainBounds();

        /// <summary>
        /// 获取指定位置的法线
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>法线向量</returns>
        Vector3 GetNormal(Vector3 position);

        /// <summary>
        /// 获取指定位置的坡度
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>坡度角度</returns>
        float GetSlope(Vector3 position);

        /// <summary>
        /// 判断路径是否畅通
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="unitRadius">单位半径</param>
        /// <returns>是否畅通</returns>
        bool IsPathClear(Vector3 start, Vector3 end, float unitRadius = 0.5f);
    }
}