using UnityEngine;
using DeepAbyssHive.Common.Placement; // Result<> / PlaceResultCode
using DeepAbyssHive.Core.Config;      // 讀取當前 GameConfig（僅顯示用）

/// <summary>
/// Dev 專用：顯示放置狀態與彩色 wire bounds（不入侵 Placer）。
/// 掛在 Dev 場景任意物件上即可（例如 Managers 或空物件）。
/// </summary>
public class PlacementStatusHUD : MonoBehaviour
{
    // 畫線持續時間（秒）；0 代表只畫當幀
    private const float LINE_DURATION = 0f;

    private void Update()
    {
        var last = PlacementValidator.LastResult;
        if (last == null) return;

        // 根據結果選色並畫出 wire bounds（12 條邊）
        var color = ColorFor(last.code, last.ok);
        DrawBoundsWire(last.data, color, LINE_DURATION);
    }

    private void OnGUI()
    {
        var last = PlacementValidator.LastResult;
        var cfg = GameConfigProvider.Current;

        string status = last == null ? "(no checks yet)" : $"{last.code} ok={last.ok} {last.message}";
        Color guiColor = last == null ? Color.white : ColorFor(last.code, last.ok);

        // 左上角狀態面板
        GUI.color = guiColor;
        GUI.Box(new Rect(10, 90, 560, 85), "");
        GUI.Label(new Rect(20, 100, 540, 20), $"[PLACE] {status}");
        GUI.Label(new Rect(20, 120, 540, 20), $"cfg: useSI={cfg.useSpatialIndexForPlacement} requireCreep={cfg.requireCreep} margin={cfg.margin:0.###} minSpacing={cfg.minSpacing:0.###}");
        GUI.Label(new Rect(20, 140, 540, 20), $"snapSize={cfg.snapSize:0.###} | Mode: Preview");
        GUI.color = Color.white;
    }

    private static Color ColorFor(PlaceResultCode code, bool ok)
    {
        if (ok) return Color.green;
        switch (code)
        {
            case PlaceResultCode.E_PLACE_COLLISION: return Color.red;
            case PlaceResultCode.E_REQUIRE_CREEP:   return Color.yellow;
            case PlaceResultCode.E_OUT_OF_BOUNDS:   return new Color(1f, 0f, 1f); // magenta
            case PlaceResultCode.E_INVALID_TYPE:    return Color.cyan;
            default:                                return Color.white;
        }
    }

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