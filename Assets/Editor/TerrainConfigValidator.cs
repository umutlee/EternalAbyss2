using UnityEngine;
using UnityEditor;
using DeepAbyssHive.Terrain.Config;

namespace DeepAbyssHive.Editor
{
    public class TerrainConfigValidator
    {
        [MenuItem("DeepAbyssHive/Validate TerrainConfig")]
        public static void ValidateTerrainConfig()
        {
            var config = Resources.Load<DeepAbyssHive.Terrain.Config.TerrainConfigSO>("Configs/TerrainConfig");
            if (config == null)
            {
                Debug.LogError("TerrainConfig not found in Resources/Configs/");
                return;
            }

            Debug.Log($"TerrainConfig loaded successfully:");
            Debug.Log($"  chunkSize: {config.chunkSize}");
            Debug.Log($"  maxLODLevels: {config.maxLODLevels}");
            Debug.Log($"  viewDistance: {config.viewDistance}");
            Debug.Log($"  tileSize: {config.tileSize}");
            Debug.Log($"  loadRadius: {config.loadRadius}");

            // 測試 ApplyConfig 日誌
            Debug.Log($"[DEV HUD] Terrain: chunkSize={config.chunkSize}, LOD={config.maxLODLevels}, view={config.viewDistance}");
        }

        [MenuItem("DeepAbyssHive/Recreate TerrainConfig")]
        public static void RecreateTerrainConfig()
        {
            var config = ScriptableObject.CreateInstance<DeepAbyssHive.Terrain.Config.TerrainConfigSO>();
            
            // 設置預設值
            config.chunkSize = 32;
            config.tileSize = 1.0f;
            config.loadRadius = 3.0f;
            config.maxLODLevels = 4;
            config.viewDistance = 512f;
            config.noiseScale = 0.1f;
            config.heightScale = 10f;
            config.seed = 12345;

            // 保存到 Resources 資料夾
            string path = "Assets/Resources/Configs/TerrainConfig.asset";
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"TerrainConfig recreated at {path}");
            Debug.Log($"New config values: chunkSize={config.chunkSize}, LOD={config.maxLODLevels}, view={config.viewDistance}");
        }
    }
}