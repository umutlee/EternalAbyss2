using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    public partial class SpatialIndexManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(UnityEngine.Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(UnityEngine.Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(UnityEngine.Time.fixedDeltaTime);
    }
}