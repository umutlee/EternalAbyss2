#if UNITY_EDITOR
// [EA-DEV|2025-09-10] 進入 Play 自動將焦點切到 Game 視窗，避免必須先點一下才能收鍵盤/滑鼠。
// 僅在 Editor 生效；打包不包含。
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FocusGameOnPlay
{
    static FocusGameOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        // 反射取得 UnityEditor.GameView 類型並切焦點
        var asm = typeof(EditorWindow).Assembly;
        var gameViewType = asm.GetType("UnityEditor.GameView");
        if (gameViewType == null) return;
        var gv = EditorWindow.GetWindow(gameViewType);
        if (gv != null) gv.Focus();
        Debug.Log("[Editor] Focused Game view on EnteredPlayMode");
    }
}
#endif