#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using DeepAbyssHive.Core.Config;

public static class GameConfigMenu
{
    private const string AssetPath = "Assets/Resources/Configs/GameConfig.asset";

    [MenuItem("DeepAbyss/Configs/Create or Select GameConfig")]
    public static void CreateOrSelectGameConfig()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameConfigSO>(AssetPath);
        if (asset == null)
        {
            // 確保資料夾存在
            var dir = Path.GetDirectoryName(AssetPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            asset = ScriptableObject.CreateInstance<GameConfigSO>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Editor] Created GameConfig at {AssetPath}");
        }
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
#endif