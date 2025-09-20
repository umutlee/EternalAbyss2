using UnityEngine;

namespace DeepAbyssHive.Creep.Managers
{
    public partial class CreepManager : MonoBehaviour
    {
        // 只留無參數入口，轉呼叫 Tick…(dt)
        private void Update()      => TickUpdate(UnityEngine.Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(UnityEngine.Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(UnityEngine.Time.fixedDeltaTime);
    }
}