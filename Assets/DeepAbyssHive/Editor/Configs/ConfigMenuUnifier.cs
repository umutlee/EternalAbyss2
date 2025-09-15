#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.EditorTools.Configs
{
    /// <summary>
    /// 將所有 CreateAssetMenu 的 menuName 路徑從
    /// "DeepAbyssHive/Config/..." 統一為 "DeepAbyssHive/Configs/..."
    /// - 只掃描 Assets/DeepAbyssHive/*.cs
    /// - 只改符合精確模式的 menuName
    /// - 改寫前先建立 .bak
    /// - 提供 Restore 功能可一鍵還原
    /// </summary>
    public class ConfigMenuUnifier : EditorWindow
    {
        private const string Root = "Assets/DeepAbyssHive";
        private static readonly Regex MenuPattern = new Regex(
            "menuName\\s*=\\s*\"DeepAbyssHive/Config/([^\"]+)\"",
            RegexOptions.Compiled);

        private List<string> _candidates = new List<string>();
        private Vector2 _scroll;

        [MenuItem("DeepAbyssHive/Tools/Configs/Unify 'Config' Menus → 'Configs'")]
        public static void Open()
        {
            var w = GetWindow<ConfigMenuUnifier>("Config Menu Unifier");
            w.minSize = new Vector2(720, 420);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Audit", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    RunAudit();
                if (GUILayout.Button("Apply Rename", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    ApplyRename();
                if (GUILayout.Button("Restore .bak", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    RestoreBak();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Scripts Folder", EditorStyles.toolbarButton, GUILayout.Width(150)))
                    PingFolder(Root);
            }

            EditorGUILayout.HelpBox(
                "目的：將所有 ScriptableObject 的 CreateAssetMenu 路徑統一為「DeepAbyssHive/Configs/...」。\n" +
                "流程：先 Audit 檢視 → Apply 會逐檔建立 .bak 並改寫 → 編譯後選單只會出現 Configs。\n" +
                "如需回滾，按 Restore .bak 即可還原。",
                MessageType.Info);

            EditorGUILayout.LabelField($"Root = {Root}");
            EditorGUILayout.Space();

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = sv.scrollPosition;
                if (_candidates.Count == 0)
                {
                    EditorGUILayout.LabelField("尚未掃描或沒有找到需要統一的檔案。");
                }
                else
                {
                    EditorGUILayout.LabelField($"找到 {_candidates.Count} 個候選檔案：");
                    foreach (var p in _candidates)
                        EditorGUILayout.LabelField("• " + p);
                }
            }
        }

        private void RunAudit()
        {
            _candidates.Clear();
            var files = Directory.GetFiles(Root, "*.cs", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var text = File.ReadAllText(f);
                if (text.Contains("CreateAssetMenu") && MenuPattern.IsMatch(text))
                    _candidates.Add(f.Replace("\\", "/"));
            }
            Repaint();
            EditorUtility.DisplayDialog("Audit", $"找到 {_candidates.Count} 個需要統一的檔案。", "OK");
        }

        private void ApplyRename()
        {
            if (_candidates.Count == 0)
            {
                if (!EditorUtility.DisplayDialog("Apply Rename", "目前清單為空，是否先進行 Audit？", "OK", "取消"))
                    return;
                RunAudit();
                if (_candidates.Count == 0) return;
            }

            int changed = 0, skipped = 0, failed = 0;
            foreach (var path in _candidates.ToList())
            {
                try
                {
                    var full = Path.GetFullPath(path);
                    var text = File.ReadAllText(full);

                    if (!MenuPattern.IsMatch(text)) { skipped++; continue; }

                    var bak = full + ".bak";
                    if (!File.Exists(bak))
                        File.Copy(full, bak);

                    var replaced = MenuPattern.Replace(text, m =>
                    {
                        var rest = m.Groups[1].Value;
                        return $"menuName = \"DeepAbyssHive/Configs/{rest}\"";
                    });

                    if (replaced != text)
                    {
                        File.WriteAllText(full, replaced);
                        changed++;
                    }
                    else skipped++;
                }
                catch (Exception ex)
                {
                    failed++;
                    DAHLog.Error(LogCategory.CONFIG, $"[ConfigMenuUnifier] 變更失敗：{path}\n{ex}");
                }
            }

            AssetDatabase.Refresh();
            RunAudit(); // 重新掃描
            EditorUtility.DisplayDialog("Apply Rename",
                $"完成。\n變更：{changed}\n略過：{skipped}\n失敗：{failed}\n\n已建立 .bak，可用 Restore 還原。",
                "OK");
        }

        private void RestoreBak()
        {
            var files = Directory.GetFiles(Root, "*.cs.bak", SearchOption.AllDirectories);
            int restored = 0, failed = 0;
            foreach (var bak in files)
            {
                try
                {
                    var orig = bak.Substring(0, bak.Length - 4);
                    File.Copy(bak, orig, true);
                    File.Delete(bak);
                    restored++;
                }
                catch (Exception ex)
                {
                    failed++;
                    DAHLog.Error(LogCategory.CONFIG, $"[ConfigMenuUnifier] 還原失敗：{bak}\n{ex}");
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Restore", $"還原完成：{restored}，失敗：{failed}", "OK");
        }

        private void PingFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                var name = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder(parent, name);
            }
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
#endif