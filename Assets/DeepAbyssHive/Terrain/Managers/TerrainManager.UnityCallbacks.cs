using UnityEngine;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// TerrainManager Unity 生命周期回調
    /// 統一管理所有 Unity 生命周期方法，避免參數衝突
    /// </summary>
    public partial class TerrainManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(Time.fixedDeltaTime);
    }
}