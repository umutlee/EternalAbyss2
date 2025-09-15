using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 鎖定 GameConfig 的載入策略：
    /// 1) 優先從固定路徑 Resources/Configs/GameConfig.asset 載入；
    /// 2) 次選 Resources/Configs 下第一個名稱含 "GameConfig" 的 ScriptableObject；
    /// 3) 成功後嘗試綁定到 GameConfigProvider.Current（以反射支援不同實作）。
    /// </summary>
    public static class GameConfigLoadLock
    {
        private const string FIXED_PATH = "Configs/GameConfig"; // Resources 相對路徑
        private static bool _boundOnce;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBindOnLoad()
        {
            TryBindProvider(out var where);
            if (!string.IsNullOrEmpty(where))
                DAHLog.Info(LogCategory.CONFIG, $"[CONFIG] GameConfig bound via {where}");
            else
                DAHLog.Warn(LogCategory.CONFIG, "[CONFIG] GameConfig not found. 建議執行：DeepAbyssHive → Configs → Create or Select → GameConfig");
        }

        /// <summary>嘗試載入並綁定到 Provider；回傳綁定來源描述字串（null 代表失敗）。</summary>
        public static string TryBindProvider(out string where)
        {
            where = null;
            var cfg = LoadFixed(out var origin) ?? LoadFallback(out origin);
            if (cfg == null) return null;

            if (BindToProvider(cfg))
            {
                where = origin;
                _boundOnce = true;
            }
            return where;
        }

        private static ScriptableObject LoadFixed(out string origin)
        {
            origin = null;
            var obj = Resources.Load(FIXED_PATH) as ScriptableObject;
            if (obj != null) { origin = $"Resources/{FIXED_PATH}.asset"; return obj; }
            return null;
        }

        private static ScriptableObject LoadFallback(out string origin)
        {
            origin = null;
            // 僅掃描 Configs 目錄以避免全專案掃資源造成負擔
            var arr = Resources.LoadAll("Configs", typeof(ScriptableObject));
            var pick = arr.FirstOrDefault(o => o != null && o.name.IndexOf("GameConfig", StringComparison.OrdinalIgnoreCase) >= 0)
                       as ScriptableObject;
            if (pick != null) { origin = $"Resources/Configs/{pick.name}.asset"; return pick; }
            return null;
        }

        private static bool BindToProvider(ScriptableObject cfg)
        {
            // 尋找 Provider 類型
            var provType = Type.GetType("DeepAbyssHive.Core.Config.GameConfigProvider, Assembly-CSharp");
            if (provType == null)
                provType = AppDomain.CurrentDomain.GetAssemblies()
                          .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                          .FirstOrDefault(t => t.Name == "GameConfigProvider");

            if (provType == null)
            {
                DAHLog.Warn(LogCategory.CONFIG, "[CONFIG] 找不到 GameConfigProvider 類別，僅完成資源載入，無法對外供應 Current。");
                return false;
            }

            const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // 1) 嘗試屬性 setter
            var prop = provType.GetProperty("Current", S);
            if (prop != null && prop.CanWrite && (prop.PropertyType == cfg.GetType() || prop.PropertyType.IsAssignableFrom(cfg.GetType())))
            {
                prop.SetValue(null, cfg);
                return true;
            }

            // 2) 嘗試已知欄位
            var fields = new[] { "Current", "_current", "s_current" };
            foreach (var f in fields)
            {
                var fi = provType.GetField(f, S);
                if (fi != null && (fi.FieldType == cfg.GetType() || fi.FieldType.IsAssignableFrom(cfg.GetType())))
                {
                    fi.SetValue(null, cfg);
                    return true;
                }
            }

            // 3) 嘗試 SetCurrent(cfg) / Initialize(cfg) / Init(cfg)
            var methods = new[] { "SetCurrent", "Initialize", "Init" };
            foreach (var m in methods)
            {
                var mi = provType.GetMethod(m, S);
                if (mi != null)
                {
                    var ps = mi.GetParameters();
                    if (ps.Length == 1 && (ps[0].ParameterType == cfg.GetType() || ps[0].ParameterType.IsAssignableFrom(cfg.GetType())))
                    {
                        mi.Invoke(null, new object[] { cfg });
                        return true;
                    }
                }
            }

            DAHLog.Warn(LogCategory.CONFIG, "[CONFIG] 找到 GameConfig 但無法綁定到 Provider（缺 setter/欄位/方法）。");
            return false;
        }

#if UNITY_EDITOR
        [MenuItem("DeepAbyssHive/Configs/Rebind GameConfig (Fixed Path)")]
        private static void RebindMenu()
        {
            var result = TryBindProvider(out var where);
            if (!string.IsNullOrEmpty(result))
                EditorUtility.DisplayDialog("GameConfig", $"綁定成功：{where}", "OK");
            else
                EditorUtility.DisplayDialog("GameConfig", "綁定失敗，請確認 Resources/Configs 下已有 GameConfig.asset。", "OK");
        }
#endif
    }
}