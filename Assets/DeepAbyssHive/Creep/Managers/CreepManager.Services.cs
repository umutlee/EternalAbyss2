using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Creep.Services;
// 明確使用介面命名空間的 ICreepSourceService，避免與 Services 同名型別衝突
using CreepSourceServiceInterface = DeepAbyssHive.Creep.Interfaces.ICreepSourceService;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// Creep管理器服務部分
    /// 負責通過ServiceLocator獲取和管理依賴服務
    /// </summary>
    public partial class CreepManager
    {
        [Header("服務依賴")]
        [SerializeField] private bool useServiceLocator = true;
        
        // 服務介面引用（使用介面類型而非具體實現）
        private ICreepQueryService _queryService;
        private CreepSourceServiceInterface _sourceService;
        private ICreepNetworkService _networkService;
        private ICreepGridService _gridService;
        private ICreepSimulationService _simulationService;
        
        // 服務初始化狀態
        private bool _servicesInitialized = false;

        /// <summary>
        /// 初始化服務依賴
        /// </summary>
        private void InitializeCreepServices()
        {
            if (_servicesInitialized) return;

            DAHLog.Info(LogCategory.MANAGER, $"[{_managerName}] 初始化Creep服務依賴...");

            if (useServiceLocator)
            {
                InitializeFromServiceLocator();
            }
            else
            {
                InitializeFromLegacyMethod();
            }

            _servicesInitialized = true;
            DAHLog.Info(LogCategory.MANAGER, $"[{_managerName}] Creep服務依賴初始化完成");
        }

        /// <summary>
        /// 從ServiceLocator獲取服務
        /// </summary>
        private void InitializeFromServiceLocator()
        {
            try
            {
                // 使用 ServiceLocator 注入所需的 Creep 相關服務
                _queryService = ServiceLocator.Get<ICreepQueryService>();
                _sourceService = ServiceLocator.Get<CreepSourceServiceInterface>();
                _networkService = ServiceLocator.Get<ICreepNetworkService>();
                _gridService = ServiceLocator.Get<ICreepGridService>();
                _simulationService = ServiceLocator.Get<ICreepSimulationService>();

                DAHLog.Info(LogCategory.MANAGER, $"[{_managerName}] 成功從ServiceLocator獲取所有Creep服務");
            }
            catch (ServiceNotFoundException ex)
            {
                DAHLog.Error(LogCategory.MANAGER, $"[{_managerName}] Creep服務獲取失敗: {ex.Message}");
                
                // 回退到舊版初始化方法
                DAHLog.Warning(LogCategory.MANAGER, $"[{_managerName}] 回退到舊版Creep服務初始化方法");
                InitializeFromLegacyMethod();
            }
        }

        /// <summary>
        /// 舊版服務初始化方法（向後兼容）
        /// </summary>
        private void InitializeFromLegacyMethod()
        {
            DAHLog.Info(LogCategory.MANAGER, $"[{_managerName}] 使用舊版Creep服務初始化方法");
            
            // 直接創建服務實例（向後兼容）
            _gridService = new CreepGridService();
            _sourceService = new DeepAbyssHive.Creep.Services.CreepSourceService() as DeepAbyssHive.Creep.Interfaces.ICreepSourceService;
            _networkService = new CreepNetworkService();
            _simulationService = new CreepSimulationService();
            _queryService = new CreepQueryService(_gridService, _sourceService as DeepAbyssHive.Creep.Services.ICreepSourceService, _networkService);
        }

        /// <summary>
        /// 在Start中初始化服務（確保ServiceLocator已準備好）
        /// </summary>
        private void StartCreepServices()
        {
            // 等待ServiceLocator初始化完成
            if (useServiceLocator && !ServiceLocator.IsRegistered<ICreepQueryService>())
            {
                DAHLog.Warning(LogCategory.MANAGER, $"[{_managerName}] ServiceLocator尚未初始化，等待...");
                StartCoroutine(WaitForServiceLocatorInitialization());
            }
            else
            {
                InitializeCreepServices();
            }
        }

        /// <summary>
        /// 等待ServiceLocator初始化完成
        /// </summary>
        private System.Collections.IEnumerator WaitForServiceLocatorInitialization()
        {
            float timeout = 5f; // 5秒超時
            float elapsed = 0f;

            while (!ServiceLocator.IsInitialized && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (ServiceLocator.IsInitialized)
            {
                InitializeCreepServices();
            }
            else
            {
                DAHLog.Error(LogCategory.MANAGER, $"[{_managerName}] ServiceLocator初始化超時，使用舊版方法");
                useServiceLocator = false;
                InitializeCreepServices();
            }
        }

        /// <summary>
        /// 獲取Creep查詢服務
        /// </summary>
        /// <returns>Creep查詢服務實例</returns>
        public ICreepQueryService GetQueryService()
        {
            if (!_servicesInitialized)
            {
                InitializeCreepServices();
            }
            return _queryService;
        }

        /// <summary>
        /// 獲取Creep源服務
        /// </summary>
        /// <returns>Creep源服務實例</returns>
        public CreepSourceServiceInterface GetSourceService()
        {
            if (!_servicesInitialized)
            {
                InitializeCreepServices();
            }
            return _sourceService;
        }

        /// <summary>
        /// 獲取Creep網絡服務
        /// </summary>
        /// <returns>Creep網絡服務實例</returns>
        public ICreepNetworkService GetNetworkService()
        {
            if (!_servicesInitialized)
            {
                InitializeCreepServices();
            }
            return _networkService;
        }

        /// <summary>
        /// 獲取Creep網格服務
        /// </summary>
        /// <returns>Creep網格服務實例</returns>
        public ICreepGridService GetGridService()
        {
            if (!_servicesInitialized)
            {
                InitializeCreepServices();
            }
            return _gridService;
        }

        /// <summary>
        /// 獲取Creep模擬服務
        /// </summary>
        /// <returns>Creep模擬服務實例</returns>
        public ICreepSimulationService GetSimulationService()
        {
            if (!_servicesInitialized)
            {
                InitializeCreepServices();
            }
            return _simulationService;
        }

        /// <summary>
        /// 檢查Creep服務是否已正確初始化
        /// </summary>
        /// <returns>服務是否可用</returns>
        public bool AreCreepServicesAvailable()
        {
            return _servicesInitialized && 
                   _queryService != null && 
                   _sourceService != null && 
                   _networkService != null &&
                   _gridService != null &&
                   _simulationService != null;
        }

        /// <summary>
        /// 重新初始化Creep服務（用於調試或熱重載）
        /// </summary>
        [ContextMenu("重新初始化Creep服務")]
        public void ReinitializeCreepServices()
        {
            _servicesInitialized = false;
            _queryService = null;
            _sourceService = null;
            _networkService = null;
            _gridService = null;
            _simulationService = null;
            
            InitializeCreepServices();
        }
    }
}