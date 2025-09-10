using UnityEngine;
using DeepAbyssHive.Core.Config;

/// <summary>
/// [EA-DEV|2025-09-10] RMB 鎖游標的「守門員」：
/// - 當 GameConfig.rmbLocksCursor = false 時，禁止右鍵鎖游標。
/// - 當偵測到建造預覽存在（~PlacementOutline）時，一律解除鎖，避免建造操作被搶焦點。
/// 放在 Dev Helper 或 Managers 上即可；不需要改 MouseCrosshairHUD 的原始碼。
/// </summary>
public class RmbLockGuard : MonoBehaviour
{
    private float _findOutlineTimer;
    private bool _hasOutline;
    private bool _allowLockCached;
    private float _lastWarnAt;

    void Update()
    {
        // 1) 基於 GameConfig 的總開關
        bool allowLock = false;
        var cfg = GameConfigProvider.Current;
        if (cfg != null) allowLock = cfg.rmbLocksCursor;

        // 2) 若有建造預覽（~PlacementOutline），一律禁鎖（避免建造被搶焦點）
        _findOutlineTimer += Time.unscaledDeltaTime;
        if (_findOutlineTimer >= 0.25f)
        {
            _findOutlineTimer = 0f;
            var go = GameObject.Find("~PlacementOutline");
            _hasOutline = (go != null && go.activeInHierarchy);
        }
        if (_hasOutline) allowLock = false;
        _allowLockCached = allowLock;

        // 3) 不允許鎖時，立即解鎖（無條件，不看按鍵）
        if (!allowLock) EnforceUnlocked();
    }

    void LateUpdate()
    {
        // 有些腳本在 LateUpdate 才鎖，我們再保險解一次
        if (!_allowLockCached) EnforceUnlocked();
    }

    private void EnforceUnlocked()
    {
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // 節流提示：若你依然看到這行，代表有其他腳本在重鎖（請檢查相機/十字準心）。
            if (Time.unscaledTime - _lastWarnAt > 1f)
            {
                _lastWarnAt = Time.unscaledTime;
                Debug.Log("[RmbLockGuard] Forced unlock (another script tried to lock cursor).");
            }
        }
    }
}