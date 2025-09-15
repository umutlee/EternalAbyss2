using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 以反射從 GameConfigSO 讀取 KeyCode 欄位；不存在時用預設並記一行說明。
    /// </summary>
    public static class GameConfigKeysCompat
    {
        private static object _cfg;

        static GameConfigKeysCompat()
        {
            var prov = System.Type.GetType("DeepAbyssHive.Core.Config.GameConfigProvider, Assembly-CSharp");
            var curProp = prov?.GetProperty("Current", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            _cfg = curProp?.GetValue(null);
        }

        public static void TryBindOrDefault(string fieldName, ref KeyCode key, KeyCode fallback = KeyCode.None)
        {
            if (_cfg == null) { if (fallback != KeyCode.None) key = fallback; return; }
            var t = _cfg.GetType();
            var fi = t.GetField(fieldName, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if (fi != null && fi.FieldType == typeof(KeyCode))
            {
                key = (KeyCode)fi.GetValue(_cfg);
            }
            else
            {
                if (fallback != KeyCode.None) key = fallback;
                DAHLog.Info(LogCategory.CONFIG, $"[CONFIG] GameConfig 缺少 {fieldName}，使用預設 {key}。");
            }
        }
    }
}