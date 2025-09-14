#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.EditorTools.Standards
{
    /// <summary>
    /// UnitTemplate → UnitTemplateSO 遷移工具（含重綁與清理）
    /// </summary>
    public static class UnitTemplateMigrator
    {
        private const string RootMenu = "DeepAbyssHive/Tools/Units/";
        private const string MapFileName = "UnitTemplateMigrationMap.json";

        [MenuItem(RootMenu + "Migrate Assets — UnitTemplate → UnitTemplateSO", priority = 0)]
        public static void MigrateAssets()
        {
            var guids = AssetDatabase.FindAssets("t:UnitTemplate");
            if (guids == null || guids.Length == 0)
            {
                Log("No UnitTemplate assets found.");
                return;
            }

            int ok = 0, fail = 0;
            var map = new List<MapItem>();
            var dstType = FindType("UnitTemplateSO");
            if (dstType == null) { EditorUtility.DisplayDialog("Migration", "找不到 UnitTemplateSO 類型。", "OK"); return; }

            foreach (var gid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(gid);
                var src = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (src == null) { fail++; continue; }

                string dir = Path.GetDirectoryName(path).Replace("\\", "/");
                string name = Path.GetFileNameWithoutExtension(path);
                string dstPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{name}_SO.asset");

                var dst = ScriptableObject.CreateInstance(dstType);
                string json = EditorJsonUtility.ToJson(src);
                EditorJsonUtility.FromJsonOverwrite(json, dst);
                AssetDatabase.CreateAsset(dst, dstPath);
                AssetDatabase.SaveAssets();

                string newGuid = AssetDatabase.AssetPathToGUID(dstPath);
                map.Add(new MapItem { oldGuid = gid, newGuid = newGuid, oldPath = path, newPath = dstPath });
                ok++;
            }

            string mapPath = "Assets/" + MapFileName;
            File.WriteAllText(mapPath, JsonUtility.ToJson(new Map { items = map.ToArray() }, true));
            AssetDatabase.ImportAsset(mapPath);
            Log($"Migration done. created={ok}, failed={fail}. Map={mapPath}");
            EditorUtility.DisplayDialog("Migration", $"完成：建立 {ok} 筆，失敗/跳過 {fail} 筆。\n對照表：{mapPath}", "OK");
        }

        [MenuItem(RootMenu + "Rebind References — (After code uses UnitTemplateSO)", priority = 1)]
        public static void RebindReferences()
        {
            string mapPath = "Assets/" + MapFileName;
            if (!File.Exists(mapPath)) { EditorUtility.DisplayDialog("Rebind", "缺少映射檔。先執行遷移。", "OK"); return; }
            var map = JsonUtility.FromJson<Map>(File.ReadAllText(mapPath));
            if (map.items == null || map.items.Length == 0) { EditorUtility.DisplayDialog("Rebind", "映射檔為空。", "OK"); return; }

            var allGuids = AssetDatabase.FindAssets("");
            int touched = 0, properties = 0;

            foreach (var gid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(gid);
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

                var obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj == null) continue;

                var so = new SerializedObject(obj);
                var sp = so.GetIterator();
                bool dirty = false;
                bool enterChildren = true;
                while (sp.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (sp.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var oldObj = sp.objectReferenceValue;
                    if (oldObj == null) continue;

                    string oldGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(oldObj));
                    var mapItem = map.items.FirstOrDefault(m => m.oldGuid == oldGuid);
                    if (!string.IsNullOrEmpty(mapItem.oldGuid))
                    {
                        var newObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(mapItem.newPath);
                        sp.objectReferenceValue = newObj;
                        dirty = true;
                        properties++;
                    }
                }
                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(obj);
                    touched++;
                }
            }

            AssetDatabase.SaveAssets();
            Log($"Rebind done. assetsTouched={touched}, propertiesUpdated={properties}");
            EditorUtility.DisplayDialog("Rebind", $"完成：{touched} 資產更新、{properties} 個引用重綁。", "OK");
        }

        [MenuItem(RootMenu + "Lint Code — Find 'UnitTemplate' type usages", priority = 2)]
        public static void LintCode()
        {
            string root = Application.dataPath.Replace("\\","/");
            var cs = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                              .Where(p => p.Contains("/Assets/DeepAbyssHive/"))
                              .ToArray();

            var hits = new System.Collections.Generic.List<string>();
            foreach (var f in cs)
            {
                string txt = File.ReadAllText(f);
                if (txt.Contains(" UnitTemplate ") || txt.Contains(": UnitTemplate") || txt.Contains("<UnitTemplate>") || txt.Contains("UnitTemplate[]"))
                    hits.Add(ToAssetPath(f));
            }

            if (hits.Count == 0) { Log("No code references to 'UnitTemplate' found."); EditorUtility.DisplayDialog("Lint Code", "未發現 'UnitTemplate' 類型使用處。", "OK"); }
            else
            {
                var report = string.Join("\n", hits);
                Log($"Found {hits.Count} code files: \n{report}");
                EditorUtility.DisplayDialog("Lint Code", $"共 {hits.Count} 個檔案仍使用 'UnitTemplate'（詳見 Console）。", "OK");
            }
        }

        // 嘗試安全移除 Legacy UnitTemplate.cs（若無資產且無代碼引用）
        [MenuItem(RootMenu + "Remove Legacy UnitTemplate.cs (safe)", priority = 3)]
        public static bool TryRemoveLegacy()
        {
            var guids = AssetDatabase.FindAssets("t:UnitTemplate");
            if (guids.Length > 0)
            {
                EditorUtility.DisplayDialog("Remove", $"尚有 {guids.Length} 個 UnitTemplate 資產，請先遷移。", "OK");
                return false;
            }

            // 搜尋程式碼引用
            string root = Application.dataPath.Replace("\\","/");
            var cs = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                              .Where(p => p.Contains("/Assets/DeepAbyssHive/"))
                              .ToArray();
            foreach (var f in cs)
            {
                string txt = File.ReadAllText(f);
                if (txt.Contains(" UnitTemplate ") || txt.Contains(": UnitTemplate") || txt.Contains("<UnitTemplate>") || txt.Contains("UnitTemplate[]"))
                {
                    EditorUtility.DisplayDialog("Remove", "仍有程式碼使用 UnitTemplate，請先改為 UnitTemplateSO。", "OK");
                    return false;
                }
            }

            // 找檔案
            var unitTemplateFile = cs.FirstOrDefault(p => Path.GetFileName(p).Equals("UnitTemplate.cs", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(unitTemplateFile))
            {
                EditorUtility.DisplayDialog("Remove", "找不到 UnitTemplate.cs。", "OK");
                return false;
            }

            // 安全移動至 _Trash，避免直接刪除
            string assetPath = ToAssetPath(unitTemplateFile);
            string trashFolder = "Assets/_Trash";
            if (!AssetDatabase.IsValidFolder(trashFolder))
                AssetDatabase.CreateFolder("Assets", "_Trash");
            string dest = AssetDatabase.GenerateUniqueAssetPath(trashFolder + "/UnitTemplate_REMOVED.cs.txt");

            if (AssetDatabase.MoveAsset(assetPath, dest) == "")
            {
                Log("Legacy UnitTemplate moved to: " + dest);
                return true;
            }
            else
            {
                EditorUtility.DisplayDialog("Remove", "移動失敗，請手動處理：" + assetPath, "OK");
                return false;
            }
        }

        private static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetTypes().FirstOrDefault(x => x != null && x.Name == name);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static string ToAssetPath(string full)
        {
            string proj = Directory.GetParent(Application.dataPath).FullName.Replace("\\","/");
            return full.Replace("\\","/").Replace(proj + "/", "");
        }

        private static void Log(string msg)
        {
            try { DAHLog.Info(LogCategory.CONFIG, "[UnitTemplateMigrator] " + msg); }
            catch { Debug.Log("[UnitTemplateMigrator] " + msg); }
        }

        [Serializable] private class Map { public MapItem[] items; }
        [Serializable] private class MapItem { public string oldGuid; public string newGuid; public string oldPath; public string newPath; }
    }
}
#endif