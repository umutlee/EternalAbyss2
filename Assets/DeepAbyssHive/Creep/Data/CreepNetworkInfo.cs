using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep
{
    /// <summary>
    /// 菌毯网络信息
    /// </summary>
    [Serializable]
    public class CreepNetworkInfo
    {
        [SerializeField] private int _networkId;
        [SerializeField] private int _ownerId;
        [SerializeField] private List<Vector2Int> _connectedTiles;
        [SerializeField] private float _totalDensity;
        [SerializeField] private float _averageDensity;
        [SerializeField] private Vector2Int _centerTile;
        [SerializeField] private int _tileCount;
        [SerializeField] private bool _isActive;

        /// <summary>
        /// 网络ID
        /// </summary>
        public int NetworkId => _networkId;

        /// <summary>
        /// 所有者ID
        /// </summary>
        public int OwnerId => _ownerId;

        /// <summary>
        /// 连接的瓦片列表
        /// </summary>
        public List<Vector2Int> ConnectedTiles => _connectedTiles;

        /// <summary>
        /// 总密度
        /// </summary>
        public float TotalDensity => _totalDensity;

        /// <summary>
        /// 平均密度
        /// </summary>
        public float AverageDensity => _averageDensity;

        /// <summary>
        /// 中心瓦片
        /// </summary>
        public Vector2Int CenterTile => _centerTile;

        /// <summary>
        /// 瓦片数量
        /// </summary>
        public int TileCount => _tileCount;

        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CreepNetworkInfo(int networkId, int ownerId)
        {
            _networkId = networkId;
            _ownerId = ownerId;
            _connectedTiles = new List<Vector2Int>();
            _totalDensity = 0f;
            _averageDensity = 0f;
            _centerTile = Vector2Int.zero;
            _tileCount = 0;
            _isActive = true;
        }

        /// <summary>
        /// 添加瓦片
        /// </summary>
        public void AddTile(Vector2Int tile, float density)
        {
            if (!_connectedTiles.Contains(tile))
            {
                _connectedTiles.Add(tile);
                _totalDensity += density;
                _tileCount = _connectedTiles.Count;
                UpdateAverageDensity();
                UpdateCenterTile();
            }
        }

        /// <summary>
        /// 移除瓦片
        /// </summary>
        public void RemoveTile(Vector2Int tile, float density)
        {
            if (_connectedTiles.Remove(tile))
            {
                _totalDensity -= density;
                _tileCount = _connectedTiles.Count;
                UpdateAverageDensity();
                UpdateCenterTile();
                
                if (_tileCount == 0)
                {
                    _isActive = false;
                }
            }
        }

        /// <summary>
        /// 更新平均密度
        /// </summary>
        private void UpdateAverageDensity()
        {
            _averageDensity = _tileCount > 0 ? _totalDensity / _tileCount : 0f;
        }

        /// <summary>
        /// 更新中心瓦片
        /// </summary>
        private void UpdateCenterTile()
        {
            if (_tileCount == 0)
            {
                _centerTile = Vector2Int.zero;
                return;
            }

            Vector2 sum = Vector2.zero;
            foreach (var tile in _connectedTiles)
            {
                sum += tile;
            }
            
            _centerTile = Vector2Int.RoundToInt(sum / _tileCount);
        }

        /// <summary>
        /// 合并其他网络
        /// </summary>
        public void MergeWith(CreepNetworkInfo other)
        {
            foreach (var tile in other._connectedTiles)
            {
                if (!_connectedTiles.Contains(tile))
                {
                    _connectedTiles.Add(tile);
                }
            }
            
            _totalDensity += other._totalDensity;
            _tileCount = _connectedTiles.Count;
            UpdateAverageDensity();
            UpdateCenterTile();
        }

        /// <summary>
        /// 检查是否包含瓦片
        /// </summary>
        public bool ContainsTile(Vector2Int tile)
        {
            return _connectedTiles.Contains(tile);
        }

        /// <summary>
        /// 获取边界瓦片
        /// </summary>
        public List<Vector2Int> GetBorderTiles()
        {
            List<Vector2Int> borderTiles = new List<Vector2Int>();
            
            foreach (var tile in _connectedTiles)
            {
                // 检查相邻瓦片是否都在网络中
                Vector2Int[] neighbors = {
                    tile + Vector2Int.up,
                    tile + Vector2Int.down,
                    tile + Vector2Int.left,
                    tile + Vector2Int.right
                };
                
                bool isBorder = false;
                foreach (var neighbor in neighbors)
                {
                    if (!_connectedTiles.Contains(neighbor))
                    {
                        isBorder = true;
                        break;
                    }
                }
                
                if (isBorder)
                {
                    borderTiles.Add(tile);
                }
            }
            
            return borderTiles;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"CreepNetwork[ID:{_networkId}, Owner:{_ownerId}, Tiles:{_tileCount}, AvgDensity:{_averageDensity:F2}]";
        }
    }
}