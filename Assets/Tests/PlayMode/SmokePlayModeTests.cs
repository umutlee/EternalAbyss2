using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

public class SmokePlayModeTests
{
    // 允許 10 秒總 timeout（CI 上冷啟動較慢），每步 yield 一小段
    [UnityTest]
    public IEnumerator Boot_Creates_Managers_And_CoreManagers()
    {
#if UNITY_EDITOR
        // 建立一個新的臨時場景，避免依賴資產
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        yield return null;
#endif
        yield return new EnterPlayMode();
        // 讓 BootEnsureManagers / BootAuditor 有時間執行
        yield return null;
        yield return null;

        var managers = GameObject.Find("Managers");
        Assert.IsNotNull(managers, "Managers root should exist after entering Play.");

        string[] qnTypes = {
            "DeepAbyssHive.Creep.Managers.CreepManager",
            "DeepAbyssHive.Units.Managers.UnitManager",
            "DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager",
            "DeepAbyssHive.Terrain.Managers.TerrainManager"
        };
        foreach (var qn in qnTypes)
        {
            var t = Type.GetType(qn);
            Assert.IsNotNull(t, $"Type not found: {qn}. Ensure assembly/namespace is correct.");
            var inst = UnityEngine.Object.FindObjectOfType(t);
            Assert.IsNotNull(inst, $"Manager missing: {qn}");
        }

        // 觀察 3 秒，期間不得拋出未處理例外（若 ErrorGuard 開啟會被率限並記錄）
        var end = UnityEngine.Time.realtimeSinceStartup + 3f;
        while (UnityEngine.Time.realtimeSinceStartup < end) { yield return null; }

        yield return new ExitPlayMode();
    }
}