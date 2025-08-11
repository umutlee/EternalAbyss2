namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 系统基础接口
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// 初始化系统
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 更新系统
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 清理系统
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// 暂停系统
        /// </summary>
        void Pause();
        
        /// <summary>
        /// 恢复系统
        /// </summary>
        void Resume();
        
        /// <summary>
        /// 获取系统名称
        /// </summary>
        /// <returns>系统名称</returns>
        string GetSystemName();
        
        /// <summary>
        /// 获取系统优先级
        /// </summary>
        /// <returns>系统优先级</returns>
        int GetSystemPriority();
    }
}