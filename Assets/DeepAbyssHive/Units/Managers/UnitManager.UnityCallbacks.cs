using UnityEngine;

namespace DeepAbyssHive.Units.Managers
{
    public partial class UnitManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(Time.fixedDeltaTime);
    }
}