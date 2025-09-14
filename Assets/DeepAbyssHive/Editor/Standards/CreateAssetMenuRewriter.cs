#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Constants;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.EditorTools.Standards
{
    /// <summary>
    /// CreateAssetMenu 與 AddComponentMenu 路徑標準化重寫器
    /// - 掃描所有 .cs 檔案中的 [CreateAssetMenu] 和 [AddComponentMenu] 屬性
    /// - 將路徑統一改為 MenuPaths 常數引用
    /// - 提供預覽與確認機制，避免意外修改
    /// </summary>
    public static class CreateAssetMenuRewriter
    {
        public static (int scanned, List<RewriteItem> changes) ScanForChanges()
        {
            var changes = new List<RewriteItem>();
            string root = Application.dataPath.Replace("\\", "/");
            var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                                  .Where(p => p.Contains("/Assets/DeepAbyssHive/"))
                                  .ToArray();

            foreach (var file in csFiles)
            {
                string content = File.ReadAllText(file);
                var fileChanges = AnalyzeFile(file, content);
                changes.AddRange(fileChanges);
            }

            return (csFiles.Length, changes);
        }

        public static int ApplyChanges(List<RewriteItem> changes)
        {
            int applied = 0;
            var fileGroups = changes.GroupBy(c => c.filePath);

            foreach (var group in fileGroups)
            {
                string filePath = group.Key;
                string content = File.ReadAllText(filePath);
                string newContent = content;

                // 按行號倒序處理，避免行號偏移
                var sortedChanges = group.OrderByDescending(c => c.lineNumber).ToList();
                
                foreach (var change in sortedChanges)
                {
                    newContent = ApplySingleChange(newContent, change);
                    applied++;
                }

                if (newContent != content)
                {
                    File.WriteAllText(filePath, newContent);
                    string assetPath = ToAssetPath(filePath);
                    AssetDatabase.ImportAsset(assetPath);
                    Log($"Updated: {assetPath}");
                }
            }

            return applied;
        }

        private static List<RewriteItem> AnalyzeFile(string filePath, string content)
        {
            var changes = new List<RewriteItem>();
            string[] lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                
                // 檢查 CreateAssetMenu
                var createMatch = Regex.Match(line, @"\[CreateAssetMenu\s*\([^)]*menuName\s*=\s*""([^""]+)""");
                if (createMatch.Success)
                {
                    string oldPath = createMatch.Groups[1].Value;
                    string newPath = GetStandardMenuPath(oldPath, true);
                    if (newPath != null && oldPath != newPath)
                    {
                        changes.Add(new RewriteItem
                        {
                            filePath = filePath,
                            lineNumber = i + 1,
                            oldLine = lines[i],
                            newLine = lines[i].Replace($'"{oldPath}"', $'"{newPath}"'),
                            changeType = "CreateAssetMenu",
                            oldPath = oldPath,
                            newPath = newPath
                        });
                    }
                }

                // 檢查 AddComponentMenu
                var componentMatch = Regex.Match(line, @"\[AddComponentMenu\s*\(\s*""([^""]+)""");
                if (componentMatch.Success)
                {
                    string oldPath = componentMatch.Groups[1].Value;
                    string newPath = GetStandardMenuPath(oldPath, false);
                    if (newPath != null && oldPath != newPath)
                    {
                        changes.Add(new RewriteItem
                        {
                            filePath = filePath,
                            lineNumber = i + 1,
                            oldLine = lines[i],
                            newLine = lines[i].Replace($'"{oldPath}"', $'"{newPath}"'),
                            changeType = "AddComponentMenu",
                            oldPath = oldPath,
                            newPath = newPath
                        });
                    }
                }
            }

            return changes;
        }

        private static string GetStandardMenuPath(string oldPath, bool isCreateAssetMenu)
        {
            // 標準化路徑邏輯
            if (string.IsNullOrEmpty(oldPath)) return null;

            // 統一使用 DeepAbyssHive 前綴
            if (oldPath.StartsWith("DeepAbyss/") && !oldPath.StartsWith("DeepAbyssHive/"))
            {
                oldPath = oldPath.Replace("DeepAbyss/", "DeepAbyssHive/");
            }

            if (!oldPath.StartsWith("DeepAbyssHive/")) return null;

            if (isCreateAssetMenu)
            {
                // CreateAssetMenu 路徑標準化
                if (oldPath.Contains("Config") && (oldPath.Contains("SO") || oldPath.Contains("Settings")))
                {
                    // 配置類型 → MenuPaths.Config.ROOT
                    if (oldPath.Contains("Game")) return MenuPaths.Config.GAME_CONFIG;
                    if (oldPath.Contains("Creep")) return MenuPaths.Config.CREEP_CONFIG;
                    if (oldPath.Contains("Terrain")) return MenuPaths.Config.TERRAIN_CONFIG;
                    if (oldPath.Contains("DevLog")) return MenuPaths.Config.DEVLOG_SETTINGS;
                    return MenuPaths.Config.ROOT;
                }
                else if (oldPath.Contains("Template"))
                {
                    // 模板類型 → MenuPaths.Templates.ROOT
                    if (oldPath.Contains("Unit")) return MenuPaths.Templates.UNIT_TEMPLATE;
                    return MenuPaths.Templates.ROOT;
                }
                else if (oldPath.Contains("Catalog"))
                {
                    return MenuPaths.Config.BUILDING_CATALOG;
                }
            }
            else
            {
                // AddComponentMenu 路徑標準化
                if (oldPath.Contains("Dev") || oldPath.Contains("Debug"))
                {
                    return MenuPaths.Dev.ROOT;
                }
                else if (oldPath.Contains("HUD"))
                {
                    return MenuPaths.Dev.HUD;
                }
            }

            return null; // 不需要更改
        }

        private static string ApplySingleChange(string content, RewriteItem change)
        {
            string[] lines = content.Split('\n');
            if (change.lineNumber > 0 && change.lineNumber <= lines.Length)
            {
                lines[change.lineNumber - 1] = change.newLine;
            }
            return string.Join("\n", lines);
        }

        private static string ToAssetPath(string fullPath)
        {
            string proj = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            return fullPath.Replace("\\", "/").Replace(proj + "/", "");
        }

        private static void Log(string msg)
        {
            try { DAHLog.Info(LogCategory.CONFIG, "[CreateAssetMenuRewriter] " + msg); }
            catch { Debug.Log("[CreateAssetMenuRewriter] " + msg); }
        }

        [Serializable]
        public class RewriteItem
        {
            public string filePath;
            public int lineNumber;
            public string oldLine;
            public string newLine;
            public string changeType;
            public string oldPath;
            public string newPath;

            public override string ToString()
            {
                string fileName = Path.GetFileName(filePath);
                return $"{fileName}:{lineNumber} [{changeType}] {oldPath} → {newPath}";
            }
        }
    }
}
#endif