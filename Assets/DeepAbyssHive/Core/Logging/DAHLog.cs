using System;
using UnityEngine;

namespace DeepAbyssHive.Core.Logging
{
    /// <summary>
    /// 統一日誌入口。格式： [CAT][HH:mm:ss.fff][frame] message
    /// 解析規則供 SmartConsole 使用。
    /// </summary>
    public static class DAHLog
    {
        public static bool IncludeFrame = true;

        static string Prefix(LogCategory cat)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            string frame = IncludeFrame ? $"[{UnityEngine.Time.frameCount}]" : "";
            return $"[{cat}][{time}]{frame} ";
        }

        public static void Info(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            if (ctx) Debug.Log(Prefix(cat) + message, ctx);
            else Debug.Log(Prefix(cat) + message);
        }

        public static void Warn(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            if (ctx) Debug.LogWarning(Prefix(cat) + message, ctx);
            else Debug.LogWarning(Prefix(cat) + message);
        }

        /// <summary>Warning 方法別名，保持向後兼容</summary>
        public static void Warning(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            Warn(cat, message, ctx);
        }

        public static void Error(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            if (ctx) Debug.LogError(Prefix(cat) + message, ctx);
            else Debug.LogError(Prefix(cat) + message);
        }

        /// <summary>開發期噪音；正式版可關閉或改為條件編譯。</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Dev(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            Info(cat, message, ctx);
        }

        /// <summary>Debug 日誌方法，等同於 Info</summary>
        public static void Debug(LogCategory cat, string message, UnityEngine.Object? ctx = null)
        {
            Info(cat, message, ctx);
        }
    }
}