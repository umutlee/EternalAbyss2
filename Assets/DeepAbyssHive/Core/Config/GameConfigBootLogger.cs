using System;
using System.Reflection;
using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 啟動後輸出 GameConfig 的精簡快照（只打一行），
    /// 所有參數皆從 Resources/Configs/GameConfig 以反射讀取；
    /// 日誌走 Smart Console（分類：CONFIG）。若無可用 API，僅退回單次 Warning。
    /// </summary>
    internal static class GameConfigBootLogger
    {
        private static bool s_logged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LogOnce()
        {
            if (s_logged) return;
            s_logged = true;

            // 以 Object 載入，避免對 GameConfigSO 產生編譯期相依
            var cfg = Resources.Load("Configs/GameConfig");
            if (cfg == null)
            {
                SmartConsoleShim.Log("CONFIG", "GameConfig asset not found at Resources/Configs/GameConfig. Skipped startup dump.");
                return;
            }

            string Get(object target, params string[] names)
            {
                var t = target.GetType();
                foreach (var n in names)
                {
                    var fi = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                    if (fi != null) return Format(fi.GetValue(target));
                    var pi = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                    if (pi != null && pi.CanRead) return Format(pi.GetValue(target, null));
                }
                return "N/A";
            }

            static string Format(object v)
            {
                if (v == null) return "null";
                if (v is float f) return f.ToString("0.###");
                if (v is double d) return d.ToString("0.###");
                if (v is Vector2 v2) return $"{v2.x:0.###},{v2.y:0.###}";
                if (v is Vector3 v3) return $"{v3.x:0.###},{v3.y:0.###},{v3.z:0.###}";
                return v.ToString();
            }

            var line =
                $"useSpatialIndex={Get(cfg, "useSpatialIndex")}, " +
                $"minSpacing={Get(cfg, "minSpacing")}, margin={Get(cfg, "margin")}, " +
                $"requireCreep={Get(cfg, "requireCreep")}, snapSize={Get(cfg, "snapSize")}, rotStep={Get(cfg, "rotStep")}, " +
                $"placerToggleKey={Get(cfg, "placerToggleKey","placeToggleKey","buildToggleKey")}, " +
                $"delKey1={Get(cfg, "delKey1","deleteKey1")}, delKey2={Get(cfg, "delKey2","deleteKey2")}, " +
                $"spawnKey={Get(cfg, "spawnKey")}, testKey={Get(cfg, "testKey")}, spawnCount={Get(cfg, "spawnCount")}, " +
                $"rmbLock={Get(cfg, "rmbLock","rightMouseLock")}, " +
                $"healthLoggingEnabled={Get(cfg, "healthLoggingEnabled")}, healthLogInterval={Get(cfg, "healthLogInterval")}, " +
                $"unitBatchSize={Get(cfg, "unitBatchSize")}, unitBatchInterval={Get(cfg, "unitBatchInterval")}, " +
                $"verboseLogs={Get(cfg, "verboseLogs")}";

            SmartConsoleShim.Log("CONFIG", $"GameConfig snapshot → {line}");
        }

        /// <summary>
        /// 以反射橋接 Smart Console，不建立編譯期相依。
        /// 嘗試尋找：
        /// - public static void DLog(string category, string message, params string[] tags)
        /// - 或 public static void Log(string category, string message) on *SmartConsole* 類別
        /// 找不到時僅 Warning 一次以避免刷屏。
        /// </summary>
        private static class SmartConsoleShim
        {
            private static Action<string,string> s_log;
            private static bool s_warned;

            public static void Log(string category, string message)
            {
                if (s_log == null) s_log = FindLogger();
                if (s_log != null)
                {
                    s_log(category, message);
                }
                else if (!s_warned)
                {
                    s_warned = true;
                    Debug.LogWarning($"[{category}] {message} (Smart Console logger not found; fallback once)"); // 單次退回，避免刷屏
                }
            }

            private static Action<string,string> FindLogger()
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch { continue; }

                    foreach (var t in types)
                    {
                        if (!t.IsClass) continue;
                        if (!t.Name.Contains("SmartConsole") && t.Name != "DLog") continue;

                        var m1 = t.GetMethod("DLog", BindingFlags.Public | BindingFlags.Static);
                        if (m1 != null)
                        {
                            return (c, m) => m1.Invoke(null, new object[] { c, m, null });
                        }

                        var m2 = t.GetMethod("Log", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
                        if (m2 != null)
                        {
                            return (c, m) => m2.Invoke(null, new object[] { c, m });
                        }
                    }
                }
                return null;
            }
        }
    }
}