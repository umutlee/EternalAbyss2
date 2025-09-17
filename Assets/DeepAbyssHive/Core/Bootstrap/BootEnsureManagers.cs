using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Services.Config;

namespace DeepAbyssHive.Core.Boot
{
    // 注意：不能是 abstract、不能是 partial
    public sealed class BootEnsureManagers : MonoBehaviour
    {
        private static bool _ensured;

        private void Awake()
        {
            if (_ensured)
            {
                // 若場景裡重複放了 Boot 物件，後來的自動移除即可
                Destroy(gameObject);
                return;
            }

            _ensured = true;

            // 1) 找/建 Managers root 並標記常駐
            var root = GameObject.Find("Managers");
            if (root == null)
            {
                root = new GameObject("Managers");
                DAHLog.Info(LogCategory.MANAGER, "[BOOT] Created 'Managers' root and marked DontDestroyOnLoad.");
            }
            DontDestroyOnLoad(root);

            // 1.5) 開機摘要與配置審計（不中斷遊戲流程）
            ConfigAuditor.PrintGameConfigSummary();
            ConfigAuditor.RunOnce();

            // 2) 確保四個 Manager 存在（直接 typeof 省去反射與字串）
            EnsureComponent<DeepAbyssHive.Creep.Managers.CreepManager>(root, "[BOOT] Added {0} to 'Managers'.");
            EnsureComponent<DeepAbyssHive.Units.Managers.UnitManager>(root, "[BOOT] Added {0} to 'Managers'.");
            EnsureComponent<DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager>(root, "[BOOT] Added {0} to 'Managers'.");
            EnsureComponent<DeepAbyssHive.Terrain.Managers.TerrainManager>(root, "[BOOT] Added {0} to 'Managers'.");
        }

        private static T EnsureComponent<T>(GameObject go, string logFmt) where T : Component
        {
            if (!go.TryGetComponent<T>(out var comp))
            {
                comp = go.AddComponent<T>();
                DAHLog.Info(LogCategory.MANAGER, string.Format(logFmt, typeof(T).FullName));
            }
            return comp;
        }
    }
}