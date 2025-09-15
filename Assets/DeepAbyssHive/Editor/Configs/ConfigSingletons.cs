#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.EditorTools.Configs
{
    /// <summary>
    /// 提供「Create or Select」單一來源的 ScriptableObject 工廠，避免產生多份 GameConfig 等單例資產。
    /// 另附「Find Duplicates」稽核，幫你找出不小心留下的副本。
    /// </summary>
    public static class ConfigSingletons
    {
        private const string TARGET_DIR = "Assets/Resources/Configs";

        // 單例型別清單（短名 → 固定檔名）
        private static readonly (string shortType, string fileName)[] Singletons = new[]
        {
            ("GameConfigSO",       "GameConfig.asset"),
            ("TerrainConfigSO",    "TerrainConfig.asset"),
            ("CreepConfigSO",      "CreepConfig.asset"),
            ("BuildingCatalogSO",  "BuildingCatalog.asset"),
            ("DevLogSettingsSO",   "DevLogSettings.asset"),
            ("UnitConfigSO",       "UnitConfig.asset"),
        };

        #region Public Menus
        [MenuItem("DeepAbyssHive/Configs/Create or Select/GameConfig")]
        public static void CreateOrSelect_GameConfig() => CreateOrSelect("GameConfigSO", "GameConfig.asset");
        [MenuItem("DeepAbyssHive/Configs/Create or Select/TerrainConfig")]
        public static void CreateOrSelect_TerrainConfig() => CreateOrSelect("TerrainConfigSO", "TerrainConfig.asset");
        [MenuItem("DeepAbyssHive/Configs/Create or Select/CreepConfig")]
        public static void CreateOrSelect_CreepConfig() => CreateOrSelect("CreepConfigSO", "CreepConfig.asset");
        [MenuItem("DeepAbyssHive/Configs/Create or Select/BuildingCatalog")]
        public static void CreateOrSelect_BuildingCatalog() => CreateOrSelect("BuildingCatalogSO", "BuildingCatalog.asset");
        [MenuItem("DeepAbyssHive/Configs/Create or Select/DevLogSettings")]
        public static void CreateOrSelect_DevLogSettings() => CreateOrSelect("DevLogSettingsSO", "DevLogSettings.asset");
        [MenuItem("DeepAbyssHive/Configs/Create or Select/UnitConfig")]
        public static void CreateOrSelect_UnitConfig() => CreateOrSelect("UnitConfigSO", "UnitConfig.asset");

        [MenuItem("DeepAbyssHive/Tools/Configs/Find Duplicates (Singletons)")]
        public static void FindDuplicates() => AuditDuplicates();
        #endregion

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name   = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateOrSelect(string shortType, string fileName)
        {
            var type = FindRuntimeType(shortType);
            if (type == null)
            {
                EditorUtility.DisplayDialog("Config", $"找不到型別：{shortType}\n請確認腳本存在且名稱正確。", "OK");
                return;
            }

            EnsureFolder(TARGET_DIR);

            // 先從正確目錄找
            var canonicalPath = $"{TARGET_DIR}/{fileName}";
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(canonicalPath);
            if (asset == null)
            {
                // 沒有 → 檢查是否有同型別其他檔案（任意路徑）
                var guids = AssetDatabase.FindAssets($"t:{type.Name}");
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (obj != null && obj.GetType() == type)
                    {
                        // 有其他副本 → 提示並選擇將此副本搬為主檔 or 建立新主檔
                        int choice = EditorUtility.DisplayDialogComplex("Duplicate Found",
                            $"偵測到已存在 {type.Name}：\n{path}\n\n你要把它搬到 {canonicalPath} 並作為主檔嗎？",
                            "搬過去並設為主檔", "建立新主檔", "取消");
                        if (choice == 0)
                        {
                            EnsureFolder(TARGET_DIR);
                            var err = AssetDatabase.MoveAsset(path, canonicalPath);
                            if (!string.IsNullOrEmpty(err))
                                EditorUtility.DisplayDialog("Move Failed", err, "OK");
                            asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(canonicalPath);
                        }
                        else if (choice == 1)
                        {
                            // 建立新主檔
                            asset = CreateNewAsset(type, canonicalPath);
                        }
                        else return;

                        break;
                    }
                }

                // 完全沒找到 → 直接建立
                if (asset == null)
                    asset = CreateNewAsset(type, canonicalPath);
            }

            // 選取並聚焦
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static UnityEngine.Object CreateNewAsset(Type type, string path)
        {
            var inst = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(inst, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CONFIGS] Created {type.Name} at {path}");
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

        private static void AuditDuplicates()
        {
            var report = new System.Text.StringBuilder();
            int dupCount = 0;

            foreach (var (shortType, fileName) in Singletons)
            {
                var type = FindRuntimeType(shortType);
                if (type == null) continue;

                var guids = AssetDatabase.FindAssets($"t:{type.Name}");
                var list = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
                if (list.Count > 1)
                {
                    dupCount += list.Count - 1;
                    report.AppendLine($"[{shortType}] found {list.Count}:");
                    foreach (var p in list) report.AppendLine("  • " + p);
                    report.AppendLine($"  → 建議保留：{TARGET_DIR}/{fileName}\n");
                }
            }

            if (dupCount == 0)
                EditorUtility.DisplayDialog("Duplicates", "未發現單例 Config 的重覆資產。", "OK");
            else
            {
                var txt = report.ToString();
                Debug.LogWarning("[CONFIGS] Duplicates found:\n" + txt);
                EditorUtility.DisplayDialog("Duplicates", $"發現 {dupCount} 個重覆項，詳見 Console。", "OK");
            }
        }

        private static Type FindRuntimeType(string shortName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(tp => tp.Name == shortName && typeof(ScriptableObject).IsAssignableFrom(tp));
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }
    }
}
#endif