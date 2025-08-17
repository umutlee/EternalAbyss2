using UnityEngine;
using DeepAbyssHive.Buildings.Managers;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Core.Config;

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
            Debug.Log("=== BuildingManager配置系统测试开始 ===");
            
            // 1. 测试配置加载
            var config = ConfigManager.GetConfig<BuildingConfigSO>("BuildingConfig");
            if (config != null)
            {
                Debug.Log($"✓ 配置加载成功: {config.ConfigName}");
                Debug.Log($"  - 建筑模板数量: {config.buildingTemplates?.Length ?? 0}");
                Debug.Log($"  - 研究模板数量: {config.researchTemplates?.Length ?? 0}");
                Debug.Log($"  - 最大同时建造: {config.constructionConfig.maxConcurrentBuilds}");
                Debug.Log($"  - 更新间隔: {config.performanceConfig.updateInterval}s");
            }
            else
            {
                Debug.LogError("✗ 配置加载失败");
                return;
            }
            
            // 2. 测试BuildingManager初始化
            var buildingManager = new BuildingManager();
            try
            {
                buildingManager.Initialize();
                Debug.Log("✓ BuildingManager初始化成功");
                
                // 3. 测试清理
                buildingManager.Cleanup();
                Debug.Log("✓ BuildingManager清理成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ BuildingManager测试失败: {e.Message}");
            }
            
            Debug.Log("=== BuildingManager配置系统测试完成 ===");
        }
    }
}