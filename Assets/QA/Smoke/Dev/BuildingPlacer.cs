using UnityEngine;
using DeepAbyssHive.Creep.Managers; // 若沒有這命名空間可刪掉這行

namespace DeepAbyssHive.Dev
{
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("Basics")]
        [SerializeField] private Camera sceneCamera;                 // 不填就用 Camera.main
        [SerializeField] private LayerMask groundMask;               // 指定「Ground」Layer
        [SerializeField] private GameObject placePrefab;             // 要放置的預製物
        [SerializeField] private Material previewMaterial;           // 預覽用透明材質（可空）

        [Header("Footprint / Grid")]
        [SerializeField, Min(1)] private int footprintSize = 1;      // 格子尺寸（單位：公尺）

        [Header("Preview")]
        [SerializeField] private float previewHeight = 0.5f;         // 預覽抬高一點避免穿地
        [SerializeField] private Vector3 placedScale = Vector3.one;  // 放下去後的縮放
        [SerializeField] private bool requireCreep = false;          // 勾選時，只允許在菌毯上放置（IsOnCreep）

        [SerializeField] private KeyCode toggleKey = KeyCode.B;
        [SerializeField] private KeyCode rotateCWKey = KeyCode.E;
        [SerializeField] private KeyCode rotateCCWKey = KeyCode.Q;
        [SerializeField] private KeyCode cancelKeyPrimary = KeyCode.Escape;
        [SerializeField] private KeyCode cancelKeyAlt = KeyCode.C;
        [SerializeField] private float rotateStep = 90f;


        private GameObject previewInstance;
        private Material previewRuntimeMat;
        private Quaternion rotation = Quaternion.identity;
        private bool isPlacing;
        private Vector3 lastValidPos;

        void Awake()
        {
            if (!sceneCamera) sceneCamera = Camera.main;
            if (!previewMaterial)
            {
                // 動態做一個半透明材質（Standard/Fade）
                var mat = new Material(Shader.Find("Standard"));
                var col = new Color(0f, 1f, 0f, 0.35f);
                mat.SetColor("_Color", col);
                mat.SetFloat("_Mode", 2); // Fade
                mat.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial = mat;
            }
        }

        void Update()
        {
            // 進出建築模式
            if (Input.GetKeyDown(toggleKey))
                TogglePlacing();

            if (!isPlacing || !placePrefab || !sceneCamera)
                return;

            // 旋轉（Shift 變成 15° 微調）＋滑鼠滾輪也可旋轉
            float step = Input.GetKey(KeyCode.LeftShift) ? 15f : rotateStep;
            if (Input.GetKeyDown(rotateCCWKey)) { rotation *= Quaternion.Euler(0, -step, 0); Debug.Log($"[Placer] Rotate -{step}"); }
            if (Input.GetKeyDown(rotateCWKey))  { rotation *= Quaternion.Euler(0,  step, 0); Debug.Log($"[Placer] Rotate +{step}"); }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)       { rotation *= Quaternion.Euler(0, wheel * step, 0); }

            // 射線打地面（Ground Mask）
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f, groundMask))
            {
                var p = hit.point;
                int cell = Mathf.Max(1, footprintSize);
                p.x = Mathf.Floor(p.x / cell) * cell + cell * 0.5f;
                p.z = Mathf.Floor(p.z / cell) * cell + cell * 0.5f;
                p.y = hit.point.y + previewHeight;

                EnsurePreview();
                previewInstance.transform.SetPositionAndRotation(p, rotation);
                lastValidPos = p;

                SetPreviewTint(new Color(0f, 1f, 0f, 0.35f)); // 先不檢查菌毯就綠色
                if (Input.GetMouseButtonDown(0))
                    PlaceNow();
            }

            // 取消（右鍵 或 Esc 或 C）
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(cancelKeyPrimary) || Input.GetKeyDown(cancelKeyAlt))
            {
                CancelPlacing();
                Debug.Log("[Placer] Cancel placing");
            }
        }

        private void TogglePlacing()
        {
            isPlacing = !isPlacing;
            if (!isPlacing) DestroyPreview();
        }

        private void EnsurePreview()
        {
            if (previewInstance) return;

            previewInstance = Instantiate(placePrefab);
            previewInstance.name = "[Preview] " + placePrefab.name;
            previewInstance.transform.localScale = placedScale;

            // 關碰撞 + 套透明材質
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            foreach (var r in previewInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (!previewRuntimeMat)
                    previewRuntimeMat = new Material(previewMaterial); // 各自一份，改色不影響資產
                r.sharedMaterial = previewRuntimeMat;
            }
        }

        private void SetPreviewTint(Color c)
        {
            if (previewRuntimeMat)
                previewRuntimeMat.color = c;
        }

        private void PlaceNow()
        {
            var pos = lastValidPos;
            pos.y -= previewHeight; // 放回地面

            var go = Instantiate(placePrefab, pos, rotation);
            go.transform.localScale = placedScale;
        }

        private void CancelPlacing()
        {
            isPlacing = false;
            DestroyPreview();
        }

        private void DestroyPreview()
        {
            if (previewInstance) Destroy(previewInstance);
            previewInstance = null;
            if (previewRuntimeMat) Destroy(previewRuntimeMat);
            previewRuntimeMat = null;
        }
    }
}
