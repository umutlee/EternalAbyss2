using UnityEngine;
using System;
using System.Collections;
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
        // --- T17i fields: verify throttle & state ---
        private float _verifyCooldownSec = 2f;
        private float _lastVerifyAt = -999f;
        private int _resetsThisSelection = 0;
        private int _maxResets = 3;
        private string _lastSetName = null;

        [Header("Runtime State")]
        [SerializeField] private int _currentIndex = 0;
        [SerializeField] private BuildingCatalogSO _catalog;
        
        [Header("Auto-Start Control")]
        [SerializeField] private bool autoApplyOnStart = false;
        
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
                // 總是進行基本的目錄初始化，確保 Tab/BackQuote 切換功能正常
                // 但只有在 autoApplyOnStart=true 時才自動進入放置模式
                if (autoApplyOnStart)
                {
                    SyncPreviewToPlacer(); // 自動進入放置模式
                    DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已載入目錄：{_catalog.Count} 個建築，當前索引：{_currentIndex}（自動啟動）");
                }
                else
                {
                    // 進行基本初始化但不進入放置模式
                    InitializeCatalogOnly();
                    DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已載入目錄：{_catalog.Count} 個建築，當前索引：{_currentIndex}（待手動啟動）");
                }
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
            
            // 切換時總是同步預覽並進入放置模式
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
            
            // 切換時總是同步預覽並進入放置模式
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
            var placer = FindPlacerStatic();
            if (placer == null)
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] BuildingPlacer not found in scene");
                return;
            }

            _buildingPlacer = placer;
            var prefab = GetCurrentBuilding();
            if (prefab == null) return;

            // 1) 嘗試方法 / 欄位 / 屬性多名稱（命中任意一個即成功）
            if (!TrySetAnyPrefab(placer, prefab))
            {
                DAHLog.Warn(LogCategory.SERVICE, "[BuildingCatalogBinder] no known prefab setter on BuildingPlacer (tried multiple names)");
                PrintPrefabMembersOnce(placer);
                return;
            }

            // 2) 嘗試刷新預覽（常見名稱擴充）
            if (!(TryInvokeMethod(placer, placer.GetType(), "RefreshPreview") ||
                  TryInvokeMethod(placer, placer.GetType(), "RebuildPreview") ||
                  TryInvokeMethod(placer, placer.GetType(), "RecreatePreview") ||
                  TryInvokeMethod(placer, placer.GetType(), "UpdatePreview") ||
                  TryInvokeMethod(placer, placer.GetType(), "BuildPreview") ||
                  TryInvokeMethod(placer, placer.GetType(), "CreatePreview")))
            {
                PrintPreviewMethodsOnce(placer);
            }
            
            _isInjected = true;
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 已成功注入到 {placer.name} 的 BuildingPlacer 組件");
        }

        /// <summary>
        /// 僅初始化目錄，不進入放置模式（用於 autoApplyOnStart=false 的情況）
        /// </summary>
        private void InitializeCatalogOnly()
        {
            if (!_isInjected) return;
            
            var currentBuilding = GetCurrentBuilding();
            if (currentBuilding != null)
            {
                // 只設置 prefab 到 BuildingPlacer，但不進入放置模式
                TryApplyPrefabToPlacer(currentBuilding, null, _currentIndex);
                
                // 不調用 TryEnterPlacingMode()，保持正常狀態
                DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 目錄初始化完成，當前建築：{currentBuilding.name}");
            }
        }

        /// <summary>
        /// 同步當前建築到 BuildingPlacer 的預覽（強化版：多路設置+延遲驗證）
        /// </summary>
        private void SyncPreviewToPlacer()
        {
            if (!_isInjected) return;
            
            var currentBuilding = GetCurrentBuilding();
            if (currentBuilding != null)
            {
                TryApplyPrefabToPlacer(currentBuilding, null, _currentIndex);
                
                // 強制進入放置模式
                TryEnterPlacingMode();
                
                // 延遲驗證：應對某些 Placer 在 Start/Update 內重置的情況
                StartCoroutine(DelayedVerify(currentBuilding, currentBuilding.name, _currentIndex));
            }
        }
        
        IEnumerator DelayedVerify(GameObject expectedPrefab, string expectedName, int catalogIndex)
        {
            // 初次延後一幀，讓預覽建立
            yield return null;

            // 若目前不在放置模式，不驗證（避免非放置時刷屏）
            if (!IsPlacingMode()) yield break;

            // 節流：至少間隔 _verifyCooldownSec 秒才驗證
            if (UnityEngine.Time.realtimeSinceStartup - _lastVerifyAt < _verifyCooldownSec)
                yield break;

            _lastVerifyAt = UnityEngine.Time.realtimeSinceStartup;

            // 讀現在 Placer 上的 prefab 名稱
            string current = GetCurrentPlacerPrefabName(_buildingPlacer);
            if (SamePrefabName(current, expectedName))
                yield break; // 已一致，不需處理

            // 嘗試重設，但限制最多重試次數，避免無限循環
            if (_resetsThisSelection >= _maxResets)
                yield break;

            DeepAbyssHive.Core.Logging.DAHLog.Warn(DeepAbyssHive.Core.Logging.LogCategory.SERVICE,
                $"[BuildingCatalogBinder] 預覽不同步，重新設置：{expectedName}");

            TryApplyPrefabToPlacer(expectedPrefab, expectedName, catalogIndex);
            _resetsThisSelection++;
        }
        
        /// <summary>
        /// 檢查 Placer 是否正在使用指定的 prefab
        /// </summary>
        private bool IsPlacerUsing(GameObject prefab)
        {
            if (_buildingPlacer == null || prefab == null) return false;
            
            var t = _buildingPlacer.GetType();
            // 檢查各種可能的欄位名
            var fields = new[] { "prefabToPlace", "previewPrefab", "currentPrefab", "prefab" };
            foreach (var fieldName in fields)
            {
                var f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && ReferenceEquals(f.GetValue(_buildingPlacer), prefab)) return true;
            }
            
            // 檢查屬性
            var props = new[] { "PrefabToPlace", "PreviewPrefab", "CurrentPrefab", "Prefab" };
            foreach (var propName in props)
            {
                var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && ReferenceEquals(p.GetValue(_buildingPlacer, null), prefab)) return true;
            }
            
            return false;
        }

        #endregion

        #region Auto Startup

        /// <summary>
        /// 自動啟動：在場景載入後自動創建 BuildingCatalogBinder
        /// 注意：組件總是啟動以支援按鍵切換，但是否自動進入放置模式由 placementAutoStart 控制
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
            
            // 創建新的 GameObject 並掛載組件（總是創建以支援按鍵切換）
            var go = new GameObject("BuildingCatalogBinder");
            var binder = go.AddComponent<BuildingCatalogBinder>();
            
            // 根據 placementAutoStart 設置是否自動進入放置模式
            var config = GameConfigProvider.Current;
            binder.autoApplyOnStart = config.placementAutoStart;
            
            // 設為 DontDestroyOnLoad 以便跨場景使用
            DontDestroyOnLoad(go);
            
            DAHLog.Info(LogCategory.SERVICE, $"[BuildingCatalogBinder] 自動啟動完成，autoApplyOnStart={binder.autoApplyOnStart}");
        }

        /// <summary>
        /// 嘗試進入放置模式
        /// </summary>
        private void TryEnterPlacingMode()
        {
            if (_buildingPlacer == null) return;
            
            var t = _buildingPlacer.GetType();
            
            // 檢查當前狀態
            var isPlacingField = t.GetField("isPlacing", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            bool currentlyPlacing = false;
            if (isPlacingField != null && isPlacingField.FieldType == typeof(bool))
            {
                currentlyPlacing = (bool)isPlacingField.GetValue(_buildingPlacer);
            }
            
            // 如果已經在放置模式，不需要再切換
            if (currentlyPlacing) return;
            
            // 嘗試調用 TogglePlacing() 方法
            var toggleMethod = t.GetMethod("TogglePlacing", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (toggleMethod != null)
            {
                toggleMethod.Invoke(_buildingPlacer, null);
                DAHLog.Info(LogCategory.SERVICE, "[BuildingCatalogBinder] 進入放置模式");
            }
            else if (isPlacingField != null)
            {
                // 直接設置 isPlacing = true
                isPlacingField.SetValue(_buildingPlacer, true);
                DAHLog.Info(LogCategory.SERVICE, "[BuildingCatalogBinder] 直接設置放置模式");
            }
        }

        #endregion
        
        #region Static API
        
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

            // 1) 嘗試常見 setter / 欄位 / 屬性（包含相容名稱）
            if (!TrySetAnyPrefab(placer, prefab))
            {
                DAHLog.Warn(LogCategory.PLACEMENT, "[CatalogBinder] ApplyPrefabToPlacer: no known setter on BuildingPlacer.");
                PrintPrefabMembersOnce(placer);
            }

            // 刪除既有預覽實例（若有），迫使 Placer 重建
            var t = placer.GetType();
            var pi = t.GetField("previewInstance", BF) ?? t.GetField("_previewInstance", BF) ?? t.GetField("previewGO", BF) ?? t.GetField("_previewGO", BF) ?? t.GetField("_preview", BF) ?? t.GetField("previewObject", BF);
            var go = pi?.GetValue(placer) as GameObject;
            if (go != null) { try { GameObject.Destroy(go); } catch { } }

            // 2) 嘗試刷新預覽（擴充命名）
            if (!(TryInvokeMethod(placer, t, "RefreshPreview") ||
                  TryInvokeMethod(placer, t, "RebuildPreview") ||
                  TryInvokeMethod(placer, t, "RecreatePreview") ||
                  TryInvokeMethod(placer, t, "UpdatePreview") ||
                  TryInvokeMethod(placer, t, "BuildPreview") ||
                  TryInvokeMethod(placer, t, "CreatePreview")))
            {
                // 列印一次診斷資訊，協助對應名稱
                PrintPreviewMethodsOnce(placer);
            }
        }

        void TryApplyPrefabToPlacer(GameObject prefab, string name, int index)
        {
            // 原本邏輯（透過反射把 prefab 指到 BuildingPlacer）保持不變：
            TrySetAnyPrefab(_buildingPlacer, prefab);

            // T17i：記錄狀態，重置統計
            _lastSetName = name;
            _resetsThisSelection = 0;
            _lastVerifyAt = -999f;
        }
        
        /// <summary>
        /// 嘗試以各種常見名稱，把 prefab 套到 Placer。
        /// 依序偏好：方法 → 欄位 → 屬性。命中一個即回 true。
        /// </summary>
        private static bool TrySetAnyPrefab(Component placer, GameObject prefab)
        {
            // 方法
            var methodNames = new[]{ "SetPreviewPrefab", "SetPrefab", "SetBuildingPrefab", "ApplyPrefab", "SetCurrentPrefab" };
            foreach (var m in methodNames) if (TryInvokeMethod(placer, placer.GetType(), m, prefab)) { DAHLog.Info(LogCategory.SERVICE, $"[CatalogBinder] Setter(Method) = {m}"); return true; }

            // 欄位（相容名稱擴充）
            var fieldNames = new[]{ "previewPrefab","currentPrefab","prefab","prefabToPlace","buildingPrefab","placePrefab","prefabToBuild" };
            foreach (var f in fieldNames) if (TrySetField(placer, placer.GetType(), f, prefab)) { DAHLog.Info(LogCategory.SERVICE, $"[CatalogBinder] Setter(Field)  = {f}"); return true; }

            // 屬性
            var propNames  = new[]{ "PreviewPrefab","CurrentPrefab","Prefab","PrefabToPlace","BuildingPrefab" };
            foreach (var p in propNames) if (TrySetProperty(placer, placer.GetType(), p, prefab)) { DAHLog.Info(LogCategory.SERVICE, $"[CatalogBinder] Setter(Prop)   = {p}"); return true; }

            return false;
        }

        private static bool _printedMembers, _printedPreviewMethods;
        private static void PrintPrefabMembersOnce(Component placer)
        {
            if (_printedMembers || placer == null) return;
            _printedMembers = true;
            var t = placer.GetType();
            var fields = t.GetFields(BF);
            var props  = t.GetProperties(BF);
            System.Collections.Generic.List<string> fList = new System.Collections.Generic.List<string>();
            foreach (var f in fields) if (f.FieldType == typeof(GameObject) && f.Name.IndexOf("prefab", StringComparison.OrdinalIgnoreCase) >= 0) fList.Add(f.Name);
            System.Collections.Generic.List<string> pList = new System.Collections.Generic.List<string>();
            foreach (var p in props) if (p.PropertyType == typeof(GameObject) && p.Name.IndexOf("prefab", StringComparison.OrdinalIgnoreCase) >= 0) pList.Add(p.Name);
            DAHLog.Info(LogCategory.SERVICE, $"[CatalogBinder] Placer prefab-like fields: {string.Join(", ", fList)}; props: {string.Join(", ", pList)}");
        }
        private static void PrintPreviewMethodsOnce(Component placer)
        {
            if (_printedPreviewMethods || placer == null) return;
            _printedPreviewMethods = true;
            var t = placer.GetType();
            var methods = t.GetMethods(BF);
            System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
            foreach (var m in methods) if (m.Name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0) names.Add(m.Name);
            DAHLog.Info(LogCategory.SERVICE, $"[CatalogBinder] Placer preview-like methods: {string.Join(", ", names)}");
        }
        
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
        
        private static bool TrySetField(Component target, Type type, string fieldName, object value)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }
            }
            catch { }
            return false;
        }
        
        private static bool TrySetProperty(Component target, Type type, string propName, object value)
        {
            try
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value, null);
                    return true;
                }
            }
            catch { }
            return false;
        }
        
        private static bool TryInvokeMethod(Component target, Type type, string methodName, params object[] args)
        {
            try
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(target, args);
                    return true;
                }
            }
            catch { }
            return false;
        }
        
        private const BindingFlags BF = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase;
        
        #endregion

        // 以反射讀取 BuildingPlacer 的『目前預覽 Prefab 名稱』
        private string GetCurrentPlacerPrefabName(Component placer)
        {
            if (placer == null) return null;
            // 常見欄位/屬性名：prefabToPlace/placePrefab/previewPrefab
            foreach (var n in new[] { "prefabToPlace", "placePrefab", "previewPrefab" })
            {
                var f = placer.GetType().GetField(n, System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.GetValue(placer) is GameObject go && go != null) return go.name;
                var p = placer.GetType().GetProperty(n, System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.GetValue(placer) is GameObject gop && gop != null) return gop.name;
            }
            return null;
        }

        private bool IsPlacingMode()
        {
            if (_buildingPlacer == null) return false;
            // 應付多版本：isPlacing / isPlacingMode / IsPlacing / IsPlacingMode
            foreach (var n in new[] { "isPlacing", "isPlacingMode", "IsPlacing", "IsPlacingMode" })
            {
                var f = _buildingPlacer.GetType().GetField(n, System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(_buildingPlacer);
                var p = _buildingPlacer.GetType().GetProperty(n, System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(_buildingPlacer, null);
            }
            // 若無旗標，保守視為 true（沿用舊版行為）
            return true;
        }

        private static bool SamePrefabName(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            string norm(string s) => s.Replace("(Clone)", string.Empty).Trim().ToLowerInvariant();
            return norm(a) == norm(b);
        }
    } // class BuildingCatalogBinder
}