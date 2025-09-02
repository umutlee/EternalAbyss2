#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public class DevTips : MonoBehaviour
{
    private string _text = "Dev Controls:\n" +
                           "B = Toggle creep at mouse\n" +
                           "N = Add cross creep at mouse\n" +
                           "LMB = Place building (must be on creep)\n" +
                           "Gizmos = On to see creep tiles";

    private void OnGUI()
    {
        GUI.Label(new Rect(10,10,400,100), _text);
    }
}
#endif
