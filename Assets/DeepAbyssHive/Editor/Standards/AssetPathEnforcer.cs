#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Constants;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.EditorTools.Standards
{
    /// <summary>
    /// 資產儲存位置標準化（Config SO → Resources/Configs；Template → Units/Templates）
    /// - 提供「掃描並搬移」交互（列出清單 → 確認 → MoveAsset）
    /// - 可選 Auto-Enforce：在新資產導入時自動搬移（EditorPrefs: DAH.AutoEnforceOnImport）
    /// </summary>
    public class AssetPathEnforcer : AssetPostprocessor
    {
        private static bool Auto => EditorPrefs.GetBool("DAH.AutoEnforceOnImport", false);

        public static (int moved, int skipped) ScanAndMoveInteractive()
        {
            Ensure(AssetPaths.ConfigsFolder);
            Ensure(AssetPaths.TemplatesFolder);

            var mis = FindMisplacedAssets();
            if (mis.Count == 0)
            {
                EditorUtility.DisplayDialog("Asset Paths", "所有已在正確位置，無需搬移。", "OK");
                return (0, 0);
            }

            string msg = "以下資產將被搬移到標準資料夾：\n" + string.Join("\n", mis.Select(m => $" - {m.path}  →  {m.destFolder}"));
            if (!EditorUtility.DisplayDialog("確認搬移", msg, "搬移", "取消"))
                return (0, mis.Count);

            int moved = 0, skipped = 0;
            foreach (var m in mis)
            {
                var name = Path.GetFileName(m.path);
                var destPath = AssetDatabase.GenerateUniqueAssetPath(m.destFolder + "/" + name);
                string err = AssetDatabase.MoveAsset(m.path, destPath);
                if (string.IsNullOrEmpty(err))
                {
                    moved++;
                    Log($"Moved: {m.path} -> {destPath}");
                }
                else
                {
                    skipped++;
                    Log($"Skip (error): {m.path}  ({err})");
                }
            }
            AssetDatabase.Refresh();
            return (moved, skipped);
        }

        private static List<(string path,string destFolder)> FindMisplacedAssets()
        {
            var list = new List<(string, string)>();
            foreach (var gid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                string path = AssetDatabase.GUIDToAssetPath(gid);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj == null) continue;

                string type = obj.GetType().Name;
                bool isConfig = type.EndsWith("ConfigSO") || type == "BuildingCatalogSO";
                bool isTemplate = type.EndsWith("TemplateSO") && !isConfig;

                if (isConfig)
                {
                    if (!path.Replace("\\","/").StartsWith(AssetPaths.ConfigsFolder + "/"))
                        list.Add((path, AssetPaths.ConfigsFolder));
                }
                else if (isTemplate)
                {
                    if (!path.Replace("\\","/").StartsWith(AssetPaths.TemplatesFolder + "/"))
                        list.Add((path, AssetPaths.TemplatesFolder));
                }
            }
            return list;
        }

        private static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static void Log(string msg)
        {
            try { DAHLog.Info(LogCategory.CONFIG, "[AssetPathEnforcer] " + msg); }
            catch { Debug.Log("[AssetPathEnforcer] " + msg); }
        }

        // Auto-Enforce：當新資產被導入時，檢查剛建立的 SO/Template 是否在正確位置，否則搬移
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!Auto) return;
            Ensure(AssetPaths.ConfigsFolder);
            Ensure(AssetPaths.TemplatesFolder);

            foreach (var p in imported)
            {
                if (!p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p);
                if (obj == null) continue;

                string type = obj.GetType().Name;
                bool isConfig = type.EndsWith("ConfigSO") || type == "BuildingCatalogSO";
                bool isTemplate = type.EndsWith("TemplateSO") && !isConfig;

                string norm = p.Replace("\\","/");
                if (isConfig && !norm.StartsWith(AssetPaths.ConfigsFolder + "/"))
                {
                    var name = Path.GetFileName(p);
                    var dest = AssetDatabase.GenerateUniqueAssetPath(AssetPaths.ConfigsFolder + "/" + name);
                    var err = AssetDatabase.MoveAsset(p, dest);
                    Log(string.IsNullOrEmpty(err) ? $"Auto-moved Config: {p} -> {dest}" : $"Auto-move failed: {p} ({err})");
                }
                else if (isTemplate && !norm.StartsWith(AssetPaths.TemplatesFolder + "/"))
                {
                    var name = Path.GetFileName(p);
                    var dest = AssetDatabase.GenerateUniqueAssetPath(AssetPaths.TemplatesFolder + "/" + name);
                    var err = AssetDatabase.MoveAsset(p, dest);
                    Log(string.IsNullOrEmpty(err) ? $"Auto-moved Template: {p} -> {dest}" : $"Auto-move failed: {p} ({err})");
                }
            }
        }

        public static void SetAuto(bool enabled) => EditorPrefs.SetBool("DAH.AutoEnforceOnImport", enabled);
    }
}
#endif