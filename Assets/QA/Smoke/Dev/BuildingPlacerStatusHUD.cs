using UnityEngine;
using System.Linq;
using System.Reflection;

namespace QA.Smoke.Dev
{
    public class BuildingPlacerStatusHUD : MonoBehaviour
    {
        [Tooltip("若未自動找到，手動指定 BuildingPlacer")]
        public MonoBehaviour placer;

        [Tooltip("沒有 BuildingPlacer 時，使用此 prefab 作為測試對象")]
        public GameObject fallbackPrefab;

        [Tooltip("擋重疊：優先用 Collider.bounds；無 Collider 才用 Renderer")]
        public bool useColliderBoundsForBlocking = true;

        [Tooltip("邊界 padding（與 BuildingPlacer 一致）。正＝更寬鬆，負＝可更靠近")]
        public float blockPadding = 0.02f;

        [Tooltip("僅命中 Terrain 層時才顯示狀態")]
        public bool requireTerrainHit = true;

        [Tooltip("顯示字樣位置偏移")]
        public Vector2 labelOffset = new Vector2(12, 18);

        [Tooltip("可放置顏色")]
        public Color okColor = new Color(0.2f, 1f, 0.2f, 1f);

        [Tooltip("不可放置顏色")]
        public Color blockedColor = new Color(1f, 0.2f, 0.2f, 1f);

        // 新增：預覽染色與框線
        [Header("Preview Visuals")]
        [Tooltip("若找到 BuildingPlacer 的 preview 物件，對其上色（綠/紅）")]
        public bool colorizePreview = true;
        [Tooltip("預覽上色透明度（0~1）")]
        [Range(0f,1f)] public float previewColorAlpha = 0.55f;
        [Tooltip("是否繪製 footprint 世界框線")]
        public bool drawFootprintOutline = true;
        [Tooltip("框線 Y 提升，避免 Z-fighting")]
        public float outlineYOffset = 0.02f;
        [Tooltip("框線寬度")]
        public float outlineWidth = 0.03f;

        private GameObject _cachedPlacerGO;
        private FieldInfo _fiPlacePrefab;
        private FieldInfo _fiGroundMask;
        private FieldInfo _fiPreviewGO;   // 可能叫 previewInstance/preview/ghost
        private FieldInfo _fiRotQuat;     // 若有 Quaternion 旋轉欄位
        private FieldInfo _fiRotYaw;      // 若用 float 儲存 y 軸角
        private LineRenderer _line;
        private Material _lineMat;

        void Awake()
        {
            if (!placer)
                placer = FindObjectsOfType<MonoBehaviour>(true).FirstOrDefault(m => m && m.GetType().Name == "BuildingPlacer");
            CacheReflection();
        }

