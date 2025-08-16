using System;

namespace DeepAbyssHive.Core.Services
{
    /// <summary>
    /// 服务基础接口
    /// 所有服务都应实现此接口
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceName { get; }
        
        /// <summary>
        /// 服务是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 初始化服务
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 清理服务资源
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// 可更新的服务接口
    /// 需要定期更新的服务实现此接口
    /// </summary>
    public interface IUpdatableService : IService
    {
        /// <summary>
        /// 更新服务
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
    }

    /// <summary>
    /// 查询服务基础接口
    /// 提供只读查询功能的服务
    /// </summary>
    public interface IQueryService : IService
    {
        /// <summary>
        /// 查询是否可用
        /// </summary>
        bool IsQueryAvailable { get; }
    }

    /// <summary>
    /// 命令服务基础接口
    /// 提供修改操作功能的服务
    /// </summary>
    public interface ICommandService : IService
    {
        /// <summary>
        /// 命令是否可用
        /// </summary>
        bool IsCommandAvailable { get; }
    }

    /// <summary>
    /// 服务优先级
    /// 用于确定服务的初始化和更新顺序
    /// </summary>
    public enum ServicePriority
    {
        Critical = 0,    // 关键服务（如配置、空间索引）
        High = 1,        // 高优先级（如地形、菌毯）
        Normal = 2,      // 普通优先级（如单位、建筑）
        Low = 3          // 低优先级（如UI、音效）
    }

    /// <summary>
    /// 服务元数据
    /// 用于服务注册和管理
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public string ServiceName { get; }
        public ServicePriority Priority { get; }
        public Type[] Dependencies { get; }

        public ServiceAttribute(string serviceName, ServicePriority priority = ServicePriority.Normal, params Type[] dependencies)
        {
            ServiceName = serviceName;
            Priority = priority;
            Dependencies = dependencies ?? new Type[0];
        }
    }
}