#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[DisallowMultipleComponent]
public class EditorAutoSelectOnSpawn : MonoBehaviour
{
    private void Start()
    {
        // 下一幀再選，確保物件已完全建立
        EditorApplication.delayCall += () =>
        {
            if (!this || !gameObject) return;
            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
        };
    }
}
#endif