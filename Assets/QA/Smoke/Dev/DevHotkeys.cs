using UnityEngine;
using UnityEngine.SceneManagement;
using QA.Smoke; // 我們先前的 RuntimeSmoke

[DefaultExecutionOrder(10001)]
public class DevHotkeys : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            var s = SceneManager.GetActiveScene().name;
            Debug.Log($"[DEV] Reload scene: {s}");
            SceneManager.LoadScene(s);
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[DEV] Run Smoke");
            RuntimeSmoke.Run();
        }
    }
}