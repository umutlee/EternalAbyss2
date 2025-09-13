#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Terrain.Editors
{
    public static class EnsureTerrainLayer
    {
        // 建議先執行一次：建立 "Terrain" 層（若缺）
        [MenuItem("DeepAbyssHive/Config/Ensure 'Terrain' Layer", priority = 1200)]
        public static void Ensure()
        {
            const string layerName = "Terrain";
            var obj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (obj == null || obj.Length == 0) { DAHLog.Warning(LogCategory.SYSTEM, "[Config] TagManager not found."); return; }

            var tagManager = new SerializedObject(obj[0]);
            var layersProp = tagManager.FindProperty("layers");

            bool exists = false;
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                var p = layersProp.GetArrayElementAtIndex(i);
                if (p != null && p.stringValue == layerName) { exists = true; break; }
            }
            if (!exists)
            {
                // 使用者可用層索引為 8..31
                for (int i = 8; i < layersProp.arraySize; i++)
                {
                    var p = layersProp.GetArrayElementAtIndex(i);
                    if (p != null && string.IsNullOrEmpty(p.stringValue))
                    {
                        p.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        DAHLog.Info(LogCategory.SYSTEM, $"[Config] Added '{layerName}' layer at slot {i}.");
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    DAHLog.Warning(LogCategory.SYSTEM, "[Config] No empty layer slot available. Please add 'Terrain' layer manually in Project Settings > Tags and Layers.");
            }
            else
            {
                DAHLog.Info(LogCategory.SYSTEM, "[Config] 'Terrain' layer already exists.");
            }
        }
    }
}
#endif