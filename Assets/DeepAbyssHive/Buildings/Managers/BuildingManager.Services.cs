using UnityEngine;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Buildings.Extensions;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// 建筑管理器 - 服务模块
    /// 负责管理建筑系统的各个服务组件
    /// </summary>
    public partial class BuildingManager
    {
        #region 服务引用

        // 注意：服务字段已在 BuildingManager.Core.cs 中定义
        // 这里只提供服务管理方法
        
        private bool IsInitialized => true;

        #endregion

        #region 服务初始化

        // 注意：InitializeServices() 和 CleanupServices() 方法已在 BuildingManager.Core.cs 中实现
        // 这里不再重复定义，避免编译错误

        /// <summary>
        /// 更新服务
        /// </summary>
        private void UpdateServices(float deltaTime)
        {
            if (!IsInitialized) return;
            
            _queryService?.Update(deltaTime);
            _constructionService?.Update(deltaTime);
            _researchService?.Update(deltaTime);
        }

        /// <summary>
        /// 暂停服务
        /// </summary>
        private void PauseServices()
        {
            if (!IsInitialized) return;
            
            _constructionService?.SetPaused(true);
            _researchService?.SetPaused(true);
        }

        /// <summary>
        /// 恢复服务
        /// </summary>
        private void ResumeServices()
        {
            if (!IsInitialized) return;
            
            _constructionService?.SetPaused(false);
            _researchService?.SetPaused(false);
        }

        #endregion
    }
}