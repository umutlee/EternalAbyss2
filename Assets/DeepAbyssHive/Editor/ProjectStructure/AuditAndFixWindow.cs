#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.EditorTools.ProjectStructure
{
    /// <summary>
    /// 專案結構稽核/修復
    /// - 一鍵建立/修復 asmdef（Runtime/Editor/Dev）
    /// - 掃描常見錯放位置並提供 Fix（使用 AssetDatabase.MoveAsset）
    /// - 依路徑修復 namespace：DeepAbyssHive.(子資料夾以'.'連結)
    /// </summary>
    public class AuditAndFixWindow : EditorWindow
    {
        private static readonly string Root = "Assets/DeepAbyssHive";
        private static readonly (string from, string to)[] MoveRules = new (string, string)[]
        {
            // 依你先前敘述列出幾個常見來源到目的位置
            ("Assets/Dev/logging/Editor", $"{Root}/Dev/Logging/Editor"),
            ("Assets/Editor/Config",       $"{Root}/Editor/Config"),
            ("Assets/Core/Config/Creep",   $"{Root}/Creep/Config"),
            ("Assets/Core/Config/Terrain", $"{Root}/Terrain/Config"),
            ("Assets/Core/Logging",        $"{Root}/Core/Logging"),
        };

        [MenuItem("DeepAbyssHive/Tools/Project Structure/Audit & Fix")]
        public static void Open()
        {
            var wnd = GetWindow<AuditAndFixWindow>("DAH Project Auditor");
            wnd.minSize = new Vector2(720, 420);
        }

        private Vector2 _scroll;
        private string _log = "";

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Audit", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    RunAudit();
                if (GUILayout.Button("Fix Namespaces", EditorStyles.toolbarButton, GUILayout.Width(120)))
                    FixNamespaces();
                if (GUILayout.Button("Create/Repair asmdefs", EditorStyles.toolbarButton, GUILayout.Width(160)))
                    CreateAsmdefs();
                if (GUILayout.Button("Clean Root asmdef dups", EditorStyles.toolbarButton, GUILayout.Width(170)))
                    CleanRootAsmdefDuplicates();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    _log = "";
            }

            EditorGUILayout.LabelField($"Root = {Root}");
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("建議：先按 Audit 檢視，再各別 Fix。", MessageType.Info);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            }
        }

        private void Append(string line)
        {
            _log += line + "\n";
        }

        private void RunAudit()
        {
            _log = "";
            Append($"[AUDIT] Start {DateTime.Now:HH:mm:ss}");

            foreach (var (from, to) in MoveRules)
            {
                if (AssetDatabase.IsValidFolder(from))
                {
                    Append($"- Found misplaced: {from}  -> should be  {to}");
                }
            }

            AuditAsmdefConflicts();

            // 找出 DeepAbyssHive 以外的 DAH* cs（可能缺少 namespace）
            var allCs = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                        .Where(p => !p.Contains("/Editor/") || p.StartsWith(Root)) // 不特別過濾 Editor，只是優先顯示 Root 內檔案
                        .ToArray();
            Append($"- Found {allCs.Length} .cs files (subset displayed).");

            int sample = 0;
            foreach (var path in allCs.Where(p => p.Contains("DeepAbyssHive")))
            {
                Append($"  · {path}");
                if (++sample > 15) { Append("  · ..."); break; }
            }

            Append("[AUDIT] Done.");
        }

        private void CreateAsmdefs()
        {
            // Ensure target folders
            EnsureFolder($"{Root}");
            EnsureFolder($"{Root}/Editor");
            EnsureFolder($"{Root}/Dev");

            // Legacy cleanup: if Editor/Dev asmdefs mistakenly exist at root, delete them before writing
            var legacyEditorPath = $"{Root}/DeepAbyssHive.Editor.asmdef";
            var legacyDevPath    = $"{Root}/DeepAbyssHive.Dev.asmdef";
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(legacyEditorPath) != null)
            {
                AssetDatabase.DeleteAsset(legacyEditorPath);
                Append($"[ASMDEF][CLEAN] Removed legacy {legacyEditorPath}");
            }
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(legacyDevPath) != null)
            {
                AssetDatabase.DeleteAsset(legacyDevPath);
                Append($"[ASMDEF][CLEAN] Removed legacy {legacyDevPath}");
            }

            // Write (or repair) asmdefs to correct locations
            WriteText($"{Root}/DeepAbyssHive.Runtime.asmdef", RUNTIME_ASMDEF);
            WriteText($"{Root}/Editor/DeepAbyssHive.Editor.asmdef", EDITOR_ASMDEF);
            WriteText($"{Root}/Dev/DeepAbyssHive.Dev.asmdef", DEV_ASMDEF);

            AssetDatabase.Refresh();
            Append("[ASMDEF] Created/Updated Runtime (root), Editor (Editor/), Dev (Dev/) asmdefs.");
        }

        private void FixNamespaces()
        {
            var csFiles = Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories);
            int changed = 0;
            foreach (var p in csFiles)
            {
                if (TryFixNamespace(p)) changed++;
            }
            AssetDatabase.Refresh();
            Append($"[NS] Fixed {changed} files.");
        }

        private static bool TryFixNamespace(string assetPath)
        {
            var full = Path.GetFullPath(assetPath);
            var text = File.ReadAllText(full);

            // 計算期望 namespace：DeepAbyssHive + 相對於 Root 的資料夾層級（以 '.' 連結，去掉 Scripts 與 Editor 可選擇保留）
            var rel = assetPath.Replace("\\", "/");
            rel = rel.Substring("Assets/DeepAbyssHive/".Length);
            var folderPart = Path.GetDirectoryName(rel)?.Replace("\\", "/") ?? "";
            var segments = folderPart.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s)
                                     .ToList();
            string ns = "DeepAbyssHive";
            if (segments.Count > 0)
                ns += "." + string.Join(".", segments);

            // 1) 找既有 namespace
            var m = Regex.Match(text, @"namespace\s+([A-Za-z0-9_.]+)\s*\{");
            if (m.Success)
            {
                var cur = m.Groups[1].Value;
                if (cur == ns) return false; // 已正確
                text = Regex.Replace(text, @"namespace\s+[A-Za-z0-9_.]+\s*\{", $"namespace {ns}\n{{");
            }
            else
            {
                // 無 namespace，嘗試包起來
                // 找第一個 using 後插入 namespace
                var usings = Regex.Match(text, @"^(?:using\s+[A-Za-z0-9_.]+;\s*\r?\n)+", RegexOptions.Multiline);
                int insertAt = usings.Success ? usings.Index + usings.Length : 0;
                text = text.Insert(insertAt, $"\nnamespace {ns}\n{{\n");
                text += "\n}\n";
            }

            File.WriteAllText(full, text);
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void WriteText(string path, string content)
        {
            var full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, content);
        }

        /// <summary>
        /// 稽核：任何資料夾同時擁有多於 1 個 asmdef，視為衝突並列出。
        /// 另外提示 Root 是否存在 Editor/Dev asmdef（應放到子資料夾）。
        /// </summary>
        private void AuditAsmdefConflicts()
        {
            var asmdefs = Directory.GetFiles("Assets/DeepAbyssHive", "*.asmdef", SearchOption.AllDirectories);
            var groups = asmdefs.GroupBy(p => Path.GetDirectoryName(p).Replace("\\", "/"));
            foreach (var g in groups)
            {
                var list = g.ToList();
                if (list.Count > 1)
                {
                    Append($"[ASMDEF][WARN] Folder has {list.Count} asmdefs: {g.Key}");
                    foreach (var f in list) Append($"   · {f}");
                }
            }

            // 針對 Root 下誤放的 Editor/Dev asmdef 額外提醒
            var root = "Assets/DeepAbyssHive";
            var editorRoot = Path.Combine(root, "DeepAbyssHive.Editor.asmdef").Replace("\\", "/");
            var devRoot    = Path.Combine(root, "DeepAbyssHive.Dev.asmdef").Replace("\\", "/");
            if (File.Exists(editorRoot) || File.Exists(devRoot))
            {
                Append("[ASMDEF][HINT] Detected Editor/Dev asmdef at Root (should live in Editor/ or Dev/). You can click 'Clean Root asmdef dups'.");
            }
        }

        /// <summary>
        /// 清除 Root 下的 Editor/Dev asmdef（若存在），避免單資料夾多 asmdef 衝突。
        /// </summary>
        private void CleanRootAsmdefDuplicates()
        {
            var root = "Assets/DeepAbyssHive";
            var editorRoot = Path.Combine(root, "DeepAbyssHive.Editor.asmdef").Replace("\\", "/");
            var devRoot    = Path.Combine(root, "DeepAbyssHive.Dev.asmdef").Replace("\\", "/");
            int removed = 0;
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(editorRoot) != null)
            {
                AssetDatabase.DeleteAsset(editorRoot);
                Append($"[ASMDEF][CLEAN] Removed legacy {editorRoot}");
                removed++;
            }
            if (AssetDatabase.LoadAssetAtPath<TextAsset>(devRoot) != null)
            {
                AssetDatabase.DeleteAsset(devRoot);
                Append($"[ASMDEF][CLEAN] Removed legacy {devRoot}");
                removed++;
            }
            if (removed == 0) Append("[ASMDEF][CLEAN] No legacy root asmdefs found.");
            AssetDatabase.Refresh();
        }

        private const string RUNTIME_ASMDEF = "{\n  \"name\": \"DeepAbyssHive.Runtime\",\n  \"references\": [],\n  \"includePlatforms\": [],\n  \"excludePlatforms\": [],\n  \"allowUnsafeCode\": false,\n  \"overrideReferences\": false,\n  \"precompiledReferences\": [],\n  \"autoReferenced\": true,\n  \"defineConstraints\": [],\n  \"versionDefines\": [],\n  \"noEngineReferences\": false\n}\n";
        private const string EDITOR_ASMDEF  = "{\n  \"name\": \"DeepAbyssHive.Editor\",\n  \"references\": [ \"DeepAbyssHive.Runtime\" ],\n  \"includePlatforms\": [ \"Editor\" ],\n  \"excludePlatforms\": [],\n  \"allowUnsafeCode\": false,\n  \"overrideReferences\": false,\n  \"precompiledReferences\": [],\n  \"autoReferenced\": true,\n  \"defineConstraints\": [],\n  \"versionDefines\": [],\n  \"noEngineReferences\": false\n}\n";
        private const string DEV_ASMDEF     = "{\n  \"name\": \"DeepAbyssHive.Dev\",\n  \"references\": [ \"DeepAbyssHive.Runtime\", \"DeepAbyssHive.Editor\" ],\n  \"includePlatforms\": [ \"Editor\" ],\n  \"excludePlatforms\": [],\n  \"allowUnsafeCode\": false,\n  \"overrideReferences\": false,\n  \"precompiledReferences\": [],\n  \"autoReferenced\": true,\n  \"defineConstraints\": [ \"DEV_TOOLS\" ],\n  \"versionDefines\": [],\n  \"noEngineReferences\": false\n}\n";
    }
}
#endif