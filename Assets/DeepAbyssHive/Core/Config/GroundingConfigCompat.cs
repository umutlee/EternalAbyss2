using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>以反射讀取 GameConfig 欄位；不存在就用預設並記一行。</summary>
    public static class GroundingConfigCompat
    {
        private static object _cfg;

        static GroundingConfigCompat()
        {
            var prov = System.Type.GetType("DeepAbyssHive.Core.Config.GameConfigProvider, Assembly-CSharp");
            var curProp = prov?.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            _cfg = curProp?.GetValue(null);
        }

        public static void TryBindInt(string field, ref int val, int fallback)
        {
            if (!TryGet(field, ref val)) { val = fallback; DAHLog.Info(DeepAbyssHive.Core.Logging.LogCategory.CONFIG, $"[CONFIG] {field} 缺少，使用預設 {fallback}"); }
        }
        public static void TryBindFloat(string field, ref float val, float fallback)
        {
            if (!TryGet(field, ref val)) { val = fallback; DAHLog.Info(DeepAbyssHive.Core.Logging.LogCategory.CONFIG, $"[CONFIG] {field} 缺少，使用預設 {fallback}"); }
        }
        public static void TryBindBool(string field, ref bool val, bool fallback)
        {
            if (!TryGet(field, ref val)) { val = fallback; DAHLog.Info(DeepAbyssHive.Core.Logging.LogCategory.CONFIG, $"[CONFIG] {field} 缺少，使用預設 {fallback}"); }
        }
        public static void TryBindLayerMask(string field, ref LayerMask val)
        {
            if (!TryGet(field, ref val))
            {
                val = Physics.DefaultRaycastLayers;
                DAHLog.Info(DeepAbyssHive.Core.Logging.LogCategory.CONFIG, $"[CONFIG] {field} 缺少，使用 Physics.DefaultRaycastLayers");
            }
        }

        private static bool TryGet<T>(string field, ref T val)
        {
            if (_cfg == null) return false;
            var t = _cfg.GetType();
            var fi = t.GetField(field, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (fi != null && typeof(T).IsAssignableFrom(fi.FieldType))
            {
                val = (T)fi.GetValue(_cfg);
                return true;
            }
            return false;
        }
    }
}