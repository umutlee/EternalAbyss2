#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Editor.Health
{
    public static class BuildHealthMenu
    {
        [MenuItem("DeepAbyssHive/Build/Run First-Launch Health Check")]
        public static void RunCheck()
        {
            PlayerPrefs.DeleteKey("dah_first_launch_done");
            EditorUtility.DisplayDialog("DeepAbyssHive", "First-launch health check will run next Play (flag reset).", "OK");
        }
    }
}
#endif