        void CacheReflection()
        {
            if (!placer) return;
            var t = placer.GetType();
            _fiPlacePrefab = t.GetField("placePrefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _fiGroundMask  = t.GetField("groundMask",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            // 常見 preview 欄位名
            _fiPreviewGO = t.GetField("previewInstance", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                          ?? t.GetField("preview",       BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                          ?? t.GetField("ghost",         BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            // 嘗試取得旋轉
            _fiRotQuat = t.GetField("rotation", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                      ?? t.GetField("currentRotation", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                      ?? t.GetField("previewRotation", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            _fiRotYaw  = t.GetField("rotationY", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                      ?? t.GetField("yaw",        BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                      ?? t.GetField("currentYaw", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        }

        GameObject GetPrefab()
        {
            if (placer && _fiPlacePrefab != null)
            {
                var v = _fiPlacePrefab.GetValue(placer) as GameObject;
                if (v) return v;
            }
            return fallbackPrefab;
        }

        int GetGroundMaskOrDefault()
        {
            if (placer && _fiGroundMask != null)
            {
                try { return (int)_fiGroundMask.GetValue(placer); } catch {}
            }
            // 預設：只打 Terrain
            int terrain = LayerMask.NameToLayer("Terrain");
            return (terrain == -1) ? ~0 : (1 << terrain);
        }

        static Bounds EncapsulateBounds(Renderer[] rends, Collider[] cols, bool preferCollider)
        {
            if (preferCollider && cols != null && cols.Length > 0)
            {
                var b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            if (rends != null && rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                return b;
            }
            if (!preferCollider && cols != null && cols.Length > 0)
            {
                var b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            return new Bounds(Vector3.zero, Vector3.one * 0.5f);
        }

        GameObject GetPreviewGO()
        {
            if (placer && _fiPreviewGO != null)
            {
                var v = _fiPreviewGO.GetValue(placer);
                if (v is GameObject g) return g;
                if (v is Component c)  return c.gameObject;
            }
            return null;
        }

        Quaternion GetPlacementRotationOrIdentity()
        {
            if (!placer) return Quaternion.identity;
            try
            {
                if (_fiRotQuat != null)
                {
                    var v = _fiRotQuat.GetValue(placer);
                    if (v is Quaternion q) return q;
                }
                if (_fiRotYaw != null)
                {
                    var v = _fiRotYaw.GetValue(placer);
                    if (v is float yaw) return Quaternion.Euler(0f, yaw, 0f);
                }
            }
            catch {}
            return Quaternion.identity;
        }

        void EnsureLine()
        {
            if (!drawFootprintOutline) return;
            if (_line) return;
            var go = new GameObject("~PlacementOutline");
            go.hideFlags = HideFlags.DontSave;
            _line = go.AddComponent<LineRenderer>();
            _line.positionCount = 5; // 關閉矩形
            _line.loop = false;
            _line.useWorldSpace = true;
            _line.widthMultiplier = outlineWidth;
            var shader = Shader.Find("Sprites/Default");
            _lineMat = new Material(shader);
            _line.material = _lineMat;
            _line.numCornerVertices = 2;
            _line.numCapVertices = 2;
        }

        void UpdateOutline(Vector3 center, Vector3 half, Quaternion rot, Color c)
        {
            if (!drawFootprintOutline) { if (_line) _line.enabled = false; return; }
            EnsureLine();
            if (!_line) return;
            _line.enabled = true;
            _line.startColor = _line.endColor = new Color(c.r, c.g, c.b, 0.9f);
            _line.widthMultiplier = outlineWidth;
            Vector3 up = Vector3.up * outlineYOffset;
            // 以 half.x/half.z 做地表矩形，套旋轉
            Vector3 a = center + rot * new Vector3(-half.x, 0, -half.z) + up;
            Vector3 b = center + rot * new Vector3( half.x, 0, -half.z) + up;
            Vector3 d = center + rot * new Vector3( half.x, 0,  half.z) + up;
            Vector3 e = center + rot * new Vector3(-half.x, 0,  half.z) + up;
            _line.SetPosition(0, a);
            _line.SetPosition(1, b);
            _line.SetPosition(2, d);
            _line.SetPosition(3, e);
            _line.SetPosition(4, a);
        }

        void ColorizePreviewGO(GameObject preview, Color col)
        {
            if (!colorizePreview || !preview) return;
            col.a = previewColorAlpha;
            var rends = preview.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) return;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in rends)
            {
                r.GetPropertyBlock(mpb);
                // 嘗試多個常見色彩屬性
                mpb.SetColor("_BaseColor", col);
                mpb.SetColor("_Color",     col);
                mpb.SetColor("_TintColor", col);
                r.SetPropertyBlock(mpb);
            }
        }

        private Rect _rect;

        void Start()
        {
            _rect = HudDragUtil.GetRect("HUD.BuildPlacer", new Rect(10, 95, 720, 100));
        }

        void OnGUI()
        {
            var cam = Camera.main;
            if (!cam) return;

            // Raycast 到 Terrain（或 groundMask）
            Vector3 mp = (Cursor.lockState == CursorLockMode.Locked)
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;

            Ray ray = cam.ScreenPointToRay(mp);
            int rayMask = GetGroundMaskOrDefault();
            if (!Physics.Raycast(ray, out var hit, 10000f, rayMask, QueryTriggerInteraction.Ignore))
                return;

            if (requireTerrainHit)
            {
                int terrain = LayerMask.NameToLayer("Terrain");
                if (terrain != -1 && hit.collider && hit.collider.gameObject.layer != terrain)
                    return;
            }

            var prefab = GetPrefab();
            if (!prefab) return;

            // 用臨時實例（禁用 collider、ignore raycast）獲得世界 bounds
            Quaternion placeRot = GetPlacementRotationOrIdentity();
            var tmp = Instantiate(prefab, hit.point, placeRot);
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1)
            {
                foreach (var t in tmp.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = ignoreRaycast;
            }
            foreach (var c in tmp.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            var bounds = EncapsulateBounds(tmp.GetComponentsInChildren<Renderer>(true),
                                           tmp.GetComponentsInChildren<Collider>(true),
                                           useColliderBoundsForBlocking);
            var center = bounds.center;
            var half   = bounds.extents + Vector3.one * blockPadding;
            Destroy(tmp);

            // 用與 PlaceNow 相同的 blockMask 檢查
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int blockMask = ~0;
            if (terrainLayer != -1)  blockMask &= ~(1 << terrainLayer);
            if (ignoreRaycast != -1) blockMask &= ~(1 << ignoreRaycast);
            bool blocked = Physics.OverlapBox(center, half, Quaternion.identity, blockMask, QueryTriggerInteraction.Ignore).Length > 0;

            // 可拖曳狀態面板
            string sizeTxt = $"[{(blocked ? "BLOCKED" : "OK")}] { (half*2f).x:F2} × { (half*2f).z:F2} × { (half*2f).y:F2}";
            var col = blocked ? blockedColor : okColor;
            GUI.color = col;
            _rect = HudDragUtil.DraggableWindow("HUD.BuildPlacer", _rect, "Building Placer", () =>
            {
                GUILayout.Label(sizeTxt);
                GUILayout.Label($"Padding: {blockPadding:F3}");
                GUILayout.Label($"Mode: {(useColliderBoundsForBlocking ? "Collider" : "Renderer")}");
            });
            GUI.color = Color.white;

            // —— 新增：染色 preview 與世界框線 —— 
            var preview = GetPreviewGO();
            ColorizePreviewGO(preview, col);
            UpdateOutline(center, half, placeRot, col);

            // 熱鍵：列印 footprint 詳細資訊
            bool mod = Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor
                        ? Event.current != null && Event.current.shift && Event.current.command
                        : Event.current != null && Event.current.shift && Event.current.control;
            if (Event.current != null && Event.current.type == EventType.KeyDown && mod && Event.current.keyCode == KeyCode.C)
            {
                Debug.Log($"[PLACE] Footprint bounds size (W,L,H): {(half*2f).x:F3}, {(half*2f).z:F3}, {(half*2f).y:F3}  | padding={blockPadding}  (mode={(useColliderBoundsForBlocking ? "Collider" : "Renderer")})");
            }
        }
    }
}