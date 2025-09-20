using UnityEngine;

namespace DeepAbyssHive.Units.Managers
{
    public partial class UnitManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(UnityEngine.Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(UnityEngine.Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(UnityEngine.Time.fixedDeltaTime);
    }
}