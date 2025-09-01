using UnityEngine;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager Unity 生命周期回調
    /// 統一管理所有 Unity 生命周期方法，避免參數衝突
    /// </summary>
    public partial class BuildingManager : MonoBehaviour
    {
        private void Update()      => TickUpdate(Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(Time.fixedDeltaTime);
        
        // 添加缺少的 Tick 方法
        public void TickLateUpdate(float deltaTime)
        {
            // 后更新逻辑
        }
        
        public void TickFixedUpdate(float fixedDeltaTime)
        {
            // 固定更新逻辑
        }
    }
}