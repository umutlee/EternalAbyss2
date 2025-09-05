#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using DeepAbyssHive.Creep.Managers;

namespace QA.Dev
{
    /// <summary>
    /// 只在 Editor/開發版啟用：按鍵在滑鼠指向處「增減」菌毯格子，方便肉眼測。
    /// C：切換目前格子（沒有就加，有就移除）
    /// X：在目前格子周圍加一個十字形
    /// </summary>
    [AddComponentMenu("DeepAbyss/Dev/Creep Debug Input")]
    public sealed class CreepDebugInput : MonoBehaviour
    {
        private CreepManager _creep;

        private void Awake()
        {
            _creep = FindObjectOfType<CreepManager>();
            if (_creep == null)
                Debug.LogWarning("[Dev] CreepManager not found. Add Managers or run BootEnsureManagers.");
        }

        private void Update()
        {
            if (_creep == null) return;

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (TryGetMouseWorldPoint(out var pos))
                {
                    var cell = _creep.WorldToCell(pos);
                    bool removed = _creep.RemoveCreep(cell);
                    if (!removed) _creep.AddCreep(cell);
                    Debug.Log($"[Dev] Toggle creep at cell {cell} ({(removed ? "Removed" : "Added")})");
                }
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                if (TryGetMouseWorldPoint(out var pos))
                {
                    var c = _creep.WorldToCell(pos);
                    var dirs = new Vector2Int[] { Vector2Int.zero, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    foreach (var d in dirs) _creep.AddCreep(c + d);
                    Debug.Log($"[Dev] Add cross at {c}");
                }
            }
        }

        private static bool TryGetMouseWorldPoint(out Vector3 world)
        {
            world = default;
            var cam = Camera.main;
            if (!cam) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 先試著打到任何碰撞體
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                world = hit.point;
                return true;
            }

            // 沒碰撞體就打到 y=0 的平面
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                world = ray.GetPoint(dist);
                return true;
            }

            return false;
        }
    }
}
#endif
