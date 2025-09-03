#if UNITY_EDITOR
using UnityEngine;

namespace DeepAbyssHive.Core.Boot
{
    /// <summary>
    /// Editor-only：在場景載入後自動把 TerrainStreamingDebugHUD 掛到 Managers（找不到則 Boot）
    /// 不修改既有 BootEnsureManagers，行為上等價於在那裡加一行。
    /// </summary>
    internal static class EditorAttachTerrainStreamingHUD
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachHud()
        {
            // 只在 Editor 生效
            var target = GameObject.Find("Managers");
            if (target == null) target = GameObject.Find("Boot");
            if (target == null) return;

            // 類別無 namespace，使用 global:: 明確指向
            if (target.GetComponent<global::TerrainStreamingDebugHUD>() == null)
            {
                target.AddComponent<global::TerrainStreamingDebugHUD>();
                Debug.Log("[DEV HUD] TerrainStreamingDebugHUD attached to '" + target.name + "' (Editor only).");
            }
        }
    }
}
#endif