#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using DeepAbyssHive.Units.Agents;

public static class UnitDevMenu
{
    private const string PrefabPath = "Assets/QA/Smoke/Dev/Prefabs/UnitDev.prefab";

    [MenuItem("DeepAbyss/Dev/Create or Select UnitDev Prefab")]
    public static void CreateOrSelectUnitDev()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[Editor] Selected existing UnitDev prefab at {PrefabPath}");
            return;
        }

        // 確保資料夾存在
        var dir = Path.GetDirectoryName(PrefabPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        // 建立臨時物件：Capsule + UnitAgent
        var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        temp.name = "UnitDev";
        if (temp.GetComponent<UnitAgent>() == null) temp.AddComponent<UnitAgent>();

        // 儲存為 Prefab（不連到場景）
        var saved = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath, out bool success);
        GameObject.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();

        if (success && saved != null)
        {
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"[Editor] Created UnitDev prefab at {PrefabPath}");
        }
        else
        {
            Debug.LogError("[Editor] Failed to create UnitDev prefab.");
        }
    }
}
#endif