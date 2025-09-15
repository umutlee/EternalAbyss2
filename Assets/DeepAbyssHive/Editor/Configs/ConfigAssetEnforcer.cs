#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.EditorTools.Configs
{
    /// <summary>
    /// 監看配置 ScriptableObject 的建立/移動：若不在 Assets/Resources/Configs 就自動搬移。
    /// 另提供手動「Audit & Fix」一鍵整理既有資產。
    /// </summary>
    public sealed class ConfigAssetEnforcer : AssetPostprocessor
    {
        private const string TargetDir = "Assets/Resources/Configs";
        private const string PrefKey   = "DAH.ConfigEnforcer.Enabled";

        private static bool Enabled => EditorPrefs.GetBool(PrefKey, true);

        [InitializeOnLoadMethod]
        private static void Boot()
        {
            EnsureFolder(TargetDir);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name   = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        // 自動執行：新建 / 匯入 / 移動後被呼叫
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                                           string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!Enabled) return;

            Process(importedAssets);
            Process(movedAssets);
        }

        private static void Process(string[] paths)
        {
            if (paths == null) return;
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p) || !p.EndsWith(".asset")) continue;
                if (p.StartsWith(TargetDir)) continue;

                var obj = AssetDatabase.LoadMainAssetAtPath(p);
                if (obj == null || !(obj is ScriptableObject)) continue;
                if (!IsConfigSO(obj)) continue;

                EnsureFolder(TargetDir);
                var to = AssetDatabase.GenerateUniqueAssetPath((TargetDir + "/" + Path.GetFileName(p)).Replace("\\", "/"));
                var err = AssetDatabase.MoveAsset(p, to);
                if (!string.IsNullOrEmpty(err))
                    Debug.LogWarning($"[CONFIGS] Move failed: {p} → {to}\n{err}");
                else
                    Debug.Log($"[CONFIGS] Moved {obj.GetType().Name} → {to}");
            }
        }

        private static bool IsConfigSO(Object obj)
        {
            var t   = obj.GetType();
            var ns  = t.Namespace ?? string.Empty;
            var n   = t.Name;

            if (n.EndsWith("ConfigSO") || n.EndsWith("CatalogSO") || n.EndsWith("SettingsSO"))
                return true;
            if (ns.Contains(".Config") || ns.EndsWith(".Configs"))
                return true;
            if (n.Contains("GameConfig") || n.Contains("TerrainConfig") || n.Contains("CreepConfig") ||
                n.Contains("BuildingConfig") || n.Contains("UnitConfig") || n.Contains("DevLogSettings"))
                return true;
            return false;
        }

        // 手動整理：掃描整個專案把不在 Resources/Configs 的配置 SO 都搬過去
        [MenuItem("DeepAbyssHive/Tools/Configs/Enforce Location (Audit & Fix)")]
        public static void EnforceNow()
        {
            EnsureFolder(TargetDir);
            var all = AssetDatabase.FindAssets("t:ScriptableObject")
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .Where(p => p.StartsWith("Assets/"))
                                   .ToArray();

            int moved = 0, skipped = 0;
            foreach (var p in all)
            {
                if (p.StartsWith(TargetDir)) { skipped++; continue; }
                var obj = AssetDatabase.LoadMainAssetAtPath(p);
                if (obj == null || !(obj is ScriptableObject) || !IsConfigSO(obj)) { skipped++; continue; }

                var to = AssetDatabase.GenerateUniqueAssetPath((TargetDir + "/" + Path.GetFileName(p)).Replace("\\", "/"));
                var err = AssetDatabase.MoveAsset(p, to);
                if (string.IsNullOrEmpty(err)) moved++;
                else Debug.LogWarning($"[CONFIGS] Move failed: {p} → {to}\n{err}");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("DAH Configs", $"Moved: {moved}\nSkipped: {skipped}\nTarget: {TargetDir}", "OK");
        }

        // 切換自動強制
        [MenuItem("DeepAbyssHive/Tools/Configs/Auto Enforce/Enable", true)]
        private static bool ValidateEnable() => !Enabled;
        [MenuItem("DeepAbyssHive/Tools/Configs/Auto Enforce/Enable")]
        private static void EnableAuto()
        {
            EditorPrefs.SetBool(PrefKey, true);
            EditorUtility.DisplayDialog("DAH Configs", "Auto enforce ENABLED", "OK");
        }

        [MenuItem("DeepAbyssHive/Tools/Configs/Auto Enforce/Disable", true)]
        private static bool ValidateDisable() => Enabled;
        [MenuItem("DeepAbyssHive/Tools/Configs/Auto Enforce/Disable")]
        private static void DisableAuto()
        {
            EditorPrefs.SetBool(PrefKey, false);
            EditorUtility.DisplayDialog("DAH Configs", "Auto enforce DISABLED", "OK");
        }
    }
}
#endif