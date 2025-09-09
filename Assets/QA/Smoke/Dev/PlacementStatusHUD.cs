using UnityEngine;
using DeepAbyssHive.Common.Placement; // Result<> / PlaceResultCode
using DeepAbyssHive.Core.Config;      // 讀取當前 GameConfig（僅顯示用）
using QA.Smoke.Dev;

/// <summary>
/// Dev 專用：顯示放置狀態與彩色 wire bounds（不入侵 Placer）。
/// 掛在 Dev 場景任意物件上即可（例如 Managers 或空物件）。
/// </summary>
public class PlacementStatusHUD : MonoBehaviour
{
    // 畫線持續時間（秒）；0 代表只畫當幀
    private const float LINE_DURATION = 0f;
    private Rect _rect;

    private void Start()
    {
        _rect = HudDragUtil.GetRect("HUD.PlaceStatus", new Rect(10, 70, 720, 120));
    }

    private void Update()
    {
        var last = PlacementValidator.LastResult;
        if (last == null) return;

        // 根據結果選色並畫出 wire bounds（12 條邊）
        var color = PlacementUiUtil.ColorFor(last, false);
        DrawBoundsWire(last.data, color, LINE_DURATION);
    }

    private void OnGUI()
    {
        var last = PlacementValidator.LastResult;
        var cfg = GameConfigProvider.Current;

        string status = PlacementUiUtil.TextFor(last);
        Color guiColor = PlacementUiUtil.ColorFor(last, false);

        // 可拖曳狀態面板
        GUI.color = guiColor;
        _rect = HudDragUtil.DraggableWindow("HUD.PlaceStatus", _rect, "Placement Status", () =>
        {
            GUILayout.Label($"Status: {status}");
            GUILayout.Label($"useSI={cfg.useSpatialIndexForPlacement} requireCreep={cfg.requireCreep}");
            GUILayout.Label($"margin={cfg.margin:0.###} minSpacing={cfg.minSpacing:0.###}");
            GUILayout.Label($"snapSize={cfg.snapSize:0.###} | Mode: Preview");
        });
        GUI.color = Color.white;
    }

    // （顏色邏輯改由 PlacementUiUtil 統一管理）

    private static void DrawBoundsWire(Bounds b, Color c, float duration)
    {
        Vector3 c0 = b.center;
        Vector3 e  = b.extents;
        Vector3 p000 = c0 + new Vector3(-e.x, -e.y, -e.z);
        Vector3 p001 = c0 + new Vector3(-e.x, -e.y,  e.z);
        Vector3 p010 = c0 + new Vector3(-e.x,  e.y, -e.z);
        Vector3 p011 = c0 + new Vector3(-e.x,  e.y,  e.z);
        Vector3 p100 = c0 + new Vector3( e.x, -e.y, -e.z);
        Vector3 p101 = c0 + new Vector3( e.x, -e.y,  e.z);
        Vector3 p110 = c0 + new Vector3( e.x,  e.y, -e.z);
        Vector3 p111 = c0 + new Vector3( e.x,  e.y,  e.z);

        DrawLine(p000, p001, c, duration); DrawLine(p001, p011, c, duration);
        DrawLine(p011, p010, c, duration); DrawLine(p010, p000, c, duration);

        DrawLine(p100, p101, c, duration); DrawLine(p101, p111, c, duration);
        DrawLine(p111, p110, c, duration); DrawLine(p110, p100, c, duration);

        DrawLine(p000, p100, c, duration); DrawLine(p001, p101, c, duration);
        DrawLine(p011, p111, c, duration); DrawLine(p010, p110, c, duration);
    }

    private static void DrawLine(Vector3 a, Vector3 b, Color c, float duration)
    {
        Debug.DrawLine(a, b, c, duration);
    }
}