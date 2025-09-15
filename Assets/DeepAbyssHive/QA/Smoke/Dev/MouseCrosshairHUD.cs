using UnityEngine;

namespace QA.Smoke.Dev
{
    public class MouseCrosshairHUD : MonoBehaviour
    {
        [Tooltip("Raycast 命中層（預設 Terrain）")]
        public LayerMask rayMask = 0;

        [Tooltip("是否顯示座標文字")]
        public bool showCoords = true;

        [Tooltip("是否由本 HUD 管理游標鎖定/可見")]
        public bool manageCursorLock = true;

        [Tooltip("僅在按住滑鼠右鍵時鎖定游標")]
        public bool lockOnlyWhileRMB = true;

        private Vector3 _hitPoint;
        private bool _hit;

        void Reset()
        {
            // 預設只打 Terrain 層；若無 Terrain 則打全部
            int terrain = LayerMask.NameToLayer("Terrain");
            rayMask = (terrain == -1) ? ~0 : (1 << terrain);
        }

        void Update()
        {
            // —— 最小修補：管理游標鎖定 —— 
            if (manageCursorLock)
            {
                bool rmb = Input.GetMouseButton(1);
                if (lockOnlyWhileRMB && !rmb)
                {
                    if (Cursor.lockState != CursorLockMode.None)
                        Cursor.lockState = CursorLockMode.None;
                    if (!Cursor.visible)
                        Cursor.visible = true;
                }
                else if (rmb)
                {
                    if (Cursor.lockState != CursorLockMode.Locked)
                        Cursor.lockState = CursorLockMode.Locked;
                    if (Cursor.visible)
                        Cursor.visible = false;
                }
            }

            var cam = Camera.main;
            if (!cam) return;
            // 游標被鎖時，Input.mousePosition 經常落在中心；這裡保持行為直覺：
            Vector3 mp = (Cursor.lockState == CursorLockMode.Locked)
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(mp);
            _hit = Physics.Raycast(ray, out var hit, 10000f, rayMask, QueryTriggerInteraction.Ignore);
            if (_hit) _hitPoint = hit.point;
        }

        void OnGUI()
        {
            // 與上面一致：若鎖定則畫在螢幕中心
            Vector3 mp = (Cursor.lockState == CursorLockMode.Locked)
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;
            float x = mp.x, y = Screen.height - mp.y;

            // 十字準星
            DrawLine(x - 6, y, x + 6, y);
            DrawLine(x, y - 6, x, y + 6);

            if (showCoords)
            {
                string txt = _hit ? $"hit: { _hitPoint.x:F1}, { _hitPoint.y:F1}, { _hitPoint.z:F1}" : "hit: --";
                GUI.Label(new Rect(x + 10, y + 10, 200, 22), txt);
            }
        }

        private void DrawLine(float x1, float y1, float x2, float y2, int thickness = 2)
        {
            var rect = new Rect(Mathf.Min(x1, x2), Mathf.Min(y1, y2),
                                Mathf.Max(1f, Mathf.Abs(x2 - x1)), Mathf.Max(1f, Mathf.Abs(y2 - y1)));
            var oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}