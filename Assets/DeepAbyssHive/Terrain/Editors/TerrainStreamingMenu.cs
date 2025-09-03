#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Terrain.Editors
{
    /// <summary>
    /// Editor 菜單：一鍵把串流目標設為目前選取物件
    /// </summary>
    public static class TerrainStreamingMenu
    {
        [MenuItem("DeepAbyssHive/Streaming/Set Target To Selected", priority = 1500)]
        private static void SetTargetToSelected()
        {
            var mgr = Object.FindObjectOfType<DeepAbyssHive.Terrain.Managers.TerrainManager>();
            if (mgr == null)
            {
                Debug.LogWarning("[STREAM] TerrainManager not found in scene.");
                return;
            }

            if (Selection.activeTransform == null)
            {
                Debug.LogWarning("[STREAM] No Selection. Select a Transform first.");
                return;
            }

            mgr.SetStreamingTarget(Selection.activeTransform);
            Debug.Log($"[STREAM] Target set to '{Selection.activeTransform.name}' via menu.");
        }
    }
}
#endif