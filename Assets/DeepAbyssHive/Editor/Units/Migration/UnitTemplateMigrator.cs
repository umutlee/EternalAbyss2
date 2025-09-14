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
            var guids = AssetDatabase.FindAssets("t:UnitTemplateSO");
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
                if (txt.Contains(" UnitTemplateSO ") || txt.Contains(": UnitTemplateSO") || txt.Contains("<UnitTemplateSO>") || txt.Contains("UnitTemplateSO[]"))
                    hits.Add(ToAssetPath(f));
            }

            if (hits.Count == 0) { Log("No code references to 'UnitTemplateSO' found."); EditorUtility.DisplayDialog("Lint Code", "未發現 'UnitTemplateSO' 類型使用處。", "OK"); }
            else
            {
                var report = string.Join("\n", hits);
                Log($"Found {hits.Count} code files: \n{report}");
                // 打開交互視窗，便於直接處理
                LintResultsWindow.Show(hits.ToArray());
            }
        }

        // 嘗試安全移除 Legacy UnitTemplate.cs（若無資產且無代碼引用）
        [MenuItem(RootMenu + "Remove Legacy UnitTemplate.cs (safe)", priority = 3)]
        public static bool TryRemoveLegacy()
        {
            var guids = AssetDatabase.FindAssets("t:UnitTemplateSO");
            if (guids.Length > 0)
            {
                EditorUtility.DisplayDialog("Remove", $"尚有 {guids.Length} 個 UnitTemplateSO 資產，請先遷移。", "OK");
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
                if (txt.Contains(" UnitTemplateSO ") || txt.Contains(": UnitTemplateSO") || txt.Contains("<UnitTemplateSO>") || txt.Contains("UnitTemplateSO[]"))
                {
                    EditorUtility.DisplayDialog("Remove", "仍有程式碼使用 UnitTemplateSO，請先改為 UnitTemplateSO。", "OK");
                    return false;
                }
            }

            // 找檔案
            var unitTemplateFile = cs.FirstOrDefault(p => Path.GetFileName(p).Equals("UnitTemplateSO.cs", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(unitTemplateFile))
            {
                EditorUtility.DisplayDialog("Remove", "找不到 UnitTemplateSO.cs。", "OK");
                return false;
            }

            // 安全移動至 _Trash，避免直接刪除
            string assetPath = ToAssetPath(unitTemplateFile);
            string trashFolder = "Assets/_Trash";
            if (!AssetDatabase.IsValidFolder(trashFolder))
                AssetDatabase.CreateFolder("Assets", "_Trash");
            string dest = AssetDatabase.GenerateUniqueAssetPath(trashFolder + "/UnitTemplateSO_REMOVED.cs.txt");

            if (AssetDatabase.MoveAsset(assetPath, dest) == "")
            {
                Log("Legacy UnitTemplateSO moved to: " + dest);
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

        /// <summary>
        /// 顯示 Lint 結果並提供安全替換工具
        /// </summary>
        private class LintResultsWindow : EditorWindow
        {
            private string[] _paths;
            private bool[] _selected;
            private Vector2 _scroll;
            private int[] _previewCounts;
            private static readonly System.Text.RegularExpressions.Regex Token = new System.Text.RegularExpressions.Regex(@"\bUnitTemplateSO\b(?!SO)");

            public static void Show(string[] assetPaths)
            {
                var w = GetWindow<LintResultsWindow>("UnitTemplateSO Lint");
                w.minSize = new Vector2(720, 420);
                w._paths = assetPaths;
                w._selected = Enumerable.Repeat(true, assetPaths.Length).ToArray();
                w._previewCounts = assetPaths.Select(p => CountInFile(ToFullPath(p))).ToArray();
                w.Show();
            }

            private static int CountInFile(string full)
            {
                try { return Token.Matches(File.ReadAllText(full)).Count; } catch { return 0; }
            }

            private static string ToFullPath(string assetPath)
            {
                string proj = Directory.GetParent(Application.dataPath).FullName.Replace("\\","/");
                return Path.Combine(proj, assetPath).Replace("\\","/");
            }

            private void OnGUI()
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("Select All", EditorStyles.toolbarButton, GUILayout.Width(90))) { for (int i=0;i<_selected.Length;i++) _selected[i]=true; }
                    if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(70))) { for (int i=0;i<_selected.Length;i++) _selected[i]=false; }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy Paths", EditorStyles.toolbarButton, GUILayout.Width(90))) GUIUtility.systemCopyBuffer = string.Join("\n", _paths);
                    if (GUILayout.Button("Open Selected", EditorStyles.toolbarButton, GUILayout.Width(110))) OpenSelected();
                }

                EditorGUILayout.HelpBox("建議先將程式欄位型別改為 UnitTemplateSO，再使用 Replace 取代純型別名稱（不會影響 UnitTemplateSO）。每檔會先建立 .bak 備份。", MessageType.Info);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                for (int i = 0; i < _paths.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _selected[i] = EditorGUILayout.Toggle(_selected[i], GUILayout.Width(20));
                        if (GUILayout.Button(_paths[i], EditorStyles.linkLabel))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_paths[i]);
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"matches: {_previewCounts[i]}", GUILayout.Width(100));
                    }
                }
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUI.enabled = _selected.Any(b => b);
                    if (GUILayout.Button("Replace in Selected (preview counts shown) — 確認後執行", GUILayout.Height(28)))
                    {
                        int sel = _selected.Count(b=>b);
                        if (!EditorUtility.DisplayDialog("Confirm Replace", $"即將在 {sel} 個檔案中用規則：\\bUnitTemplateSO\\b(?!SO) 進行取代。\n每檔會先產生 .bak 備份。\n不可逆動作，是否繼續？", "Replace", "Cancel"))
                            return;
                        int files=0, reps=0;
                        for (int i=0;i<_paths.Length;i++)
                        {
                            if (!_selected[i]) continue;
                            string full = ToFullPath(_paths[i]);
                            try
                            {
                                string src = File.ReadAllText(full);
                                int before = Token.Matches(src).Count;
                                if (before == 0) continue;
                                File.Copy(full, full + ".bak", overwrite: true);
                                string dst = Token.Replace(src, "UnitTemplateSO");
                                File.WriteAllText(full, dst);
                                files++;
                                reps += before;
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning("[UnitTemplateSO Lint] Replace failed: " + _paths[i] + "  " + e.Message);
                            }
                        }
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Replace Done", $"已處理檔案：{files}，替換總數：{reps}\n備份 .bak 已建立。", "OK");
                        // 更新預覽數
                        _previewCounts = _paths.Select(p => CountInFile(ToFullPath(p))).ToArray();
                    }
                    GUI.enabled = true;
                }
            }

            private void OpenSelected()
            {
                foreach (var p in _paths.Where((_,i)=>_selected[i]))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                    AssetDatabase.OpenAsset(obj);
                }
            }
        }
    }
}
#endif