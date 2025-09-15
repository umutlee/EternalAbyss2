// Assets/QA/Smoke/Dev/DevHotkeys.cs
using UnityEngine;

namespace QA.Smoke.Dev
{
    /// <summary>
    /// 開發用熱鍵：
    /// - F5：觸發 RuntimeSmoke（延一幀執行，避免與 Boot 時序衝突）
    /// </summary>
    public sealed class DevHotkeys : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                var smoke = FindObjectOfType<QA.Smoke.RuntimeSmoke>();
                if (smoke != null)
                {
                    smoke.RunNow(true);
                }
                else
                {
                    Debug.LogWarning("[DEV] RuntimeSmoke not found in scene. Add 'RuntimeSmoke' (e.g., under Managers) to enable F5 smoke test.");
                }
            }
        }
    }
}
