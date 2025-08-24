using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 空间索引接口
    /// 用于高效的空间查询和管理
    /// </summary>
    [System.Obsolete("请使用 DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex 接口")]
    public interface ISpatialIndex
    {
        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        void Insert(object obj, Vector3 position, Vector3 bounds);
        
        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        void Remove(object obj, Vector3 position, Vector3 bounds);
        
        /// <summary>
        /// 查询指定区域内的对象
        /// </summary>
        System.Collections.Generic.List<object> Query(Vector3 center, Vector3 bounds);
        
        /// <summary>
        /// 清空空间索引
        /// </summary>
        void Clear();
    }
    
}
