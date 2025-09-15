// Assets/QA/Smoke/RuntimeSmoke.cs
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace QA.Smoke
{
    /// <summary>
    /// 極小型冒煙測試：驗證 Managers 物件是否存在，且三個 Manager 元件是否已附加且啟用。
    /// - 進入 Play 後下一幀會自動跑一次
    /// - 也可由 DevHotkeys（F5）呼叫 RunNow()
    /// </summary>
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            // 等待一幀，讓 BootEnsureManagers 有時間把 Managers 與各 Manager 建好
            yield return null;
            Run();
        }

        /// <summary>
        /// ✅ 對外可呼叫：立刻執行冒煙測試（不延遲）
        /// </summary>
        public void Run()
        {
            var managersGO = GameObject.Find("Managers");
            Assert.IsNotNull(managersGO, "[SMOKE] 'Managers' GameObject not found (Boot should create it)");
            Debug.Log("[SMOKE] Managers root present ✔");

            // 這三個型別名稱與 BootEnsureManagers 保持一致
            string[] expectedTypes =
            {
                "DeepAbyssHive.Creep.Managers.CreepManager",
                "DeepAbyssHive.Units.Managers.UnitManager",
                "DeepAbyssHive.SpatialIndex.Managers.SpatialIndexManager"
            };

            for (int i = 0; i < expectedTypes.Length; i++)
            {
                string typeName = expectedTypes[i];

                var t = FindType(typeName);
                Assert.IsNotNull(t, $"[SMOKE] Type not found: {typeName}");

                var comp = FindComponentOnManagersOrScene(managersGO, t);
                Assert.IsNotNull(comp, $"[SMOKE] Component {typeName} not found in scene");

                bool active = false;
                var bh = comp as Behaviour;
                if (bh != null) active = bh.isActiveAndEnabled;
                else
                {
                    var c = comp as Component;
                    if (c != null && c.gameObject != null) active = c.gameObject.activeInHierarchy;
                }

                Assert.IsTrue(active, $"[SMOKE] {typeName} present but disabled/inactive");
                Debug.Log($"[SMOKE] {typeName} present & active ✔");
            }

            Debug.Log("[SMOKE] M0-T03 PASS ✅");
        }

        /// <summary>
        /// ✅ 對外可呼叫：延一幀再跑，避免與 Boot 時序衝突
        /// </summary>
        public void RunNow(bool deferOneFrame = true)
        {
            if (deferOneFrame) { StartCoroutine(RunDeferred()); }
            else { Run(); }
        }

        private System.Collections.IEnumerator RunDeferred()
        {
            yield return null;
            Run();
        }

        // ---- Helpers --------------------------------------------------------

        /// <summary>在目前 AppDomain 內找出完整型別名稱</summary>
        private static Type FindType(string fullName)
        {
            // 直取
            var t = Type.GetType(fullName);
            if (t != null) return t;

            // 逐組件嘗試 GetType（較快）
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    t = asms[i].GetType(fullName, throwOnError: false);
                    if (t != null) return t;
                }
                catch { /* 忽略反射例外 */ }
            }

            // 最後手段：掃描所有型別（較慢，但一次也可）
            for (int i = 0; i < asms.Length; i++)
            {
                Type[] types = null;
                try { types = asms[i].GetTypes(); }
                catch (ReflectionTypeLoadException rtle) { types = rtle.Types; }
                catch { /* 忽略 */ }

                if (types == null) continue;
                for (int j = 0; j < types.Length; j++)
                {
                    var tt = types[j];
                    if (tt == null) continue;
                    if (tt.FullName == fullName) return tt;
                }
            }

            return null;
        }

        /// <summary>
        /// 先從 Managers 物件往下找，找不到再從整個場景（已載入物件）搜尋。
        /// 僅返回場景中可見的 Component（排除資產/隱藏物件）。
        /// </summary>
        private static Component FindComponentOnManagersOrScene(GameObject managersGO, Type t)
        {
            if (managersGO != null)
            {
                var c = managersGO.GetComponentInChildren(t, includeInactive: true) as Component;
                if (c != null) return c;
            }

            // 使用 Resources.FindObjectsOfTypeAll 遍歷已載入物件，過濾場景內的
            var all = Resources.FindObjectsOfTypeAll(t);
            for (int i = 0; i < all.Length; i++)
            {
                var comp = all[i] as Component;
                if (comp == null) continue;

                var go = comp.gameObject;
                if (go == null) continue;

                // 排除資產與未載入場景、排除隱藏物件
                var scene = go.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (go.hideFlags != HideFlags.None) continue;

                return comp;
            }

            return null;
        }
    }
}
