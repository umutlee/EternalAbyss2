using UnityEngine;
using System.Linq;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Buildings.Runtime
{
    /// <summary>集中管理 BuildingCatalog 與目前選中索引（Runtime 單例）。</summary>
    public sealed class BuildingCatalogRuntime : MonoBehaviour
    {
        public static BuildingCatalogRuntime Instance { get; private set; }
        public ScriptableObject CatalogAsset; // 將於 Awake 自動從 Resources 載入
        public int CurrentIndex { get; private set; } = 0;

        private System.Array _entries; // 延遲用反射取 items 陣列
        private System.Type _entryType;
        private System.Reflection.FieldInfo _idField;
        private System.Reflection.FieldInfo _prefabField;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("BuildingCatalogRuntime");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildingCatalogRuntime>();
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            // 嘗試從 Resources/Configs 載入 BuildingCatalog
            CatalogAsset = Resources.Load<ScriptableObject>("Configs/BuildingCatalog");
            if (CatalogAsset == null)
            {
                DAHLog.Warn(LogCategory.CONFIG, "[PLACEMENT] 未找到 Resources/Configs/BuildingCatalog.asset（可先建立再回來）。");
                return;
            }

            // 反射定位 entries 與欄位
            var t = CatalogAsset.GetType();
            var itemsField = t.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                              .FirstOrDefault(f => f.FieldType.IsArray);
            if (itemsField == null)
            {
                DAHLog.Error(LogCategory.CONFIG, "[PLACEMENT] BuildingCatalogSO 缺少陣列欄位（items/entries）。");
                return;
            }

            _entries = (System.Array)itemsField.GetValue(CatalogAsset);
            if (_entries == null || _entries.Length == 0)
            {
                DAHLog.Warn(LogCategory.CONFIG, "[PLACEMENT] BuildingCatalog 為空，請先在 Inspector 填入項目。");
                return;
            }

            _entryType = _entries.GetType().GetElementType();
            _idField = _entryType.GetField("id", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
            _prefabField = _entryType.GetField("prefab", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance);
            DAHLog.Info(LogCategory.PLACEMENT, $"[PLACEMENT] Catalog 載入完成（{_entries.Length} 項）。");
        }

        public int Count => _entries == null ? 0 : _entries.Length;

        public string CurrentId
        {
            get
            {
                if (_entries == null || Count == 0) return "(none)";
                var e = _entries.GetValue(Mathf.Clamp(CurrentIndex, 0, Count-1));
                return (string)_idField?.GetValue(e) ?? "(id)";
            }
        }

        public GameObject CurrentPrefab
        {
            get
            {
                if (_entries == null || Count == 0) return null;
                var e = _entries.GetValue(Mathf.Clamp(CurrentIndex, 0, Count-1));
                return (GameObject)_prefabField?.GetValue(e);
            }
        }

        public bool Step(int delta)
        {
            if (Count == 0) return false;
            CurrentIndex = (CurrentIndex + delta + Count) % Count;
            return true;
        }
    }
}