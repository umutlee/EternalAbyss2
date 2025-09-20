using System;
using System.Collections;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Diagnostics
{
    /// <summary>
    /// 提供 Try/安全協程封裝與例外格式化。
    /// 後續可逐步以 ErrorPolicy.Try / host.StartSafeCoroutine 取代高風險區塊。
    /// </summary>
    public static class ErrorPolicy
    {
        /// <summary>在 try/catch 中執行，錯誤以結構化日誌輸出，不拋出。</summary>
        public static void Try(Action action, string context = null)
        {
            try { action?.Invoke(); }
            catch (Exception ex) { DAHLog.Error(LogCategory.COMMON, FormatException(ex, context)); }
        }

        /// <summary>安全協程：在 MoveNext 期間捕捉例外，避免炸掉執行緒；錯誤會被記錄。</summary>
        public static Coroutine StartSafeCoroutine(this MonoBehaviour host, IEnumerator routine, string context = null)
        {
            if (host == null || routine == null) return null;
            return host.StartCoroutine(SafeWrap(routine, context));
        }

        private static IEnumerator SafeWrap(IEnumerator inner, string context)
        {
            bool moveNext;
            object current = null;
            while (true)
            {
                try
                {
                    moveNext = inner.MoveNext();
                    if (!moveNext) yield break;
                    current = inner.Current;
                }
                catch (Exception ex)
                {
                    DAHLog.Error(LogCategory.COMMON, FormatException(ex, context));
                    yield break; // 協程終止但不炸引擎
                }
                yield return current;
            }
        }

        /// <summary>將例外訊息精簡化，避免長堆疊造成刷屏。</summary>
        public static string FormatException(Exception ex, string context = null, int maxLines = 10)
        {
            if (ex == null) return "<null exception>";
            var msg = $"{ex.GetType().Name}: {ex.Message}";
            var st = ex.StackTrace ?? string.Empty;
            var lines = st.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > maxLines) st = string.Join("\n", lines, 0, maxLines) + "\n…";
            return string.IsNullOrEmpty(context) ? $"{msg}\n{st}" : $"[{context}] {msg}\n{st}";
        }
    }
}