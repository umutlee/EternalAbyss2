namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 管理器基础接口
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// 初始化管理器
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        void FixedUpdate(float fixedDeltaTime);
        
        /// <summary>
        /// 后更新管理器
        /// </summary>
        void LateUpdate();
        
        /// <summary>
        /// 清理管理器
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// 暂停管理器
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 恢复管理器
        /// </summary>
        void Resume();
        
        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        string GetManagerName();
    }
}