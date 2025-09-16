using UnityEngine;
using System.Reflection;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Buildings.Config;

namespace DeepAbyssHive.Buildings.Runtime
{
    /// <summary>
    /// 建築目錄綁定器：監聽 Tab/BackQuote 輸入，循環切換建築並同步到 BuildingPlacer 預覽
    /// 自包含設計，通過反射自動注入到場景中的 BuildingPlacer 組件
    /// </summary>
    public class BuildingCatalogBinder : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private int _currentIndex = 0;
        [SerializeField] private BuildingCatalogSO _catalog;
        
        // 反射快取
        private Component _buildingPlacer;
        private FieldInfo _prefabToPlaceField;
        private bool _isInjected = false;

        #region Unity Lifecycle

        private void Start()
        {
            LoadCatalogFromConfig();
            InjectToBuildingPlacer();
            
            if (_catalog != null && _catalog.Count > 0)
            {
                SyncPreviewToPlacer();
                DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已載入目錄：{_catalog.Count} 個建築，當前索引：{_currentIndex}");
            }
            else
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] 無有效建築目錄，功能停用");
            }
        }

        private void Update()
        {
            if (!_isInjected || _catalog == null || _catalog.Count == 0) return;

            var config = GameConfigProvider.Current;
            
            // Tab: 下一個建築
            if (config.buildingCycleNextKey != KeyCode.None && Input.GetKeyDown(config.buildingCycleNextKey))
            {
                CycleNext();
            }
            
            // BackQuote: 上一個建築  
            if (config.buildingCyclePrevKey != KeyCode.None && Input.GetKeyDown(config.buildingCyclePrevKey))
            {
                CyclePrev();
            }
        }

        #endregion

        #region Building Cycling

        /// <summary>
        /// 切換到下一個建築
        /// </summary>
        public void CycleNext()
        {
            if (_catalog == null || _catalog.Count == 0) return;
            
            _currentIndex = (_currentIndex + 1) % _catalog.Count;
            SyncPreviewToPlacer();
            
            var currentEntry = _catalog.Get(_currentIndex);
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 切換到下一個：[{_currentIndex}] {currentEntry.prefab.name}");
        }

        /// <summary>
        /// 切換到上一個建築
        /// </summary>
        public void CyclePrev()
        {
            if (_catalog == null || _catalog.Count == 0) return;
            
            _currentIndex = (_currentIndex - 1 + _catalog.Count) % _catalog.Count;
            SyncPreviewToPlacer();
            
            var currentEntry = _catalog.Get(_currentIndex);
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 切換到上一個：[{_currentIndex}] {currentEntry.prefab.name}");
        }

        /// <summary>
        /// 獲取當前選中的建築 Prefab
        /// </summary>
        public GameObject GetCurrentBuilding()
        {
            if (_catalog == null || _catalog.Count == 0 || _currentIndex < 0 || _currentIndex >= _catalog.Count)
                return null;
                
            var entry = _catalog.Get(_currentIndex);
            return entry?.prefab;
        }

        #endregion

        #region Configuration & Injection

        /// <summary>
        /// 從 GameConfig 載入建築目錄
        /// </summary>
        private void LoadCatalogFromConfig()
        {
            var config = GameConfigProvider.Current;
            _catalog = config.buildingCatalog;
            
            if (_catalog == null)
            {
                DAHLog.Warn(LogCategory.CONFIG, "[BuildingCatalogBinder] GameConfig.buildingCatalog 未設定");
                return;
            }
            
            // 確保索引有效
            if (_catalog.Count > 0)
            {
                _currentIndex = Mathf.Clamp(_currentIndex, 0, _catalog.Count - 1);
            }
        }

        /// <summary>
        /// 自動注入到場景中的 BuildingPlacer 組件
        /// </summary>
        private void InjectToBuildingPlacer()
        {
            // 尋找場景中的 BuildingPlacer（可能在 QA/Smoke/Dev 或其他位置）
            var placers = FindObjectsOfType<MonoBehaviour>();
            
            foreach (var placer in placers)
            {
                if (placer.GetType().Name == "BuildingPlacer")
                {
                    _buildingPlacer = placer;
                    break;
                }
            }
            
            if (_buildingPlacer == null)
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] 場景中未找到 BuildingPlacer 組件");
                return;
            }
            
            // 通過反射獲取 prefabToPlace 欄位
            var placerType = _buildingPlacer.GetType();
            _prefabToPlaceField = placerType.GetField("prefabToPlace", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (_prefabToPlaceField == null)
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] BuildingPlacer 中未找到 prefabToPlace 欄位");
                return;
            }
            
            _isInjected = true;
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已成功注入到 {_buildingPlacer.name} 的 BuildingPlacer 組件");
        }

        /// <summary>
        /// 同步當前建築到 BuildingPlacer 的預覽
        /// </summary>
        private void SyncPreviewToPlacer()
        {
            if (!_isInjected || _prefabToPlaceField == null) return;
            
            var currentBuilding = GetCurrentBuilding();
            if (currentBuilding != null)
            {
                _prefabToPlaceField.SetValue(_buildingPlacer, currentBuilding);
                DAHLog.Dev(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已同步預覽：{currentBuilding.name}");
            }
        }

        #endregion

        #region Auto Startup

        /// <summary>
        /// 自動啟動：在場景載入後自動創建 BuildingCatalogBinder
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartup()
        {
            // 檢查是否已存在
            if (FindObjectOfType<BuildingCatalogBinder>() != null)
            {
                DAHLog.Dev(LogCategory.SERVICE, "[BuildingCatalogBinder] 場景中已存在實例，跳過自動創建");
                return;
            }
            
            // 創建新的 GameObject 並掛載組件
            var go = new GameObject("BuildingCatalogBinder");
            go.AddComponent<BuildingCatalogBinder>();
            
            // 設為 DontDestroyOnLoad 以便跨場景使用
            DontDestroyOnLoad(go);
            
            DAHLog.Info(LogCategory.SERVICE, "[BuildingCatalogBinder] 自動啟動完成");
        }

        #endregion
    }
}