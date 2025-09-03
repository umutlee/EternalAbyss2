#if UNITY_EDITOR
using UnityEngine;
using DeepAbyssHive.Terrain.Managers;

public class TerrainStreamingDebugHUD : MonoBehaviour
{
    private TerrainManager _tm;

    private void Awake() { _tm = FindObjectOfType<TerrainManager>(); }

    private void OnGUI()
    {
        if (_tm == null) return;

        var rect = new Rect(10, 10, 520, 22);
        GUI.Label(rect,
            $"[STREAM HUD] center={_tm.CurrentStreamCenterChunk}  interval={_tm.StreamUpdateInterval:0.###}s  hysteresis={_tm.StreamHysteresisChunks}ch");
    }
}
#endif