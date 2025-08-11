using System.Collections.Generic;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 游戏管理器接口
    /// </summary>
    public interface IGameManager
    {
        /// <summary>
        /// 初始化游戏管理器
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="manager">管理器实例</param>
        void RegisterManager(IManager manager);
        
        /// <summary>
        /// 注销管理器
        /// </summary>
        /// <param name="managerName">管理器名称</param>
        void UnregisterManager(string managerName);
        
        /// <summary>
        /// 获取管理器
        /// </summary>
        /// <typeparam name="T">管理器类型</typeparam>
        /// <returns>管理器实例</returns>
        T GetManager<T>() where T : IManager;
        
        /// <summary>
        /// 获取所有管理器
        /// </summary>
        /// <returns>管理器列表</returns>
        List<IManager> GetAllManagers();
        
        /// <summary>
        /// 注册系统
        /// </summary>
        /// <param name="system">系统实例</param>
        void RegisterSystem(ISystem system);
        
        /// <summary>
        /// 注销系统
        /// </summary>
        /// <param name="systemName">系统名称</param>
        void UnregisterSystem(string systemName);
        
        /// <summary>
        /// 获取系统
        /// </summary>
        /// <typeparam name="T">系统类型</typeparam>
        /// <returns>系统实例</returns>
        T GetSystem<T>() where T : ISystem;
        
        /// <summary>
        /// 获取所有系统
        /// </summary>
        /// <returns>系统列表</returns>
        List<ISystem> GetAllSystems();
        
        /// <summary>
        /// 更新游戏
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 固定更新游戏
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        void FixedUpdate(float fixedDeltaTime);
        
        /// <summary>
        /// 后更新游戏
        /// </summary>
        void LateUpdate();
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        void PauseGame();
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        void ResumeGame();
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        void QuitGame();
    }
}