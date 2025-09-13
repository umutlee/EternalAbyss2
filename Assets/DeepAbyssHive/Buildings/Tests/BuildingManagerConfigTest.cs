#if UNITY_EDITOR && false
using UnityEngine;
using DeepAbyssHive.Buildings.Managers;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Buildings.Tests
{
    /// <summary>
    /// BuildingManager配置系统测试
    /// </summary>
    public class BuildingManagerConfigTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runTestOnStart = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestBuildingManagerConfig();
            }
        }
        
        /// <summary>
        /// 测试BuildingManager配置加载
        /// </summary>
        [ContextMenu("测试BuildingManager配置")]
        public void TestBuildingManagerConfig()
        {
            DAHLog.Info(LogCategory.TEST, "=== BuildingManager配置系统测试开始 ===");
            
            // 1. 测试配置加载
            var configService = ServiceManager.GetService<IConfigService>();
            var config = configService?.GetConfig<BuildingConfigSO>();
            if (config != null)
            {
                DAHLog.Info(LogCategory.TEST, $"✓ 配置加载成功: {config.ConfigName}");
                DAHLog.Info(LogCategory.TEST, $"  - 建筑模板数量: {config.buildingTemplates?.Length ?? 0}");
                DAHLog.Info(LogCategory.TEST, $"  - 研究模板数量: {config.researchTemplates?.Length ?? 0}");
                DAHLog.Info(LogCategory.TEST, $"  - 最大同时建造: {config.constructionConfig.maxConcurrentBuilds}");
                DAHLog.Info(LogCategory.TEST, $"  - 更新间隔: {config.performanceConfig.updateInterval}s");
            }
            else
            {
                DAHLog.Error(LogCategory.TEST, "✗ 配置加载失败");
                return;
            }
            
            // 2. 测试BuildingManager初始化
            var buildingManager = new BuildingManager();
            try
            {
                buildingManager.Initialize();
                DAHLog.Info(LogCategory.TEST, "✓ BuildingManager初始化成功");
                
                // 3. 测试清理
                buildingManager.Cleanup();
                DAHLog.Info(LogCategory.TEST, "✓ BuildingManager清理成功");
            }
            catch (System.Exception e)
            {
                DAHLog.Error(LogCategory.TEST, $"✗ BuildingManager测试失败: {e.Message}");
            }
            
            DAHLog.Info(LogCategory.TEST, "=== BuildingManager配置系统测试完成 ===");
        }
    }
}
#endif
