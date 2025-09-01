using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    public partial class CreepData
    {
        // 允許用物件初始化指定 PlayerId
        public int PlayerId { get; set; }
        
        // 無參數建構子，供 new CreepData() / 物件初始化使用
        public CreepData() { }
        
        // 添加建構子多載
        public CreepData(float strength, float decay, int playerId)
            : this(Vector3.zero, strength, decay, playerId) {}
    }
}