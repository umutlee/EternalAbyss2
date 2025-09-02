using UnityEngine;
using DeepAbyssHive.Creep.Managers;

namespace Gameplay.Building
{
    [AddComponentMenu("DeepAbyss/Gameplay/Building Placer")]
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("Footprint / Grid")]
        public float footprintSize = 1f; // 與 CreepManager 的 cellSize 對齊（預設 1）

        [Header("Preview")]
        public float previewHeight = 0.5f;  // 預覽方塊的高度
        public Vector3 placedScale = new Vector3(1f, 1f, 1f);

        private CreepManager _creep;
        private GameObject _preview;
        private Material _matGreen, _matRed;

        private void Start()
        {
            // Lazy 找 CreepManager（BootEnsureManagers 在同一幀會先創好）
            _creep = FindObjectOfType<CreepManager>();
            if (_creep == null)
                Debug.LogWarning("[Placer] CreepManager not found. Ensure BootEnsureManagers is in scene.");

            // 預覽方塊
            _preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _preview.name = "[Preview] Building";
            _preview.transform.localScale = new Vector3(footprintSize, previewHeight, footprintSize);
            Destroy(_preview.GetComponent<Collider>()); // 免得擋到 Raycast

            _matGreen = new Material(Shader.Find("Standard")) { color = new Color(0f, 1f, 0f, 0.5f) };
            _matRed   = new Material(Shader.Find("Standard")) { color = new Color(1f, 0f, 0f, 0.5f) };
            _matGreen.SetFloat("_Mode", 3); _matRed.SetFloat("_Mode", 3); // 透明
            _preview.GetComponent<MeshRenderer>().material = _matRed; // 初始先紅色
        }

        private void Update()
        {
            if (TryGetMouseWorldPoint(out var world))
            {
                // 對齊格子中心
                var cell = _creep != null ? _creep.WorldToCell(world) : new Vector2Int(
                    Mathf.FloorToInt(world.x / Mathf.Max(0.0001f, footprintSize)),
                    Mathf.FloorToInt(world.z / Mathf.Max(0.0001f, footprintSize))
                );
                var center = _creep != null ? _creep.CellToWorldCenter(cell) : new Vector3(
                    (cell.x + 0.5f) * footprintSize, 0f, (cell.y + 0.5f) * footprintSize
                );
                _preview.transform.position = center + Vector3.up * (previewHeight * 0.5f);

                bool canPlace = _creep != null && _creep.IsOnCreep(center);
                _preview.GetComponent<MeshRenderer>().material = canPlace ? _matGreen : _matRed;

                // 左鍵放置
                if (canPlace && Input.GetMouseButtonDown(0))
                {
                    Place(center);
                }
            }
        }

        private void Place(Vector3 center)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building";
            go.transform.position = center + Vector3.up * 0.5f;
            go.transform.localScale = placedScale;
            // 放置好的當實體：保留 Collider 方便之後互動
        }

        private static bool TryGetMouseWorldPoint(out Vector3 world)
        {
            world = default;
            var cam = Camera.main;
            if (!cam) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 先打有 Collider 的物件（地面 Plane）
            if (Physics.Raycast(ray, out var hit, 1000f))
            {
                world = hit.point;
                return true;
            }

            // 沒打中就打 y=0 平面
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
