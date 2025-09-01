#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FindBadUnityEvents
{
    [MenuItem("Tools/QA/Find invalid Unity event signatures")]
    public static void Run()
    {
        var unityNames = new[] { "Update", "LateUpdate", "FixedUpdate" };

        // 建索引：Type -> MonoScript path
        var allScripts = AssetDatabase.FindAssets("t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => (path: p, script: AssetDatabase.LoadAssetAtPath<MonoScript>(p)))
            .Where(t => t.script != null && t.script.GetClass() != null)
            .GroupBy(t => t.script.GetClass())
            .ToDictionary(g => g.Key, g => g.First().path);

        int offenders = 0;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types = Array.Empty<Type>();
            try { types = asm.GetTypes(); } catch { continue; }

            foreach (var t in types)
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(t)) continue;

                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!unityNames.Contains(m.Name)) continue;

                    var ps = m.GetParameters();
                    if (ps.Length == 0) continue; // 只抓帶參數的

                    offenders++;
                    allScripts.TryGetValue(t, out var path);

                    string signature = m.ToString();       // e.g. "Void Update(Single)"
                    string asmName   = t.Assembly.GetName().Name;
                    string where     = string.IsNullOrEmpty(path) ? $"(assembly: {asmName}, file: unknown)" : path;

                    Debug.LogError($"[{t.FullName}] {m.Name} has parameters => {signature}. Fix signature. File: {where}");
                }
            }
        }

        Debug.Log($"FindBadUnityEvents: Offenders = {offenders}");
    }
}
#endif
