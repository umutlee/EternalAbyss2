using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Bootstrap
{
    /// <summary>
    /// 啟動期護欄與自修：確保 Managers 根物件與四大 Manager 存在；輸出 GameConfig 摘要；檢查 Building 層與 LayerMask 對齊。
    /// 以 WaitForEndOfFrame 延後執行，避免與既有 BootEnsureManagers 競速。
    /// </summary>
    public class BootAuditor : MonoBehaviour
    {
        private static bool _enabled = true;
        private static bool _autoFix = true;
        private static bool _logDetails = false;

        private static readonly string[] ManagerTypes = new[] {
            "DeepAbyssHive.Creep.Managers.CreepManager",
            "DeepAbyssHive.Units.Managers.UnitManager",
            "DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager",
            "DeepAbyssHive.Terrain.Managers.TerrainManager"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<BootAuditor>() != null) return;
            var go = new GameObject("BootAuditor"); go.AddComponent<BootAuditor>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            DAHLog.Info(LogCategory.SERVICE, "BootAuditor created");
        }

        private IEnumerator Start()
        {
            // 等一幀，讓既有 BootEnsureManagers 先跑完
            yield return new WaitForEndOfFrame();
            TryLoadFromGameConfig();
            if (!_enabled) { DAHLog.Info(LogCategory.SERVICE, "BootAudit disabled via GameConfig"); yield break; }

            var managers = EnsureManagersRoot();
            EnsureCoreManagers(managers);
            LogGameConfigSummary();
            CheckBuildingLayerAndMasks();
        }

        private GameObject EnsureManagersRoot()
        {
            var managers = GameObject.Find("Managers");
            if (managers == null && _autoFix)
            {
                managers = new GameObject("Managers");
                DontDestroyOnLoad(managers);
                DAHLog.Info(LogCategory.SERVICE, "Created 'Managers' root (autofix)");
            }
            else if (managers != null)
            {
                DAHLog.Info(LogCategory.SERVICE, "Managers root present");
                DontDestroyOnLoad(managers);
            }
            return managers;
        }

        private void EnsureCoreManagers(GameObject managers)
        {
            foreach (var qn in ManagerTypes)
            {
                var t = Type.GetType(qn);
                if (t == null) { if (_logDetails) DAHLog.Info(LogCategory.SERVICE, "Type not found: " + qn); continue; }
                var existing = FindObjectOfType(t);
                if (existing == null && _autoFix && managers != null)
                {
                    managers.AddComponent(t);
                    DAHLog.Info(LogCategory.SERVICE, "Added manager: " + t.Name);
                }
                else
                {
                    DAHLog.Info(LogCategory.SERVICE, "Manager present: " + t.Name);
                }
            }
        }

        private void LogGameConfigSummary()
        {
            try
            {
                object cfg = null; Type pt = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    pt = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (pt != null) break;
                }
                if (pt != null) cfg = pt.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (cfg == null) { DAHLog.Info(LogCategory.CONFIG, "GameConfig: <null> (provider not found or not initialized)"); return; }

                // 收集常用鍵的摘要輸出（存在才印）
                var t = cfg.GetType();
                string[] keys = { "useSpatialIndexForPlacement","minSpacing","margin","requireCreep","snapSize","rotationStepDegrees",
                    "buildPlacerToggleKey","buildingDeleteKey1","buildingDeleteKey2","devUnitsSpawnKey","devUnitsTestKey","devSpawnCount",
                    "rmbLocksCursor","healthLogEnabled","healthLogInterval","devVerboseLogs"};
                System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                sb.Append("GameConfig: ");
                bool first = true;
                foreach (var k in keys)
                {
                    object v = GetMemberValue(t, cfg, k);
                    if (v == null) continue;
                    if (!first) sb.Append(", "); first = false;
                    sb.Append(k).Append("=").Append(v);
                }
                DAHLog.Info(LogCategory.CONFIG, sb.ToString());
            }
            catch (Exception ex)
            {
                DAHLog.Info(LogCategory.CONFIG, "GameConfig summary failed: " + ex.Message);
            }
        }

        private void CheckBuildingLayerAndMasks()
        {
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer < 0)
            {
                DAHLog.Warning(LogCategory.COMMON, "Layer 'Building' MISSING — raycast delete may fail; please add layer.");
            }
            else
            {
                DAHLog.Info(LogCategory.COMMON, "Layer 'Building' present (id=" + buildingLayer + ")");
            }

            // 若 GameConfig 有放置/刪除的 LayerMask，檢查其中是否包含 Building
            try
            {
                object cfg = null; Type pt = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    pt = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (pt != null) break;
                }
                if (pt == null) return;
                cfg = pt.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (cfg == null) return;
                var t = cfg.GetType();
                int delMask = GetInt(t, cfg, "deleteRayMask", -1);
                int placeMask = GetInt(t, cfg, "placementRayMask", -1);
                if (delMask >= 0)
                {
                    bool ok = (delMask & (1 << buildingLayer)) != 0;
                    DAHLog.Info(LogCategory.COMMON, $"deleteRayMask has Building={ok} (mask={delMask})");
                }
                if (placeMask >= 0)
                {
                    bool ok = (placeMask & (1 << buildingLayer)) != 0;
                    DAHLog.Info(LogCategory.COMMON, $"placementRayMask has Building={ok} (mask={placeMask})");
                }
            } catch {}
        }

        private static object GetMemberValue(Type t, object inst, string name)
        {
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return f.GetValue(inst);
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return p.GetValue(inst);
            return null;
        }
        private static int GetInt(Type t, object inst, string name, int defVal)
        {
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return Convert.ToInt32(f.GetValue(inst));
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return Convert.ToInt32(p.GetValue(inst));
            return defVal;
        }

        private void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    var t = cfg.GetType();
                    _enabled = GetBool(t, cfg, "bootAuditEnabled", true);
                    _autoFix = GetBool(t, cfg, "bootAutofixManagers", true);
                    _logDetails = GetBool(t, cfg, "bootLogDetails", false);
                    break;
                }
            } catch {}
        }
        private static bool GetBool(Type t, object inst, string name, bool defVal)
        {
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(inst);
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(inst);
            return defVal;
        }

        private static UnityEngine.Object FindObjectOfType(Type t)
        {
            var all = GameObject.FindObjectsOfType<MonoBehaviour>(true);
            foreach (var mb in all) if (t.IsAssignableFrom(mb.GetType())) return mb;
            return null;
        }
    }
}