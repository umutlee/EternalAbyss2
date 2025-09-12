#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DeepAbyssHive.Dev.Logging.Editor
{
    [InitializeOnLoad]
    public static class DevLogBootstrap
    {
        static DevLogBootstrap()
        {
            // 確保 Resources/DevLogSettings.asset 存在
            var settings = Resources.Load<DevLogSettingsSO>("DevLogSettings");
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<DevLogSettingsSO>();
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CreateAsset(settings, "Assets/Resources/DevLogSettings.asset");
                AssetDatabase.SaveAssets();
            }

            // 如需自動開窗，可以解除註解
            // SmartConsoleWindow.Open();
        }
    }
}
#endif