using System.Linq;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Buildings.Selection;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Common.Placement
{
    /// <summary>
    /// 將 BuildingSelectionProvider 的選擇同步到場景中的 BuildingPlacer。
    /// - 若 Placer 具有 public/protected SetPrefab(GameObject) 會優先呼叫。
    /// - 否則以常見欄位名（prefab/buildingPrefab/currentPrefab/defaultPrefab）反射賦值。
    /// - 僅在選擇變更時觸發，不刷屏。
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class BuildingPlacerSelectionBridge : MonoBehaviour
    {
        private MonoBehaviour _placer;
        private MethodInfo _setPrefab;
        private FieldInfo _prefabField;
        private bool _warned;

        private void Awake()
        {
            // 嘗試在本物件或全場景找到 BuildingPlacer（避免直接依賴命名空間）
            _placer = GetComponents<MonoBehaviour>().FirstOrDefault(c => c.GetType().Name == "BuildingPlacer")
                   ?? FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(c => c.GetType().Name == "BuildingPlacer");
            if (_placer != null)
            {
                var t = _placer.GetType();
                _setPrefab = t.GetMethod("SetPrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(GameObject) }, null);
                string[] candidates = { "prefab", "buildingPrefab", "currentPrefab", "defaultPrefab" };
                foreach (var n in candidates)
                {
                    _prefabField = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (_prefabField != null && _prefabField.FieldType == typeof(GameObject)) break;
                }
            }
        }

        private void OnEnable()
        {
            BuildingSelectionProvider.OnSelectionChanged += OnSelectionChanged;
        }
        private void OnDisable()
        {
            BuildingSelectionProvider.OnSelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(BuildingCatalogEntry entry)
        {
            if (entry == null || entry.prefab == null) return;
            if (_placer == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    DAHLog.Warning(LogCategory.PLACEMENT, "[BuildingCycle] No BuildingPlacer found; selection will not affect preview.");
                }
                return;
            }

            try
            {
                if (_setPrefab != null)
                {
                    _setPrefab.Invoke(_placer, new object[] { entry.prefab });
                    DAHLog.Info(LogCategory.PLACEMENT, "[BuildingCycle] Applied to BuildingPlacer via SetPrefab().");
                    return;
                }
                if (_prefabField != null)
                {
                    _prefabField.SetValue(_placer, entry.prefab);
                    DAHLog.Info(LogCategory.PLACEMENT, $"[BuildingCycle] Applied to BuildingPlacer field {_prefabField.Name}.");
                    return;
                }
                if (!_warned)
                {
                    _warned = true;
                    DAHLog.Warning(LogCategory.PLACEMENT, "[BuildingCycle] Placer has no SetPrefab() or known prefab field. Please expose SetPrefab(GameObject).");
                }
            }
            catch (System.Exception ex)
            {
                if (!_warned)
                {
                    _warned = true;
                    DAHLog.Warning(LogCategory.PLACEMENT, $"[BuildingCycle] Apply failed: {ex.Message}");
                }
            }
        }
    }
}