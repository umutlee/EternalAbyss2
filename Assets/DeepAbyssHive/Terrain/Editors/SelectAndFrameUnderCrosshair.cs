#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Terrain.Editors
{
    public static class SelectAndFrameUnderCrosshair
    {
        // Ctrl/Cmd(%) + Alt(&) + F  →  Win: Ctrl+Alt+F, Mac: Cmd+Alt+F
        [MenuItem("DeepAbyssHive/Dev/Select & Frame Under Crosshair %&f", priority = 1500)]
        private static void SelectAndFrame()
        {
            Camera cam = Camera.main;
            if (!cam && SceneView.lastActiveSceneView != null)
                cam = SceneView.lastActiveSceneView.camera;
            if (!cam) { DAHLog.Warning(LogCategory.DEV, "[Dev] No camera found to raycast from."); return; }

            // 用相機像素維度取中心，對 Game/Scene 皆穩定
            Vector3 center = new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f);
            Ray ray = cam.ScreenPointToRay(center);

            var hits = Physics.RaycastAll(ray, 10000f);
            if (hits == null || hits.Length == 0)
            {
                DAHLog.Info(LogCategory.DEV, "[Dev] Nothing under crosshair.");
                return;
            }

            // 依距離排序，挑第一個「不是 TerrainChunkRuntime（含父節點）」的命中
            var ordered = hits.OrderBy(h => h.distance);
            GameObject pick = null;
            foreach (var h in ordered)
            {
                var go = h.collider ? h.collider.gameObject : null;
                if (!go) continue;
                if (go.GetComponentInParent<DeepAbyssHive.Terrain.Chunks.TerrainChunkRuntime>() != null)
                    continue; // 跳過地形塊
                pick = go;
                break;
            }

            // 若都只有地形，則取最近的一個（至少有東西可選）
            if (!pick) pick = ordered.First().collider.gameObject;

            Selection.activeGameObject = pick;
            EditorGUIUtility.PingObject(pick);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
            DAHLog.Info(LogCategory.DEV, $"[Dev] Selected under crosshair: {pick.name}");
        }
    }
}
#endif