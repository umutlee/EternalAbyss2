using UnityEngine;
using DeepAbyssHive.Creep.Managers;

[DefaultExecutionOrder(10000)]
public class DevHUD : MonoBehaviour
{
    private CreepManager _creep;
    private string _status = "Init...";
    private Rect _rect;

    void Start()
    {
        _rect = HudDragUtil.GetRect("HUD.DevHUD", new Rect(10, 10, 480, 120));
        _creep = FindAnyObjectByType<CreepManager>(FindObjectsInactive.Include);
        _status = _creep ? "CreepManager: OK" : "CreepManager: MISSING";
        Debug.Log($"[DEV HUD] {_status}");
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        var titleStyle = new GUIStyle(style) { richText = true, fontSize = 16 };
        
        _rect = HudDragUtil.DraggableWindow("HUD.DevHUD", _rect, "Dev Playground HUD", () =>
        {
            GUILayout.Label("<b>Dev Playground HUD</b>", titleStyle);
            GUILayout.Space(4);
            GUILayout.Label(_status, style);
            GUILayout.Label("F1: Re-log manager status | F9: Run Smoke | R: Reload scene", style);
        });
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