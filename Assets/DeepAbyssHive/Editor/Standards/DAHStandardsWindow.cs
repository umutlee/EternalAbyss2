#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Constants;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.EditorTools.Standards
{
    /// <summary>
    /// 一站式標準化工具面板
    /// - 修正 CreateAssetMenu（Configs/Templates 分流）
    /// - 修正 / 搬移資產存放位置（含 BuildingCatalog 固定到 Resources/Configs）
    /// - UnitTemplate → UnitTemplateSO 遷移 / 重綁 / 清理
    /// - 開關：Auto-Enforce On Import
    /// </summary>
    public class DAHStandardsWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _log = "";
        private bool _autoEnforceOnImport;

        [MenuItem(MenuPaths.Tools + "Standards & Fixes")]
        public static void Open()
        {
            var w = GetWindow<DAHStandardsWindow>("DAH Standards");
            w.minSize = new Vector2(840, 520);
            w.LoadPrefs();
        }

        private void LoadPrefs()
        {
            _autoEnforceOnImport = EditorPrefs.GetBool("DAH.AutoEnforceOnImport", false);
        }
        private void SavePrefs()
        {
            EditorPrefs.SetBool("DAH.AutoEnforceOnImport", _autoEnforceOnImport);
        }

        private void Append(string msg)
        {
            _log += msg + "\n";
            try { DAHLog.Info(LogCategory.CONFIG, "[Standards] " + msg); } catch { Debug.Log("[Standards] " + msg); }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(80))) _log = "";
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear Log", EditorStyles.toolbarButton, GUILayout.Width(80))) _log = "";
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("CreateAssetMenu", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("將所有 [CreateAssetMenu] 統一改為使用常數：\nConfigs → MenuPaths.Configs + \"...\"\nTemplates → MenuPaths.Templates + \"...\"", MessageType.None);
            if (GUILayout.Button("Scan & Fix Menus (Preview → Confirm)"))
            {
                var (scanned, changes) = CreateAssetMenuRewriter.ScanForChanges();
                if (changes.Count > 0)
                {
                    int applied = CreateAssetMenuRewriter.ApplyChanges(changes);
                    Append($"Menus fixed. scanned={scanned}, applied={applied}");
                }
                else
                {
                    Append($"No menu changes needed. scanned={scanned}");
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Asset Save Locations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("將 Config SO 移動到 " + AssetPaths.ConfigsFolder + "；Template 移動到 " + AssetPaths.TemplatesFolder + "。\n（僅列出不在正確位置者，按下會顯示確認清單）", MessageType.None);
            if (GUILayout.Button("Scan & Move Misplaced Assets (SO/Template)"))
            {
                var r = Standards.AssetPathEnforcer.ScanAndMoveInteractive();
                Append($"Move done. moved={r.moved}, skipped={r.skipped}");
            }

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                _autoEnforceOnImport = EditorGUILayout.ToggleLeft("Auto-Enforce On Import (move new SO/Template to standard folders)", _autoEnforceOnImport);
                if (GUI.changed) { SavePrefs(); Standards.AssetPathEnforcer.SetAuto(_autoEnforceOnImport); }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Building Catalog", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("建立或選取唯一的 BuildingCatalog，並確保它位於 Resources/Configs。", MessageType.None);
            if (GUILayout.Button("Create or Select BuildingCatalog at Resources/Configs"))
            {
                var obj = Standards.BuildingCatalogMenu.CreateOrSelectAtStandardPath();
                if (obj != null) Append("Catalog ready at " + AssetDatabase.GetAssetPath(obj));
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("UnitTemplate → UnitTemplateSO", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Migrate Assets")) Standards.UnitTemplateMigrator.MigrateAssets();
                if (GUILayout.Button("Lint Code")) Standards.UnitTemplateMigrator.LintCode();
                if (GUILayout.Button("Rebind References")) Standards.UnitTemplateMigrator.RebindReferences();
                if (GUILayout.Button("Try Remove Legacy UnitTemplate.cs (safe)")) {
                    var ok = Standards.UnitTemplateMigrator.TryRemoveLegacy();
                    Append(ok ? "Legacy UnitTemplate removed/moved." : "Legacy UnitTemplate NOT removed (has references or assets).");
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_log, GUILayout.Height(120));
        }
    }
}
#endif