using System;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

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
                DAHLog.Warn(LogCategory.CONFIG, "GameConfig asset not found at Resources/Configs/GameConfig. Skipped startup dump.");
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
                $"verboseLogs={Get(cfg, "verboseLogs")}, " +
                $"enableCostChecking={Get(cfg, "enableCostChecking")}, showToastOnInsufficientResources={Get(cfg, "showToastOnInsufficientResources")}, " +
                $"logPlacementAttempts={Get(cfg, "logPlacementAttempts")}";

            DAHLog.Info(LogCategory.CONFIG, $"GameConfig snapshot → {line}");
        }


    }
}