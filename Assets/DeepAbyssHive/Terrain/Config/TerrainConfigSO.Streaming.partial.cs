using UnityEngine;

namespace DeepAbyssHive.Terrain.Config
{
    /// <summary>
    /// Streaming 參數放入 TerrainConfigSO（加值 A）
    /// </summary>
    public partial class TerrainConfigSO
    {
        [Header("Streaming")]
        [Min(0.02f)] public float streamUpdateInterval = 0.25f;
        [Min(0)]     public int   streamHysteresisChunks = 1;
    }
}