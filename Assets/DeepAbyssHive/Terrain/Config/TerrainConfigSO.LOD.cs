// Assets/DeepAbyssHive/Terrain/Config/TerrainConfigSO.LOD.cs
using UnityEngine;

namespace DeepAbyssHive.Terrain.Config
{
    // 只補缺的兩個，避免重複
    public partial class TerrainConfigSO
    {
        [Header("LOD / View")]
        [Min(1)]   public int   maxLODLevels = 4;     // LOD 等級數
        [Min(10f)] public float viewDistance = 512f;  // 額外視距(選用)
    }
}
