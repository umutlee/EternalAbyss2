using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Buildings.Config;

namespace DeepAbyssHive.Buildings.Runtime
{
    /// <summary>
    /// 簡化的建築目錄綁定器：只負責設置建築到 BuildingPlacer
    /// </summary>
    public class BuildingCatalogBinder : MonoBehaviour
    {
        [SerializeField] private int _currentIndex = 0;
        [SerializeField] private BuildingCatalogSO _catalog;
        
        private Component _buildingPlacer;
        private bool _isInjected = false;

        private void Start()
        {
            LoadCatalogFromConfig();
            InjectToBuildingPlacer();
        }

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
            
            if (_catalog.Count > 0)
            {
                _currentIndex = Mathf.Clamp(_currentIndex, 0, _catalog.Count - 1);
            }
        }

        /// <summary>
        /// 找到場景中的 BuildingPlacer
        /// </summary>
        private void InjectToBuildingPlacer()
        {
            var placer = FindPlacerStatic();
            if (placer == null)
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] BuildingPlacer not found in scene");
                return;
            }

            _buildingPlacer = placer;
            _isInjected = true;
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已注入到 {placer.name} 的 BuildingPlacer 組件");
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

        /// <summary>
        /// 設置指定索引的建築並進入放置模式
        /// </summary>
        public void SelectBuilding(int index)
        {
            if (_catalog == null || index < 0 || index >= _catalog.Count) return;
            
            _currentIndex = index;
            var entry = _catalog.Get(index);
            if (entry?.prefab != null)
            {
                ApplyToPlacer(entry.prefab);
                DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 選擇建築：[{index}] {entry.prefab.name}");
            }
        }

        /// <summary>
        /// 應用建築到 BuildingPlacer 並進入放置模式
        /// </summary>
        private void ApplyToPlacer(GameObject prefab)
        {
            if (!_isInjected || _buildingPlacer == null) return;
            
            // 設置 prefab
            TrySetPrefab(_buildingPlacer, prefab);
            
            // 進入放置模式
            TryStartPlacing(_buildingPlacer);
        }

        /// <summary>
        /// 嘗試設置 prefab 到 BuildingPlacer
        /// </summary>
        private bool TrySetPrefab(Component placer, GameObject prefab)
        {
            var type = placer.GetType();
            
            // 嘗試 SetPrefab 方法
            var method = type.GetMethod("SetPrefab", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(placer, new object[] { prefab });
                return true;
            }
            
            // 嘗試 PrefabToPlace 屬性
            var property = type.GetProperty("PrefabToPlace", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(placer, prefab);
                return true;
            }
            
            // 嘗試 placePrefab 字段
            var field = type.GetField("placePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(placer, prefab);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 嘗試讓 BuildingPlacer 進入放置模式
        /// </summary>
        private void TryStartPlacing(Component placer)
        {
            var type = placer.GetType();
            
            // 嘗試 StartPlacing 方法
            var method = type.GetMethod("StartPlacing", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(placer, null);
            }
        }

        /// <summary>
        /// 靜態 API：直接設置指定 prefab 到場景中的 BuildingPlacer
        /// </summary>
        public static void ApplyPrefabToPlacer(GameObject prefab, string name = null, int index = -1)
        {
            var placer = FindPlacerStatic();
            if (placer == null)
            {
                DAHLog.Warn(LogCategory.PLACEMENT, "[CatalogBinder] ApplyPrefabToPlacer: BuildingPlacer not found.");
                return;
            }
            if (prefab == null)
            {
                DAHLog.Warn(LogCategory.PLACEMENT, "[CatalogBinder] ApplyPrefabToPlacer: prefab is null.");
                return;
            }

            var binder = FindObjectOfType<BuildingCatalogBinder>();
            if (binder != null)
            {
                binder.ApplyToPlacer(prefab);
            }
        }

        /// <summary>
        /// 找到場景中的 BuildingPlacer
        /// </summary>
        private static Component FindPlacerStatic()
        {
            var placers = FindObjectsOfType<MonoBehaviour>();
            foreach (var placer in placers)
            {
                if (placer.GetType().Name == "BuildingPlacer")
                    return placer;
            }
            return null;
        }

        /// <summary>
        /// 自動啟動：在場景載入後自動創建 BuildingCatalogBinder
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartup()
        {
            // 檢查是否已存在
            if (FindObjectOfType<BuildingCatalogBinder>() != null)
            {
                return;
            }
            
            // 創建新的 GameObject 並掛載組件
            var go = new GameObject("BuildingCatalogBinder");
            go.AddComponent<BuildingCatalogBinder>();
            
            // 設為 DontDestroyOnLoad 以便跨場景使用
            DontDestroyOnLoad(go);
            
            DAHLog.Info(LogCategory.SERVICE, "[BuildingCatalogBinder] 自動啟動完成");
        }
    }
}