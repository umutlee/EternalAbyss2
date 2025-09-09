using UnityEngine;
using DeepAbyssHive.Creep.Managers;

namespace QA.Smoke.Dev
{
    public class CreepBrushAndRunner : MonoBehaviour
    {
        [Tooltip("每幀擴張的最大格數")]
        public int budgetPerFrame = 2000;

        [Tooltip("Raycast 命中層（預設 Terrain）")]
        public LayerMask rayMask = 0;

        [Tooltip("左鍵種子啟用")]
        public bool enableSeeding = true;

        void Reset()
        {
            int terrain = LayerMask.NameToLayer("Terrain");
            rayMask = (terrain >= 0) ? (1 << terrain) : ~0;
        }

        void Update()
        {
            var cm = CreepManager.GetActive();
            if (!cm) 
            {
                if (Time.frameCount % 300 == 0) // 每5秒提醒一次
                    Debug.LogWarning("[CreepBrushAndRunner] CreepManager not found or not active.");
                return;
            }

            // 每幀跑擴張
            if (budgetPerFrame > 0)
                cm.StepExpansionBudgeted(budgetPerFrame);

            // 左鍵種子
            if (enableSeeding && Input.GetMouseButtonDown(0))
            {
                var cam = Camera.main;
                if (!cam) 
                {
                    Debug.LogWarning("[CreepBrushAndRunner] Main camera not found.");
                    return;
                }
                
                Vector3 mp = (Cursor.lockState == CursorLockMode.Locked)
                    ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                    : Input.mousePosition;
                Ray ray = cam.ScreenPointToRay(mp);
                
                if (Physics.Raycast(ray, out var hit, 10000f, rayMask, QueryTriggerInteraction.Ignore))
                {
                    cm.SeedWorld(hit.point);
                    Debug.Log($"[CREEP] Seed at world {hit.point}");
                }
                else
                {
                    Debug.LogWarning($"[CreepBrushAndRunner] Raycast missed. LayerMask: {rayMask.value}");
                }
            }
        }
    }
}