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
            Debug.Log($"[DEV HUD] Game: useSpatialIndex={cfg.useSpatialIndexForPlacement}, minSpacing={cfg.minSpacing:0.###}, margin={cfg.margin:0.###}, requireCreep={cfg.requireCreep}, snapSize={cfg.snapSize:0.###}");
        }
    }
}