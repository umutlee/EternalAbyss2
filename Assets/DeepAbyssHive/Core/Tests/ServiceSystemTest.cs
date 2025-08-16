using UnityEngine;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Core.Tests
{
    /// <summary>
    /// 服务系统测试
    /// </summary>
    public class ServiceSystemTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runTestOnStart = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestServiceSystem();
            }
        }
        
        /// <summary>
        /// 测试服务系统
        /// </summary>
        [ContextMenu("测试服务系统")]
        public void TestServiceSystem()
        {
            Debug.Log("=== 服务系统测试开始 ===");
            
            // 1. 测试ServiceManager单例
            var serviceManager = ServiceManager.Instance;
            if (serviceManager != null)
            {
                Debug.Log("✓ ServiceManager单例创建成功");
            }
            else
            {
                Debug.LogError("✗ ServiceManager单例创建失败");
                return;
            }
            
            // 2. 测试服务注册器
            var registrar = FindObjectOfType<ServiceRegistrar>();
            if (registrar != null)
            {
                Debug.Log("✓ ServiceRegistrar找到");
                registrar.RegisterAllServices();
            }
            else
            {
                Debug.LogWarning("⚠ ServiceRegistrar未找到，手动创建");
                var go = new GameObject("ServiceRegistrar");
                registrar = go.AddComponent<ServiceRegistrar>();
                registrar.RegisterAllServices();
            }
            
            // 3. 测试服务初始化
            try
            {
                serviceManager.InitializeAllServices();
                Debug.Log("✓ 服务初始化成功");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 服务初始化失败: {e.Message}");
            }
            
            // 4. 测试服务查询（目前服务还未实现，所以会返回null）
            // var unitQueryService = serviceManager.GetService<IUnitQueryService>();
            // Debug.Log($"单位查询服务: {(unitQueryService != null ? "已注册" : "未注册")}");
            
            Debug.Log("=== 服务系统测试完成 ===");
        }
        
        /// <summary>
        /// 测试服务清理
        /// </summary>
        [ContextMenu("测试服务清理")]
        public void TestServiceCleanup()
        {
            Debug.Log("=== 服务清理测试开始 ===");
            
            var serviceManager = ServiceManager.Instance;
            if (serviceManager != null)
            {
                serviceManager.CleanupAllServices();
                Debug.Log("✓ 服务清理完成");
            }
            else
            {
                Debug.LogError("✗ ServiceManager不存在");
            }
            
            Debug.Log("=== 服务清理测试完成 ===");
        }
    }
}