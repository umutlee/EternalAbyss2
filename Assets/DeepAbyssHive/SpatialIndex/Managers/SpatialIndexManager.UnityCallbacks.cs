using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    public partial class SpatialIndexManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(Time.fixedDeltaTime);
    }
}