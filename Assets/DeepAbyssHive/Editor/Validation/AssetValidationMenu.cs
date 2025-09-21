#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Editor.Validation
{
    public static class AssetValidationMenu
    {
        [MenuItem("DeepAbyssHive/Validation/Run Asset Validation")]
        public static void Run()
        {
            EditorUtility.DisplayDialog("DeepAbyssHive", "Asset validation will run on next play (see Console).", "OK");
        }
    }
}
#endif