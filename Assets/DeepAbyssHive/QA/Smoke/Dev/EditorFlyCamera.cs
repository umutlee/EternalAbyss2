#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Editor 專用簡易飛行相機（右鍵 + WASD/滑鼠）
/// 掛到 Main Camera 即可；僅在 Editor 生效，不影響出包
/// </summary>
[DisallowMultipleComponent]
public class EditorFlyCamera : MonoBehaviour
{
    [Header("Speed")]
    public float moveSpeed = 8f;
    public float fastMul = 3f;
    public float slowMul = 0.3f;
    public float lookSensitivity = 2f;

    private float _yaw;
    private float _pitch;

    private void OnEnable()
    {
        var euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
    }

    private void Update()
    {
        // 僅在 Editor 播放時提供；避免搶控制
        if (!Input.GetMouseButton(1)) return; // 右鍵按住才啟用
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 旋轉
        _yaw   += Input.GetAxis("Mouse X") * lookSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // 移動速度
        float mul = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) mul *= fastMul;
        if (Input.GetKey(KeyCode.LeftControl)) mul *= slowMul;
        mul *= 1f + (Input.mouseScrollDelta.y * 0.1f); // 滑輪微調

        float s = moveSpeed * mul * Time.unscaledDeltaTime;

        // WASD/Space/Ctrl
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) dir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) dir += Vector3.back;
        if (Input.GetKey(KeyCode.A)) dir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) dir += Vector3.right;
        if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftCommand)) dir += Vector3.down;

        transform.position += transform.TransformDirection(dir.normalized) * s;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
#endif