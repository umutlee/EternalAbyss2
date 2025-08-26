using UnityEngine;

namespace DeepAbyssHive.SpatialIndex.Data
{
    public partial class SpatialNode
    {
        // 舊代碼常用 Data → 新版若移除，這裡先提供一個可用的暫存
        public object Data { get => _compatData; set => _compatData = value; }
        private object _compatData;
    }
}