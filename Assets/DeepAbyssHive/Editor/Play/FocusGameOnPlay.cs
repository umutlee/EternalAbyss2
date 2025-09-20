#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace DeepAbyssHive.EditorTools
{
    /// <summary>進入 Play 自動聚焦 Game 視窗；可在選單切換，EditorPrefs 記憶。</summary>
    [InitializeOnLoad]
    public static class FocusGameOnPlay
    {
        private const string PrefKey = "DAH.FocusGameOnPlay";

        static FocusGameOnPlay()
        {
            EditorApplication.playModeStateChanged += OnStateChanged;
        }

        [MenuItem("DeepAbyssHive/Focus Game On Play", priority = 10)]
        public static void Toggle()
        {
            bool enabled = EditorPrefs.GetBool(PrefKey, true);
            EditorPrefs.SetBool(PrefKey, !enabled);
            Debug.Log($"[HUD] FocusGameOnPlay={( !enabled ? "ON" : "OFF")}");
        }

        [MenuItem("DeepAbyssHive/Focus Game On Play", true)]
        public static bool ToggleValidate()
        {
            bool enabled = EditorPrefs.GetBool(PrefKey, true);
            Menu.SetChecked("DeepAbyssHive/Focus Game On Play", enabled);
            return true;
        }

        private static void OnStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (!EditorPrefs.GetBool(PrefKey, true)) return;
            var gameView = GetGameView();
            if (gameView != null) gameView.Focus();
        }

        private static EditorWindow GetGameView()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            return EditorWindow.GetWindow(T);
        }
    }
}
#endif