using UnityEngine;

[CreateAssetMenu(menuName = "DeepAbyssHive/Configs/TerrainConfig", fileName = "TerrainConfig")]
public class TerrainConfigSO : ScriptableObject
{
    // ===== 新設計（M1） =====
    [Header("Streaming / LOD (new)")]
    [Min(1)] public int chunkSize = 32;
    [Min(1)] public int maxLODLevels = 4;
    [Min(10f)] public float viewDistance = 512f;

    [Header("Generation (new)")]
    public int noiseSeed = 1337;                 // 新命名：seed 對應
    public float heightScale = 40f;
    [Range(0.0001f, 1f)] public float noiseFrequency = 0.01f; // 新命名：noiseScale 對應
    [Min(1)] public int noiseOctaves = 4;

    // ===== 舊碼相容欄位（不要移除，先讓現有程式跑起來） =====
    [Header("Compatibility (legacy fields used by existing code)")]
    [Min(1)] public int tileSize = 2;            // 舊碼使用：格子大小（世界單位/每格）
    [Min(1f)] public float loadRadius = 512f;    // 舊碼使用：載入半徑（通常≈viewDistance）
    [Range(0.0001f, 1f)] public float noiseScale = 0.01f; // 舊碼名稱，等價於 noiseFrequency
    public int seed = 1337;                      // 舊碼名稱，等價於 noiseSeed
    [Min(1)] public int maxModificationsPerFrame = 64;
    [Min(0.001f)] public float modificationProcessInterval = 0.05f;

    private void OnValidate()
    {
        // 基本護欄，避免出現 0 或負值
        chunkSize = Mathf.Max(1, chunkSize);
        maxLODLevels = Mathf.Max(1, maxLODLevels);
        viewDistance = Mathf.Max(10f, viewDistance);

        tileSize = Mathf.Max(1, tileSize);
        loadRadius = Mathf.Max(10f, loadRadius);
        noiseScale = Mathf.Max(0.0001f, noiseScale);
        noiseFrequency = Mathf.Max(0.0001f, noiseFrequency);
        maxModificationsPerFrame = Mathf.Max(1, maxModificationsPerFrame);
        modificationProcessInterval = Mathf.Max(0.001f, modificationProcessInterval);

        // 盡量讓新舊數值保持一致（非強制，只做輕同步）
        if (Mathf.Abs(noiseScale - noiseFrequency) > 1e-6f)
            noiseScale = noiseFrequency; // 以新設計為主
        if (seed != noiseSeed)
            seed = noiseSeed;
        if (loadRadius < viewDistance)
            loadRadius = viewDistance;   // 載入半徑至少不小於可視距離
    }
}
