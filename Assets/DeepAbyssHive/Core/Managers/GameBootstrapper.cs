using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Creep.Services;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Units.Services;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Core.Managers
{
    /// <summary>
    /// 遊戲啟動時註冊核心服務的腳本
    /// 負責初始化和註冊所有系統服務到ServiceLocator
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("啟動配置")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool validateServicesOnStart = true;
        
        [Header("服務初始化順序")]
        [SerializeField] private bool initializeConfigFirst = true;
        [SerializeField] private bool initializeSpatialIndexFirst = true;

        private void Awake()
        {
            // 確保只有一個GameBootstrapper實例
            if (FindObjectsOfType<GameBootstrapper>().Length > 1)
            {
                Debug.LogWarning("[GameBootstrapper] 發現多個GameBootstrapper實例，銷毀重複的實例");
                Destroy(gameObject);
                return;
            }

            // 設置為不銷毀
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameBootstrapper] 開始初始化核心服務...");

            InitializeServices();
            
            if (validateServicesOnStart)
            {
                ValidateServices();
            }

            ServiceLocator.MarkAsInitialized();
            
            if (enableDebugLogging)
            {
                Debug.Log(ServiceLocator.GetStatusInfo());
            }

            Debug.Log("[GameBootstrapper] 核心服務初始化完成");
        }

        /// <summary>
        /// 初始化所有核心服務
        /// </summary>
        private void InitializeServices()
        {
            // 1. 首先初始化配置管理器（如果需要）
            if (initializeConfigFirst)
            {
                InitializeConfigManager();
            }

            // 2. 初始化空間索引服務（基礎服務）
            if (initializeSpatialIndexFirst)
            {
                InitializeSpatialIndexServices();
            }

            // 3. 初始化Creep相關服務
            InitializeCreepServices();

            // 4. 初始化建築相關服務
            InitializeBuildingServices();

            // 5. 初始化單位相關服務
            InitializeUnitServices();

            // 6. 初始化其他核心服務
            InitializeOtherServices();
        }

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        private void InitializeConfigManager()
        {
            Debug.Log("[GameBootstrapper] 初始化配置管理器...");
            
            // ConfigManager是單例，直接確保其初始化
            if (!ConfigManager.Instance.IsInitialized)
            {
                ConfigManager.Instance.Initialize();
            }
        }

        /// <summary>
        /// 初始化空間索引服務
        /// </summary>
        private void InitializeSpatialIndexServices()
        {
            Debug.Log("[GameBootstrapper] 初始化空間索引服務...");
            
            var spatialIndexService = new SpatialIndexService();
            ServiceLocator.Register<ISpatialIndexService>(spatialIndexService, "SpatialIndexService");
        }

        /// <summary>
        /// 初始化Creep相關服務
        /// </summary>
        private void InitializeCreepServices()
        {
            Debug.Log("[GameBootstrapper] 初始化Creep服務...");

            // 創建Creep服務實例
            var creepGridService = new CreepGridService();
            var creepSourceService = new CreepSourceService();
            var creepNetworkService = new CreepNetworkService();
            var creepSimulationService = new CreepSimulationService();
            
            // 創建查詢服務（可能依賴其他服務）
            var creepQueryService = new CreepQueryService();

            // 註冊服務到ServiceLocator
            ServiceLocator.Register<ICreepGridService>(creepGridService, "CreepGridService");
            ServiceLocator.Register<ICreepSourceService>(creepSourceService, "CreepSourceService");
            ServiceLocator.Register<ICreepNetworkService>(creepNetworkService, "CreepNetworkService");
            ServiceLocator.Register<ICreepSimulationService>(creepSimulationService, "CreepSimulationService");
            ServiceLocator.Register<ICreepQueryService>(creepQueryService, "CreepQueryService");
        }

        /// <summary>
        /// 初始化建築相關服務
        /// </summary>
        private void InitializeBuildingServices()
        {
            Debug.Log("[GameBootstrapper] 初始化建築服務...");

            // 創建建築服務實例
            var buildingQueryService = new BuildingQueryService();
            var buildingConstructionService = new BuildingConstructionService();
            var researchService = new ResearchService();

            // 註冊服務到ServiceLocator
            ServiceLocator.Register<IBuildingQueryService>(buildingQueryService, "BuildingQueryService");
            ServiceLocator.Register<IBuildingConstructionService>(buildingConstructionService, "BuildingConstructionService");
            ServiceLocator.Register<IResearchService>(researchService, "ResearchService");
        }

        /// <summary>
        /// 初始化單位相關服務
        /// </summary>
        private void InitializeUnitServices()
        {
            Debug.Log("[GameBootstrapper] 初始化單位服務...");

            // 創建單位服務實例
            var unitQueryService = new UnitQueryService();
            var unitCommandService = new UnitCommandService();

            // 註冊服務到ServiceLocator
            ServiceLocator.Register<IUnitQueryService>(unitQueryService, "UnitQueryService");
            ServiceLocator.Register<IUnitCommandService>(unitCommandService, "UnitCommandService");
        }

        /// <summary>
        /// 初始化其他核心服務
        /// </summary>
        private void InitializeOtherServices()
        {
            Debug.Log("[GameBootstrapper] 初始化其他核心服務...");
            
            // 這裡可以添加其他需要的服務
            // 例如：音效服務、UI服務、存檔服務等
        }

        /// <summary>
        /// 驗證所有服務是否正確註冊
        /// </summary>
        private void ValidateServices()
        {
            Debug.Log("[GameBootstrapper] 驗證服務註冊狀態...");

            var requiredServices = new System.Type[]
            {
                typeof(ICreepGridService),
                typeof(ICreepSourceService),
                typeof(ICreepNetworkService),
                typeof(ICreepQueryService),
                typeof(IBuildingQueryService),
                typeof(IBuildingConstructionService),
                typeof(IResearchService),
                typeof(IUnitQueryService),
                typeof(IUnitCommandService)
            };

            bool allServicesRegistered = true;

            foreach (var serviceType in requiredServices)
            {
                if (!ServiceLocator.IsRegistered(serviceType))
                {
                    Debug.LogError($"[GameBootstrapper] 必要服務未註冊: {serviceType.Name}");
                    allServicesRegistered = false;
                }
            }

            if (allServicesRegistered)
            {
                Debug.Log("[GameBootstrapper] 所有必要服務已正確註冊");
            }
            else
            {
                Debug.LogError("[GameBootstrapper] 部分必要服務未註冊，可能影響遊戲功能");
            }
        }

        /// <summary>
        /// 應用程序退出時清理服務
        /// </summary>
        private void OnApplicationQuit()
        {
            Debug.Log("[GameBootstrapper] 應用程序退出，清理服務...");
            ServiceLocator.Clear();
        }

        /// <summary>
        /// 在編輯器中提供重新初始化功能
        /// </summary>
        [ContextMenu("重新初始化服務")]
        private void ReinitializeServices()
        {
            ServiceLocator.Clear();
            InitializeServices();
            Debug.Log("[GameBootstrapper] 服務重新初始化完成");
        }
    }
}