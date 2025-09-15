using UnityEngine;

/// <summary>
/// 小型 HUD 拖曳工具：以 PlayerPrefs 永續化視窗位置。
/// 用法：
///   _rect = HudDragUtil.GetRect("Key", defaultRect);
///   _rect = HudDragUtil.DraggableWindow("Key", _rect, "Title", () => { /* 內容 */ });
/// </summary>
public static class HudDragUtil
{
    public static Rect GetRect(string key, Rect def)
    {
        float x = PlayerPrefs.GetFloat(key + ".x", def.x);
        float y = PlayerPrefs.GetFloat(key + ".y", def.y);
        float w = PlayerPrefs.GetFloat(key + ".w", def.width);
        float h = PlayerPrefs.GetFloat(key + ".h", def.height);
        // 防止跑到螢幕外
        x = Mathf.Clamp(x, -10, Screen.width  - 50);
        y = Mathf.Clamp(y, -10, Screen.height - 20);
        return new Rect(x, y, w, h);
    }

    public static void SaveRect(string key, Rect r)
    {
        PlayerPrefs.SetFloat(key + ".x", r.x);
        PlayerPrefs.SetFloat(key + ".y", r.y);
        PlayerPrefs.SetFloat(key + ".w", r.width);
        PlayerPrefs.SetFloat(key + ".h", r.height);
    }

    public static Rect DraggableWindow(string key, Rect rect, string title, System.Action draw)
    {
        int id = key.GetHashCode();
        Rect newRect = GUI.Window(id, rect, _ =>
        {
            // 上方 20px 做拖曳區
            var drag = new Rect(0, 0, 10000, 20);
            GUI.DragWindow(drag);
            GUILayout.BeginVertical();
            GUILayout.Space(2);
            draw?.Invoke();
            GUILayout.EndVertical();
        }, title);
        if (newRect.position != rect.position) SaveRect(key, newRect);
        return newRect;
    }
}