using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex.Interfaces
{
    /// <summary>
    /// 空间索引接口
    /// </summary>
    public interface ISpatialIndex
    {
        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        void Insert(object obj, Vector3 position, Vector3 size);
        
        /// <summary>
        /// 更新对象在空间索引中的位置
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="oldPosition">旧位置</param>
        /// <param name="newPosition">新位置</param>
        /// <param name="size">大小</param>
        void Update(object obj, Vector3 oldPosition, Vector3 newPosition, Vector3 size);
        
        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        void Remove(object obj, Vector3 position, Vector3 size);
        
        /// <summary>
        /// 查询指定区域内的所有对象
        /// </summary>
        /// <param name="position">查询位置</param>
        /// <param name="size">查询大小</param>
        /// <returns>区域内的对象列表</returns>
        List<SpatialNode> QueryRange(Vector3 position, Vector3 size);
        
        /// <summary>
        /// 查询指定点最近的对象
        /// </summary>
        /// <param name="position">查询位置</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="maxResults">最大结果数</param>
        /// <returns>最近的对象列表</returns>
        List<SpatialNode> QueryNearest(Vector3 position, float maxDistance, int maxResults);
        
        /// <summary>
        /// 查询与射线相交的对象
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>相交的对象列表</returns>
        List<SpatialNode> QueryRaycast(Ray ray, float maxDistance);
        
        /// <summary>
        /// 清空空间索引
        /// </summary>
        void Clear();
        
        /// <summary>
        /// 重建空间索引
        /// </summary>
        void Rebuild();
        
        /// <summary>
        /// 获取空间索引中的对象数量
        /// </summary>
        /// <returns>对象数量</returns>
        int GetCount();
        
        /// <summary>
        /// 获取空间索引的深度
        /// </summary>
        /// <returns>索引深度</returns>
        int GetDepth();
        
        /// <summary>
        /// 获取空间索引的边界
        /// </summary>
        /// <returns>边界</returns>
        Bounds GetBounds();
    }
}