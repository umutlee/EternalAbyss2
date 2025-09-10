using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 放置/驗證規則的全域設定（從 Resources/Configs/GameConfig.asset 讀取）
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DeepAbyssHive/Configs/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Placement Validation")]
        [Tooltip("啟用並聯校驗：Physics 與 SpatialIndex 皆須通過才允許放置")]
        public bool useSpatialIndexForPlacement = false;

        [Tooltip("放置物之間的最小中心間距（世界單位）")]
        public float minSpacing = 0.0f;

        [Tooltip("碰撞檢查額外外擴邊界（世界單位）")]
        public float margin = 0.0f;

        [Tooltip("是否要求必須在菌毯上才可放置")]
        public bool requireCreep = false;

        [Header("Placement UX")]
        [Tooltip("格點對齊步長（世界單位；0=關閉）。啟用後放置中心會對齊到該步長的網格。")]
        public float snapSize = 0.0f;

        [Tooltip("旋轉步進（度；0=關閉自由旋轉）。僅量化 Y 軸旋轉，並在預覽/放置均套用。")]
        public float rotationStepDegrees = 0.0f;

        [Header("Dev / Testing")]
        [Tooltip("Placement SMOKE 觸發鍵（None=停用）。")]
        public KeyCode placementSmokeKey = KeyCode.F7;
        [Tooltip("建築刪除主鍵（None=停用）。")]
        public KeyCode buildingDeleteKey1 = KeyCode.Delete;
        [Tooltip("建築刪除副鍵（None=停用）。")]
        public KeyCode buildingDeleteKey2 = KeyCode.X;
        [Tooltip("DEV：生成單位熱鍵（None=停用）。")]
        public KeyCode devUnitsSpawnKey = KeyCode.F9;
        [Tooltip("DEV：單位測試熱鍵（指派目標；None=停用）。")]
        public KeyCode devUnitsTestKey = KeyCode.F10;
        [Tooltip("DEV：一次生成的單位數量。")]
        public int devSpawnCount = 200;

        [Header("Input / Cursor")]
        [Tooltip("右鍵是否鎖定游標（mouselook）。預設關閉以避免與建造/點地互動衝突。")]
        public bool rmbLocksCursor = false;

        [Header("Units × Creep")]
        [Tooltip("在 Creep 上的速度倍率（1 = 不變）。")]
        public float creepSpeedMul = 1.25f;
        [Tooltip("不在 Creep 上的速度倍率（1 = 不變）。")]
        public float offCreepSpeedMul = 1.0f;
        [Tooltip("UnitAgent 取樣是否在 Creep 上的週期（秒）。")]
        public float creepSampleInterval = 0.25f;
    }

    /// <summary>
    /// 提供目前執行中的 Game 設定快取與載入
    /// </summary>
    public static class GameConfigProvider
    {
        private static GameConfigSO _current;

        /// <summary>
        /// 優先讀取 Resources/Configs/GameConfig；若無則回退為記憶體預設
        /// </summary>
        public static GameConfigSO Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<GameConfigSO>("Configs/GameConfig");
                    if (_current == null)
                    {
                        _current = ScriptableObject.CreateInstance<GameConfigSO>();
                        _current.name = "GameConfig (Runtime Default)";
                    }
                }
                return _current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LogConfigOnLoad()
        {
            var cfg = Current;
            Debug.Log($"[DEV HUD] Game: useSpatialIndex={cfg.useSpatialIndexForPlacement}, minSpacing={cfg.minSpacing:0.###}, margin={cfg.margin:0.###}, requireCreep={cfg.requireCreep}, snapSize={cfg.snapSize:0.###}, rotStep={cfg.rotationStepDegrees:0.#}, smokeKey={cfg.placementSmokeKey}, delKey1={cfg.buildingDeleteKey1}, delKey2={cfg.buildingDeleteKey2}, spawnKey={cfg.devUnitsSpawnKey}, testKey={cfg.devUnitsTestKey}, spawnCount={cfg.devSpawnCount}, creepMul={cfg.creepSpeedMul:0.##}/{cfg.offCreepSpeedMul:0.##}, creepDt={cfg.creepSampleInterval:0.##}s, rmbLock={cfg.rmbLocksCursor}");
        }
    }
}