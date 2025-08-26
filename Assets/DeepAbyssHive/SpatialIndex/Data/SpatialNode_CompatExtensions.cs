using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>
    /// SpatialNode的兼容扩展方法
    /// 提供向后兼容的构造函数和方法
    /// </summary>
    public static class SpatialNode_CompatExtensions
    {
        /// <summary>
        /// 创建SpatialNode的兼容方法
        /// </summary>
        /// <param name="id">节点ID</param>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        /// <returns>新的SpatialNode实例</returns>
        public static SpatialNode CreateNode(int id, Vector3 position, Vector3 size)
        {
            Bounds bounds = new Bounds(position, size);
            return new SpatialNode(id, null, position, bounds);
        }
    }
}