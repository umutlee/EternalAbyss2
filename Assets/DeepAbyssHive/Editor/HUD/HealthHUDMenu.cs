#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.EditorTools.HUD
{
    /// <summary>Editor 菜單切換 HealthHUD 顯示；僅在 Editor 下編譯。</summary>
    public static class HealthHUDMenu
    {
        [MenuItem("DeepAbyssHive/HUD/Toggle Health HUD")]	
        private static void Toggle()
        {
            var t = typeof(UnityEngine.GameObject).Assembly.GetType("DeepAbyssHive.QA.Smoke.Dev.HUD.HealthHUD");
            if (t == null) return;
            var mi = t.GetMethod("Toggle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            mi?.Invoke(null, null);
        }
    }
}
#endif