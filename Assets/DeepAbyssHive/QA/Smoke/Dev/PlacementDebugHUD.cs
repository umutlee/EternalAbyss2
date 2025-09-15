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
    private Rect _rect;

    private void Start()
    {
        _rect = HudDragUtil.GetRect("HUD.PlacementDebug", new Rect(10, 200, 580, 90));
    }

    private void OnGUI()
    {
        var cfg = GameConfigProvider.Current;
        var hasSI = PlacementValidator.HasSpatialIndex;
        var hasCreep = PlacementValidator.HasRequireCreep;

        var last = PlacementValidator.LastResult;
        string lastText = last == null ? "(no checks yet)" : $"{last.code} ok={last.ok}";

        string configText = $"Placement cfg: useSI={cfg.useSpatialIndexForPlacement} requireCreep={cfg.requireCreep} margin={cfg.margin:0.###} minSpacing={cfg.minSpacing:0.###}";
        string hooksText = $"Hooks: SpatialIndex={(hasSI ? "OK" : "NONE")}  Creep={(hasCreep ? "OK" : "NONE")}";
        string lastResultText = $"Last: {lastText}";

        _rect = HudDragUtil.DraggableWindow("HUD.PlacementDebug", _rect, "Placement Debug", () =>
        {
            GUILayout.Label(configText);
            GUILayout.Label(hooksText);
            GUILayout.Label(lastResultText);
        });

        // 變更時也打一條 Console，方便過帳
        string fullText = $"[HUD] {configText}\n[HUD] {hooksText}\n[HUD] {lastResultText}";
        if (fullText != _lastLog)
        {
            _lastLog = fullText;
            Debug.Log(fullText);
        }
    }
}