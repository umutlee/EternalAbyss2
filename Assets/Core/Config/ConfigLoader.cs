using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public static class ConfigLoader
{
    // 會去 Resources/{folder}/{assetName} 找；Editor 下若缺少可自動建立
    public static T LoadOrDefault<T>(string folder, string assetName) where T : ScriptableObject
    {
        var pathInResources = string.IsNullOrEmpty(folder) ? assetName : (folder + "/" + assetName);
        var so = Resources.Load<T>(pathInResources);
        if (so != null) return so;

        #if UNITY_EDITOR
        // 在 Editor 自動產生預設 SO，放到 Assets/Resources/{folder}/{assetName}.asset
        var dir = string.IsNullOrEmpty(folder) ? "Assets/Resources" : $"Assets/Resources/{folder}";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var instance = ScriptableObject.CreateInstance<T>();
        var assetPath = $"{dir}/{assetName}.asset";
        AssetDatabase.CreateAsset(instance, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ConfigLoader] Created default {typeof(T).Name} at {assetPath}");
        return instance;
        #else
        Debug.LogWarning($"[ConfigLoader] Missing config at Resources/{pathInResources}. Using null/defaults.");
        return null;
        #endif
    }
}
