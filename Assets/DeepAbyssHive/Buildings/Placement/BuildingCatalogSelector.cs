using UnityEngine;
using DeepAbyssHive.Buildings.Runtime;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Buildings.Placement
{
    /// <summary>
    /// 監聽上一個/下一個鍵，切換 Catalog 選中，並嘗試把 Prefab 注入現有 BuildingPlacer（反射支援多名稱）。
    /// </summary>
    public class BuildingCatalogSelector : MonoBehaviour
    {
        private KeyCode _prev = KeyCode.LeftBracket; // '['
        private KeyCode _next = KeyCode.RightBracket; // ']'
        private float _lastLog;

        private void Start()
        {
            // 讀 GameConfig（反射，若不存在使用預設）
            GameConfigKeysCompat.TryBindOrDefault("prevBuildingKey", ref _prev);
            GameConfigKeysCompat.TryBindOrDefault("nextBuildingKey", ref _next);

            // 初次套用
            ApplyToPlacer();
            DAHLog.Info(LogCategory.PLACEMENT, $"[PLACEMENT] 建築切換鍵：prev={_prev}, next={_next}; current={BuildingCatalogRuntime.Instance?.CurrentId}");
        }

        private void Update()
        {
            var cat = BuildingCatalogRuntime.Instance;
            if (cat == null || cat.Count == 0) return;

            bool changed = false;
            if (Input.GetKeyDown(_prev)) { changed = cat.Step(-1); }
            else if (Input.GetKeyDown(_next)) { changed = cat.Step(1); }

            if (changed)
            {
                ApplyToPlacer();
                ThrottledLog($"selected={cat.CurrentId}");
            }
        }

        private void ApplyToPlacer()
        {
            var cat = BuildingCatalogRuntime.Instance;
            var prefab = cat?.CurrentPrefab;
            if (prefab == null) return;

            // 以反射找場景中的 BuildingPlacer 並注入 Prefab
            System.Type placerType = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tp in asm.GetTypes())
                {
                    if (tp.Name == "BuildingPlacer" && typeof(MonoBehaviour).IsAssignableFrom(tp))
                    {
                        placerType = tp; break;
                    }
                }
                if (placerType != null) break;
            }
            if (placerType == null)
            {
                DAHLog.Warn(LogCategory.PLACEMENT, "[PLACEMENT] 找不到 BuildingPlacer 類別，無法注入選中 Prefab。");
                return;
            }

            var placerObj = Object.FindObjectOfType(placerType) as MonoBehaviour;
            if (placerObj == null)
            {
                DAHLog.Warn(LogCategory.PLACEMENT, "[PLACEMENT] 場景裡尚未有 BuildingPlacer。");
                return;
            }

            // 嘗試多種命名：欄位或方法
            var tP = placerType;
            var flags = System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var fields = new[] { "prefab", "buildingPrefab", "currentPrefab", "previewPrefab", "placePrefab" };
            foreach (var f in fields)
            {
                var fi = tP.GetField(f, flags);
                if (fi != null && fi.FieldType == typeof(GameObject))
                {
                    fi.SetValue(placerObj, prefab);
                    DAHLog.Info(LogCategory.PLACEMENT, $"[PLACEMENT] 注入 Placer.{f} ← {cat.CurrentId}");
                    TryRefreshPreview(tP, placerObj);
                    return;
                }
            }
            var methods = new[] { "SetPrefab", "SetPreviewPrefab", "SetBuildingPrefab" };
            foreach (var m in methods)
            {
                var mi = tP.GetMethod(m, flags);
                if (mi != null)
                {
                    var pars = mi.GetParameters();
                    if (pars.Length == 1 && pars[0].ParameterType == typeof(GameObject))
                    {
                        mi.Invoke(placerObj, new object[]{ prefab });
                        DAHLog.Info(LogCategory.PLACEMENT, $"[PLACEMENT] 呼叫 Placer.{m}({cat.CurrentId})");
                        TryRefreshPreview(tP, placerObj);
                        return;
                    }
                }
            }

            DAHLog.Warn(LogCategory.PLACEMENT, "[PLACEMENT] 無法以反射設定 Placer 的 Prefab，請手動接線或提供 SetPrefab(GameObject) API。");
        }

        private void TryRefreshPreview(System.Type tP, MonoBehaviour placerObj)
        {
            var flags = System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var mi = tP.GetMethod("RefreshPreview", flags);
            if (mi != null && mi.GetParameters().Length == 0) mi.Invoke(placerObj, null);
        }

        private void ThrottledLog(string msg)
        {
            if (Time.unscaledTime - _lastLog < 0.25f) return;
            _lastLog = Time.unscaledTime;
            DAHLog.Info(LogCategory.PLACEMENT, "[PLACEMENT] " + msg);
        }
    }
}