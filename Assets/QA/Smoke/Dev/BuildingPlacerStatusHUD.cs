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

        private GameObject _cachedPlacerGO;
        private FieldInfo _fiPlacePrefab;
        private FieldInfo _fiGroundMask;

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
            var tmp = Instantiate(prefab, hit.point, Quaternion.identity);
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

            // 畫 UI：OK/Blocked + footprint 尺寸
            string sizeTxt = $"[{(blocked ? "BLOCKED" : "OK")}] { (half*2f).x:F2} × { (half*2f).z:F2} × { (half*2f).y:F2}";
            var col = blocked ? blockedColor : okColor;
            var old = GUI.color; GUI.color = col;
            GUI.Label(new Rect(mp.x + labelOffset.x, Screen.height - mp.y + labelOffset.y, 280, 24), sizeTxt);
            GUI.color = old;

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