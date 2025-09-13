// Assets/Editor/FindBadUnityEvents.cs
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
        int offenders = 0;
        int checkedTypes = 0;

        // TypeCache 在 2020+ 可用；若你的版本太舊，可改用 AppDomain 掃描
#if UNITY_2020_1_OR_NEWER
        var types = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();
#else
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            })
            .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t));
#endif

        foreach (var t in types)
        {
            checkedTypes++;
            var methods = t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                // 只檢查 Unity 的三個回呼名稱（顯式介面實作不會被抓到，正是我們要的）
                if (m.Name == "Update" || m.Name == "FixedUpdate" || m.Name == "LateUpdate")
                {
                    if (m.GetParameters().Length > 0)
                    {
                        offenders++;
                        Debug.LogError($"[{t.FullName}] {m.Name} has parameters. Fix signature.");
                    }
                }
            }
        }

        // 一定要輸出總結（用 Warning 比較不容易被過濾掉）
        Debug.LogWarning($"QA: FindBadUnityEvents finished. Offenders={offenders}, TypesChecked={checkedTypes}, Time={DateTime.Now:HH:mm:ss}");
    }
}
