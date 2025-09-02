using UnityEngine;
using UnityEngine.Assertions;

// 新增：引入我們的 Managers
using DeepAbyssHive.Creep.Managers;
using DeepAbyssHive.Units.Managers;
using DeepAbyssHive.SpatialIndex.Managers;

namespace QA.Smoke
{
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        private void Start()
        {
            Run();
        }

        public static void Run()
        {
            var creep = Object.FindObjectOfType<CreepManager>();
            Assert.IsNotNull(creep, "[SMOKE] CreepManager not found in scene");
            Debug.Log("[SMOKE] CreepManager present & active ✔");

            var unit = Object.FindObjectOfType<UnitManager>();
            Assert.IsNotNull(unit, "[SMOKE] UnitManager not found in scene");
            Debug.Log("[SMOKE] UnitManager present & active ✔");

            var spatial = Object.FindObjectOfType<SpatialIndexManager>();
            Assert.IsNotNull(spatial, "[SMOKE] SpatialIndexManager not found in scene");
            Debug.Log("[SMOKE] SpatialIndexManager present & active ✔");
        }
    }
}
