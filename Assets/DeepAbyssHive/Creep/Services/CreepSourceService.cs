using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using CreepSourceType = DeepAbyssHive.Creep.Data.CreepSourceType;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯源点服务实现
    /// 负责菌毯源点的创建、管理和维护
    /// </summary>
    public class CreepSourceService : ICreepSourceService, IService
    {
        #region 私有字段

        private readonly Dictionary<int, CreepSource> _creepSources;
        private readonly Dictionary<int, List<int>> _playerSources; // 玩家ID -> 源点ID列表
        private int _nextSourceId = 1;

        #endregion

        #region 属性

        public string ServiceName => "CreepSourceService";
        public bool IsInitialized { get; private set; }

        #endregion

        #region 构造函数

        // public CreepSourceService()
        public CreepSourceService()
        {
            _creepSources = new Dictionary<int, CreepSource>();
            _playerSources = new Dictionary<int, List<int>>();
        }

        // 舊代碼可能用不同建構子簽名，這裡提供兼容
        public CreepSourceService(object _ignored) : this() {}

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;

            _creepSources.Clear();
            _playerSources.Clear();
            _nextSourceId = 1;
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            _creepSources.Clear();
            _playerSources.Clear();
            
            IsInitialized = false;
        }

        #endregion

        #region ICreepSourceService 实现

        public int CreateCreepSource(Vector3 position, int playerId, CreepSourceType sourceType, float strength = 1.0f)
        {
            int sourceId = _nextSourceId++;
            
            var source = new CreepSource();
            source.SourceId = sourceId;
            source.Position = position;
            source.Type = sourceType;
            source.Strength = Mathf.Clamp01(strength);
            source.IsActive = true;
            source.CreationTime = Time.time;
            source.Radius = CalculateInfluenceRadius(sourceType, strength);
            source.NetworkId = 0;

            _creepSources[sourceId] = source;

            // 添加到玩家源点列表
            if (!_playerSources.ContainsKey(playerId))
            {
                _playerSources[playerId] = new List<int>();
            }
            _playerSources[playerId].Add(sourceId);

            return sourceId;
        }

        public bool RemoveCreepSource(int sourceId)
        {
            if (!_creepSources.TryGetValue(sourceId, out CreepSource source))
                return false;

            _creepSources.Remove(sourceId);

            // 从玩家源点列表中移除
            if (_playerSources.TryGetValue(source.NetworkId, out List<int> playerSourceList))
            {
                playerSourceList.Remove(sourceId);
                if (playerSourceList.Count == 0)
                {
                    _playerSources.Remove(source.NetworkId);
                }
            }

            return true;
        }

        public CreepSource GetCreepSource(int sourceId)
        {
            _creepSources.TryGetValue(sourceId, out CreepSource source);
            return source;
        }

        public NativeArray<CreepSource> GetPlayerCreepSources(int playerId)
        {
            if (!_playerSources.TryGetValue(playerId, out List<int> sourceIds))
            {
                return new NativeArray<CreepSource>(0, Allocator.Temp);
            }

            var sources = new NativeArray<CreepSource>(sourceIds.Count, Allocator.Temp);
            for (int i = 0; i < sourceIds.Count; i++)
            {
                sources[i] = _creepSources[sourceIds[i]];
            }

            return sources;
        }

        public NativeArray<CreepSource> GetCreepSourcesInRange(Vector3 center, float radius, int playerId = -1)
        {
            var sourcesInRange = new List<CreepSource>();

            foreach (var source in _creepSources.Values)
            {
                if (playerId != -1 && source.NetworkId != playerId)
                    continue;

                float distance = Vector3.Distance(center, source.Position);
                if (distance <= radius)
                {
                    sourcesInRange.Add(source);
                }
            }

            var result = new NativeArray<CreepSource>(sourcesInRange.Count, Allocator.Temp);
            for (int i = 0; i < sourcesInRange.Count; i++)
            {
                result[i] = sourcesInRange[i];
            }

            return result;
        }

        public CreepSource GetNearestCreepSource(Vector3 position, int playerId = -1, float maxDistance = float.MaxValue)
        {
            CreepSource nearestSource = default;
            float nearestDistance = maxDistance;

            foreach (var source in _creepSources.Values)
            {
                if (playerId != -1 && source.PlayerId != playerId)
                    continue;

                if (!source.IsActive)
                    continue;

                float distance = Vector3.Distance(position, source.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSource = source;
                }
            }

            return nearestSource;
        }

        public void UpdateSourceStrength(int sourceId, float strength)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                source.Strength = Mathf.Clamp01(strength);
                source.InfluenceRadius = CalculateInfluenceRadius(source.Type, source.Strength);
                source.LastUpdateTime = Time.time;
                _creepSources[sourceId] = source;
            }
        }

        public void UpdateSourceType(int sourceId, CreepSourceType sourceType)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                source.Type = sourceType;
                source.InfluenceRadius = CalculateInfluenceRadius(sourceType, source.Strength);
                source.LastUpdateTime = Time.time;
                _creepSources[sourceId] = source;
            }
        }

        public void ActivateSource(int sourceId)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                source.IsActive = true;
                source.LastUpdateTime = Time.time;
                _creepSources[sourceId] = source;
            }
        }

        public void DeactivateSource(int sourceId)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                source.IsActive = false;
                source.LastUpdateTime = Time.time;
                _creepSources[sourceId] = source;
            }
        }

        public bool IsSourceActive(int sourceId)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                return source.IsActive;
            }
            return false;
        }

        public int MergeCreepSources(int sourceId1, int sourceId2)
        {
            if (!_creepSources.TryGetValue(sourceId1, out CreepSource source1) ||
                !_creepSources.TryGetValue(sourceId2, out CreepSource source2))
            {
                return -1;
            }

            // 计算合并后的位置（加权平均）
            float totalStrength = source1.Strength + source2.Strength;
            Vector3 mergedPosition = (source1.Position * source1.Strength + source2.Position * source2.Strength) / totalStrength;

            // 创建新的合并源点
            CreepSourceType mergedType = (CreepSourceType)Mathf.Max((int)source1.Type, (int)source2.Type);
            int mergedSourceId = CreateCreepSource(mergedPosition, source1.NetworkId, mergedType, totalStrength);

            // 移除原来的源点
            RemoveCreepSource(sourceId1);
            RemoveCreepSource(sourceId2);

            return mergedSourceId;
        }

        public int SplitCreepSource(int sourceId, Vector3 newPosition, float strengthRatio = 0.5f)
        {
            if (!_creepSources.TryGetValue(sourceId, out CreepSource originalSource))
                return -1;

            strengthRatio = Mathf.Clamp01(strengthRatio);
            float newStrength = originalSource.Strength * strengthRatio;
            float remainingStrength = originalSource.Strength * (1f - strengthRatio);

            // 创建新源点
            int newSourceId = CreateCreepSource(newPosition, originalSource.NetworkId, originalSource.Type, newStrength);

            // 更新原源点强度
            UpdateSourceStrength(sourceId, remainingStrength);

            return newSourceId;
        }

        public void UpdateSources(float deltaTime)
        {
            if (!IsInitialized)
                return;

            // 更新所有活跃源点
            var sourcesToUpdate = new List<int>();
            foreach (var kvp in _creepSources)
            {
                if (kvp.Value.IsActive)
                {
                    sourcesToUpdate.Add(kvp.Key);
                }
            }

            foreach (int sourceId in sourcesToUpdate)
            {
                UpdateSourceLogic(sourceId, deltaTime);
            }
        }

        public float GetSourceInfluenceRadius(int sourceId)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                return source.InfluenceRadius;
            }
            return 0f;
        }

        public bool IsPositionInSourceInfluence(Vector3 position, int sourceId)
        {
            if (_creepSources.TryGetValue(sourceId, out CreepSource source))
            {
                float distance = Vector3.Distance(position, source.Position);
                return distance <= source.InfluenceRadius;
            }
            return false;
        }

        public CreepSourceStatistics GetSourceStatistics(int playerId)
        {
            var stats = new CreepSourceStatistics();

            if (!_playerSources.TryGetValue(playerId, out List<int> sourceIds))
            {
                return stats;
            }

            float totalStrength = 0f;
            float totalInfluenceArea = 0f;

            foreach (int sourceId in sourceIds)
            {
                if (_creepSources.TryGetValue(sourceId, out CreepSource source))
                {
                    stats.TotalSources++;
                    
                    if (source.IsActive)
                    {
                        stats.ActiveSources++;
                        totalStrength += source.Strength;
                        totalInfluenceArea += Mathf.PI * source.InfluenceRadius * source.InfluenceRadius;
                    }
                    else
                    {
                        stats.InactiveSources++;
                    }

                    // 按类型统计
                    switch ((int)source.Type)
                    {
                        case (int)DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Basic:
                            stats.SourcesByType_Basic++;
                            break;
                        case (int)DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Enhanced:
                            stats.SourcesByType_Enhanced++;
                            break;
                        case (int)DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Specialized:
                            stats.SourcesByType_Specialized++;
                            break;
                    }
                }
            }

            stats.TotalStrength = totalStrength;
            stats.AverageStrength = stats.ActiveSources > 0 ? totalStrength / stats.ActiveSources : 0f;
            stats.TotalInfluenceArea = totalInfluenceArea;

            return stats;
        }

        public void CleanupInvalidSources()
        {
            var invalidSources = new List<int>();

            foreach (var kvp in _creepSources)
            {
                var source = kvp.Value;
                
                // 检查源点是否有效（例如强度过低、长时间未更新等）
                if (source.Strength <= 0f || 
                    (!source.IsActive && Time.time - source.LastUpdateTime > 300f)) // 5分钟未更新的非活跃源点
                {
                    invalidSources.Add(kvp.Key);
                }
            }

            foreach (int sourceId in invalidSources)
            {
                RemoveCreepSource(sourceId);
            }
        }

        #endregion

        #region 私有方法

        private float CalculateInfluenceRadius(CreepSourceType sourceType, float strength)
        {
            float baseRadius = 10f;
            
            switch ((int)sourceType)
            {
                case (int)DeepAbyssHive.Creep.Data.CreepSourceTypeCompat.Basic:
                    baseRadius = 10f;
                    break;
                case (int)DeepAbyssHive.Creep.Data.CreepSourceTypeCompat.Enhanced:
                    baseRadius = 15f;
                    break;
                case (int)DeepAbyssHive.Creep.Data.CreepSourceTypeCompat.Specialized:
                    baseRadius = 20f;
                    break;
            }

            return baseRadius * strength;
        }

        private void UpdateSourceLogic(int sourceId, float deltaTime)
        {
            if (!_creepSources.TryGetValue(sourceId, out CreepSource source))
                return;

            // 更新源点逻辑（例如强度衰减、影响范围变化等）
            source.LastUpdateTime = Time.time;
            
            // 这里可以添加更复杂的源点更新逻辑
            // 例如：根据周围菌毯密度调整强度、检查资源消耗等
            
            _creepSources[sourceId] = source;
        }

        #endregion
    }
}