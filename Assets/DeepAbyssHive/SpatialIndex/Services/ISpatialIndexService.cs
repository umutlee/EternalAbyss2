using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// <summary>
    /// 空间索引服务接口
    /// 提供高效的空间查询和管理功能
    /// </summary>
    public interface ISpatialIndexService : IUpdatableService
    {
        /// <summary>
        /// 添加对象到空间索引
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="position">位置</param>
        /// <param name="bounds">边界</param>
        /// <param name="objectType">对象类型</param>
        /// <returns>是否成功</returns>
        bool AddObject(int objectId, Vector3 position, Bounds bounds, SpatialObjectType objectType);

        /// <summary>
        /// 从空间索引移除对象
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <returns>是否成功</returns>
        bool RemoveObject(int objectId);

        /// <summary>
        /// 更新对象位置
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="newPosition">新位置</param>
        /// <param name="newBounds">新边界</param>
        /// <returns>是否成功</returns>
        bool UpdateObject(int objectId, Vector3 newPosition, Bounds? newBounds = null);

        /// <summary>
        /// 查询指定范围内的对象
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        /// <param name="objectType">对象类型过滤</param>
        /// <returns>对象ID数组（需要调用者Dispose）</returns>
        NativeArray<int> QueryRange(Vector3 center, float radius, SpatialObjectType objectType = SpatialObjectType.All);

        /// <summary>
        /// 查询指定边界内的对象
        /// </summary>
        /// <param name="bounds">边界</param>
        /// <param name="objectType">对象类型过滤</param>
        /// <returns>对象ID数组（需要调用者Dispose）</returns>
        NativeArray<int> QueryBounds(Bounds bounds, SpatialObjectType objectType = SpatialObjectType.All);

        /// <summary>
        /// 查询射线碰撞的对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="objectType">对象类型过滤</param>
        /// <returns>碰撞的对象ID列表</returns>
        List<UnityEngine.RaycastHit> QueryRaycast(Ray ray, float maxDistance = float.MaxValue, SpatialObjectType objectType = SpatialObjectType.All);

        /// <summary>
        /// 查询最近的对象
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="objectType">对象类型过滤</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的对象ID，如果没有返回-1</returns>
        int QueryNearest(Vector3 position, SpatialObjectType objectType = SpatialObjectType.All, float maxDistance = float.MaxValue);

        /// <summary>
        /// 查询K个最近的对象
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="k">对象数量</param>
        /// <param name="objectType">对象类型过滤</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的K个对象ID数组（需要调用者Dispose）</returns>
        NativeArray<int> QueryKNearest(Vector3 position, int k, SpatialObjectType objectType = SpatialObjectType.All, float maxDistance = float.MaxValue);

        /// <summary>
        /// 检查位置是否被占用
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">检查半径</param>
        /// <param name="excludeObjectId">排除的对象ID</param>
        /// <returns>是否被占用</returns>
        bool IsPositionOccupied(Vector3 position, float radius = 0.5f, int excludeObjectId = -1);

        /// <summary>
        /// 获取对象信息
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <returns>对象信息</returns>
        SpatialObjectInfo? GetObjectInfo(int objectId);

        /// <summary>
        /// 获取所有对象数量
        /// </summary>
        /// <param name="objectType">对象类型过滤</param>
        /// <returns>对象数量</returns>
        int GetObjectCount(SpatialObjectType objectType = SpatialObjectType.All);

        /// <summary>
        /// 清空空间索引
        /// </summary>
        /// <param name="objectType">要清空的对象类型</param>
        void Clear(SpatialObjectType objectType = SpatialObjectType.All);

        /// <summary>
        /// 优化空间索引
        /// </summary>
        void Optimize();

        /// <summary>
        /// 重建空间索引
        /// </summary>
        void Rebuild();

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        /// <returns>性能统计</returns>
        SpatialIndexPerformanceStats GetPerformanceStats();

        /// <summary>
        /// 设置索引参数
        /// </summary>
        /// <param name="maxDepth">最大深度</param>
        /// <param name="maxObjectsPerNode">每节点最大对象数</param>
        /// <param name="minNodeSize">最小节点大小</param>
        void SetIndexParameters(int? maxDepth = null, int? maxObjectsPerNode = null, float? minNodeSize = null);

        /// <summary>
        /// 批量添加对象
        /// </summary>
        /// <param name="objects">对象信息数组</param>
        /// <returns>成功添加的对象数量</returns>
        int AddObjectsBatch(SpatialObjectInfo[] objects);

        /// <summary>
        /// 批量移除对象
        /// </summary>
        /// <param name="objectIds">对象ID数组</param>
        /// <returns>成功移除的对象数量</returns>
        int RemoveObjectsBatch(int[] objectIds);

        /// <summary>
        /// 批量更新对象
        /// </summary>
        /// <param name="updates">更新信息数组</param>
        /// <returns>成功更新的对象数量</returns>
        int UpdateObjectsBatch(SpatialObjectUpdate[] updates);
    }

}