using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Units.Services;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 單位管理器服務部分
    /// 負責通過ServiceLocator獲取和管理依賴服務
    /// </summary>
    public partial class UnitManager
    {
        [Header("服務依賴")]
        [SerializeField] private bool useServiceLocator = true;
        
        // 服務介面引用（使用介面類型而非具體實現）
        private IUnitQueryService _unitQueryService;
        private IUnitCommandService _unitCommandService;
        
        // 服務初始化狀態
        private bool _unitServicesInitialized = false;

        /// <summary>
        /// 初始化單位服務依賴
        /// </summary>
        private void InitializeUnitServices()
        {
            if (_unitServicesInitialized) return;

            Debug.Log($"[{_managerName}] 初始化單位服務依賴...");

            if (useServiceLocator)
            {
                InitializeFromServiceLocator();
            }
            else
            {
                InitializeFromLegacyMethod();
            }

            _unitServicesInitialized = true;
            Debug.Log($"[{_managerName}] 單位服務依賴初始化完成");
        }

        /// <summary>
        /// 從ServiceLocator獲取服務
        /// </summary>
        private void InitializeFromServiceLocator()
        {
            try
            {
                // 使用 ServiceLocator 注入所需的單位相關服務
                _unitQueryService = ServiceLocator.Get<IUnitQueryService>();
                _unitCommandService = ServiceLocator.Get<IUnitCommandService>();

                Debug.Log($"[{_managerName}] 成功從ServiceLocator獲取所有單位服務");
            }
            catch (ServiceNotFoundException ex)
            {
                Debug.LogError($"[{_managerName}] 單位服務獲取失敗: {ex.Message}");
                
                // 回退到舊版初始化方法
                Debug.LogWarning($"[{_managerName}] 回退到舊版單位服務初始化方法");
                InitializeFromLegacyMethod();
            }
        }

        /// <summary>
        /// 舊版服務初始化方法（向後兼容）
        /// </summary>
        private void InitializeFromLegacyMethod()
        {
            Debug.Log($"[{_managerName}] 使用舊版單位服務初始化方法");
            
            // 直接創建服務實例（向後兼容）
            _unitQueryService = new UnitQueryService();
            _unitCommandService = new UnitCommandService();
        }

        /// <summary>
        /// 在Start中初始化服務（確保ServiceLocator已準備好）
        /// </summary>
        private void StartUnitServices()
        {
            // 等待ServiceLocator初始化完成
            if (useServiceLocator && !ServiceLocator.IsRegistered<IUnitQueryService>())
            {
                Debug.LogWarning($"[{_managerName}] ServiceLocator尚未初始化，等待...");
                DeepAbyssHive.Core.Utils.CoroutineStub.Start(WaitForUnitServiceLocatorInitialization());
            }
            else
            {
                InitializeUnitServices();
            }
        }

        /// <summary>
        /// 等待ServiceLocator初始化完成
        /// </summary>
        private System.Collections.IEnumerator WaitForUnitServiceLocatorInitialization()
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
                InitializeUnitServices();
            }
            else
            {
                Debug.LogError($"[{_managerName}] ServiceLocator初始化超時，使用舊版方法");
                useServiceLocator = false;
                InitializeUnitServices();
            }
        }

        /// <summary>
        /// 獲取單位查詢服務
        /// </summary>
        /// <returns>單位查詢服務實例</returns>
        public IUnitQueryService GetUnitQueryService()
        {
            if (!_unitServicesInitialized)
            {
                InitializeUnitServices();
            }
            return _unitQueryService;
        }

        /// <summary>
        /// 獲取單位命令服務
        /// </summary>
        /// <returns>單位命令服務實例</returns>
        public IUnitCommandService GetUnitCommandService()
        {
            if (!_unitServicesInitialized)
            {
                InitializeUnitServices();
            }
            return _unitCommandService;
        }

        /// <summary>
        /// 檢查單位服務是否已正確初始化
        /// </summary>
        /// <returns>服務是否可用</returns>
        public bool AreUnitServicesAvailable()
        {
            return _unitServicesInitialized && 
                   _unitQueryService != null && 
                   _unitCommandService != null;
        }

        /// <summary>
        /// 重新初始化單位服務（用於調試或熱重載）
        /// </summary>
        [ContextMenu("重新初始化單位服務")]
        public void ReinitializeUnitServices()
        {
            _unitServicesInitialized = false;
            _unitQueryService = null;
            _unitCommandService = null;
            
            InitializeUnitServices();
        }
    }
}