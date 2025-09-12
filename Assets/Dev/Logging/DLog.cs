using UnityEngine;

namespace DeepAbyssHive.Dev.Logging
{
    public static class DLog
    {
        public static void T(string cat, string msg, Object ctx=null) => Write(LogLevel.Trace, cat, msg, ctx);
        public static void D(string cat, string msg, Object ctx=null) => Write(LogLevel.Debug, cat, msg, ctx);
        public static void I(string cat, string msg, Object ctx=null) => Write(LogLevel.Info , cat, msg, ctx);
        public static void W(string cat, string msg, Object ctx=null) => Write(LogLevel.Warn , cat, msg, ctx);
        public static void E(string cat, string msg, Object ctx=null) => Write(LogLevel.Error, cat, msg, ctx);
        public static void F(string cat, string msg, Object ctx=null) => Write(LogLevel.Fatal, cat, msg, ctx);

        public static void Tf(string cat, string fmt, params object[] args) => T(cat, string.Format(fmt,args));
        public static void Df(string cat, string fmt, params object[] args) => D(cat, string.Format(fmt,args));
        public static void If(string cat, string fmt, params object[] args) => I(cat, string.Format(fmt,args));
        public static void Wf(string cat, string fmt, params object[] args) => W(cat, string.Format(fmt,args));
        public static void Ef(string cat, string fmt, params object[] args) => E(cat, string.Format(fmt,args));
        public static void Ff(string cat, string fmt, params object[] args) => F(cat, string.Format(fmt,args));

        private static void Write(LogLevel level, string category, string message, Object ctx)
        {
#if UNITY_EDITOR
            // 給 SmartConsole 解析的明確前綴：[Level][Category]
            var prefix = $"[{level}][{category}] ";
            switch (level)
            {
                case LogLevel.Warn:  Debug.LogWarning(prefix + message, ctx); break;
                case LogLevel.Error:
                case LogLevel.Fatal: Debug.LogError  (prefix + message, ctx); break;
                default:             Debug.Log      (prefix + message, ctx); break;
            }
#else
            // Player 環境不需要層級前綴
            switch (level)
            {
                case LogLevel.Warn:  Debug.LogWarning(message, ctx); break;
                case LogLevel.Error:
                case LogLevel.Fatal: Debug.LogError  (message, ctx); break;
                default:             Debug.Log      (message, ctx); break;
            }
#endif
        }
    }
}