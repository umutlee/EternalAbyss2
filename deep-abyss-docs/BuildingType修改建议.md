# BuildingType枚举修改建议

## 文档信息
- **版本**: 1.0
- **日期**: 2025年8月11日
- **作者**: CodeBuddy
- **状态**: 建议

## 修改原因

根据深渊巢穴游戏设定，游戏使用的是生物能而非电力，因此BuildingType枚举中的"PowerPlant"命名与游戏设定不符。需要将其修改为更符合游戏世界观的名称。

## 当前定义

```csharp
public enum BuildingType
{
    /// <summary>
    /// 母巢 - 主要生产建筑
    /// </summary>
    Hive = 0,
    
    // ... 其他建筑类型 ...
    
    /// <summary>
    /// 电力工厂 - 产生能量
    /// </summary>
    PowerPlant = 7,
    
    // ... 其他建筑类型 ...
}
```

## 建议修改

```csharp
public enum BuildingType
{
    /// <summary>
    /// 母巢 - 主要生产建筑
    /// </summary>
    Hive = 0,
    
    // ... 其他建筑类型 ...
    
    /// <summary>
    /// 生物能核心 - 产生生物能
    /// </summary>
    BioEnergyCore = 7,
    
    // ... 其他建筑类型 ...
}
```

## 影响范围

修改BuildingType枚举可能会影响以下文件和功能：

1. **BuildingManager.cs**: 可能使用BuildingType进行建筑创建和管理
2. **BuildingTemplate.cs**: 可能使用BuildingType定义建筑模板
3. **BuildingData.cs**: 可能使用BuildingType存储建筑类型
4. **UI相关文件**: 可能使用BuildingType显示建筑信息
5. **序列化数据**: 如果有使用BuildingType的序列化数据，需要进行迁移

## 实施建议

1. 修改BuildingType枚举，将PowerPlant改为BioEnergyCore
2. 使用全局搜索找出所有使用PowerPlant的代码位置
3. 更新所有引用位置，确保使用新的BioEnergyCore名称
4. 更新相关文档和注释，确保术语一致性
5. 如有必要，编写数据迁移脚本处理已保存的游戏数据

## 任务优先级

该修改应被视为**P0**（最高优先级）任务，因为它涉及到游戏核心设定的一致性，应在当前迭代中完成。

## 预计工作量

- 修改枚举: 0.5小时
- 更新引用: 0.5小时
- 测试验证: 1小时
- 总计: 约2小时