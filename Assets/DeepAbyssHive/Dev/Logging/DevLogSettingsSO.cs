using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Dev.Logging
{
    public enum LogLevel { Trace=0, Debug=1, Info=2, Warn=3, Error=4, Fatal=5 }

    [CreateAssetMenu(fileName = "DevLogSettings", menuName = "DeepAbyss/DevLog Settings")]
    public class DevLogSettingsSO : ScriptableObject
    {
        [Header("General")]
        public LogLevel minLevel = LogLevel.Debug;
        [Min(100)] public int ringBufferCapacity = 2000;

        [Header("Folding / Debounce")]
        public bool foldDuplicates = true;
        [Range(0f, 5f)] public float foldWindowSeconds = 1.0f;

        [Header("Rate Limit (per category)")]
        public bool enableRateLimit = true;
        [Min(1)] public int maxLogsPerSecond = 200;

        [Header("Tag Bridge")]
        public bool enableTagBridge = true;
        [Tooltip("若訊息前綴為 [Xxx] 會視為分類；找不到則歸為 General")]
        public string fallbackCategory = "General";

        [Header("Known Categories (初始清單，可動態增補)")]
        public List<string> knownCategories = new()
        {
            "BOOT","DEV","HUD","CREEP","UNIT","TERRAIN","STREAM",
            "HEALTH","SMOKE","PLACEMENT","Placement",
            "TerrainManager","CreepManager","UnitManager",
            "UnitDevSpawner","CreepSim","Game","General"
        };

        [Header("Runtime Overlay (optional)")]
        public bool enableRuntimeOverlay = false;
        public LogLevel overlayMinLevel = LogLevel.Warn;

        /// <summary>對應 Unity LogType 的預設層級對映。</summary>
        public static LogLevel FromUnityLogType(LogType lt) => lt switch {
            LogType.Error => LogLevel.Error,
            LogType.Assert => LogLevel.Error,
            LogType.Warning => LogLevel.Warn,
            LogType.Log => LogLevel.Info,
            LogType.Exception => LogLevel.Fatal,
            _ => LogLevel.Info
        };
    }
}