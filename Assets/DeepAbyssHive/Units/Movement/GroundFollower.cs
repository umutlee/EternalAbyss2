using UnityEngine;

namespace DeepAbyssHive.Units.Movement
{
    /// <summary>掛在單位上，使其由 GroundingManager 管理貼地。</summary>
    [DisallowMultipleComponent]
    public class GroundFollower : MonoBehaviour
    {
        private void OnEnable()  => GroundingManager.Register(this);
        private void OnDisable() => GroundingManager.Unregister(this);
    }
}