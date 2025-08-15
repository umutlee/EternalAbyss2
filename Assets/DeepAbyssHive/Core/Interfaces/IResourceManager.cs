using System.Collections.Generic;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 资源管理器接口
    /// </summary>
    public interface IResourceManager : IManager
    {
        /// <summary>
        /// 添加资源
        /// </summary>
        void AddResource(string resourceType, float amount);
        
        /// <summary>
        /// 检查是否有足够资源
        /// </summary>
        bool CanAfford(Dictionary<string, float> cost);
        
        /// <summary>
        /// 消费资源
        /// </summary>
        bool ConsumeResources(Dictionary<string, float> cost);
        
        /// <summary>
        /// 获取资源数量
        /// </summary>
        float GetResourceAmount(string resourceType);
    }
}