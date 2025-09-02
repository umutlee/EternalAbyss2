using UnityEngine;
using DeepAbyssHive.Creep.Managers;

[DefaultExecutionOrder(10000)]
public class DevHUD : MonoBehaviour
{
    private CreepManager _creep;
    private string _status = "Init...";

    void Start()
    {
        _creep = FindAnyObjectByType<CreepManager>(FindObjectsInactive.Include);
        _status = _creep ? "CreepManager: OK" : "CreepManager: MISSING";
        Debug.Log($"[DEV HUD] {_status}");
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        GUILayout.BeginArea(new Rect(10,10,480,200), GUI.skin.box);
        GUILayout.Label("<b>Dev Playground HUD</b>", new GUIStyle(style){ richText = true, fontSize = 16});
        GUILayout.Space(4);
        GUILayout.Label(_status, style);
        GUILayout.Label("F1: Re-log manager status | F9: Run Smoke | R: Reload scene", style);
        GUILayout.EndArea();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _creep = FindAnyObjectByType<CreepManager>(FindObjectsInactive.Include);
            _status = _creep ? "CreepManager: OK" : "CreepManager: MISSING";
            Debug.Log($"[DEV HUD] {_status}");
        }
    }
}