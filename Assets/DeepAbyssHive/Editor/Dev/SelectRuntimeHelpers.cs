#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Editor 選取輔助：一鍵選到 DontDestroyOnLoad 內的 Managers 或 ~PlacementOutline。
/// </summary>
public static class SelectRuntimeHelpers
{
    [MenuItem("DeepAbyssHive/Dev/Select Managers (DontDestroyOnLoad) %#m")]
    public static void SelectManagers()
    {
        var go = FindByName("Managers");
        if (go == null) { EditorUtility.DisplayDialog("Select Managers", "Managers not found (Play 模式中才會存在，或尚未啟動 Boot)。", "OK"); return; }
        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log($"[Editor] Selected: {GetPath(go)}");
    }

    [MenuItem("DeepAbyssHive/Dev/Select Placement Outline")]
    public static void SelectPlacementOutline()
    {
        var go = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g && g.name.StartsWith("~PlacementOutline"));
        if (go == null) { EditorUtility.DisplayDialog("Select Placement Outline", "~PlacementOutline not found (需進 Play 且開啟預覽)。", "OK"); return; }
        Selection.activeObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log($"[Editor] Selected: {GetPath(go)}");
    }

    private static GameObject FindByName(string name)
    {
        return Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(g => g && g.name == name);
    }

    private static string GetPath(GameObject go)
    {
        string p = go.name;
        var t = go.transform.parent;
        while (t != null) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
#endif