#if UNITY_EDITOR
using UnityEngine;
using DeepAbyssHive.Terrain.Managers;
using QA.Smoke.Dev;

public class TerrainStreamingDebugHUD : MonoBehaviour
{
    private TerrainManager _tm;
    private Rect _rect;

    private void Awake() { _tm = FindObjectOfType<TerrainManager>(); }

    private void Start()
    {
        _rect = HudDragUtil.GetRect("HUD.StreamHUD", new Rect(10, 10, 520, 60));
    }

    private void OnGUI()
    {
        if (_tm == null) return;

        _rect = HudDragUtil.DraggableWindow("HUD.StreamHUD", _rect, "Stream HUD", () =>
        {
            GUILayout.Label($"Center: {_tm.CurrentStreamCenterChunk}");
            GUILayout.Label($"Interval: {_tm.StreamUpdateInterval:0.###}s");
            GUILayout.Label($"Hysteresis: {_tm.StreamHysteresisChunks}ch");
        });
    }
}
#endif