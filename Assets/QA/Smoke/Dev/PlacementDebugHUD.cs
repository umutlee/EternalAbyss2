using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Common.Placement;

/// <summary>
/// Dev 專用 HUD：顯示放置規則參數、Predicate 鉤掛狀態、最近一次驗證結果（C）
/// 挂在任意場景物件上即可；不影響正式版（可加到 Dev_Playground）。
/// </summary>
public class PlacementDebugHUD : MonoBehaviour
{
    private string _lastLog;

    private void OnGUI()
    {
        var cfg = GameConfigProvider.Current;
        var hasSI = PlacementValidator.HasSpatialIndex;
        var hasCreep = PlacementValidator.HasRequireCreep;

        var last = PlacementValidator.LastResult;
        string lastText = last == null ? "(no checks yet)" : $"{last.code} ok={last.ok}";

        string text = $"[HUD] Placement cfg: useSI={cfg.useSpatialIndexForPlacement} requireCreep={cfg.requireCreep} margin={cfg.margin:0.###} minSpacing={cfg.minSpacing:0.###}\n" +
                      $"[HUD] Hooks: SpatialIndex={(hasSI ? "OK" : "NONE")}  Creep={(hasCreep ? "OK" : "NONE")}\n" +
                      $"[HUD] Last: {lastText}";

        // 左上角小面板
        GUI.Box(new Rect(10, 10, 560, 70), "");
        GUI.Label(new Rect(20, 20, 540, 50), text);

        // 變更時也打一條 Console，方便過帳
        if (text != _lastLog)
        {
            _lastLog = text;
            Debug.Log(text);
        }
    }
}