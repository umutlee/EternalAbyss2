# 建筑系统设计与 API 参考（v1.1）
版本: 1.1
日期: 2025-08-28
作者: CodeBuddy
上游版本：_archive/2025-08/建筑系统设计文档.md

变更摘要（相对 v1.0）
- 引入 BuildingState 枚举（Assets/DeepAbyssHive/Buildings/Enums/BuildingState.cs）
- 明确与 SpatialIndex 的集成点（放置校验、范围查询）
- 标注未实现项并给出最小可用返回/错误码约定

一、概述
- 建筑系统负责建筑的放置、升级、研究交互；与空间索引协同进行碰撞/范围判定

二、核心数据结构
- BuildingState: Idle/Constructing/Active/Disabled/Destroyed（示例，详见代码）
- 放置参数：position(Bounds 派生)、type、playerId

三、服务接口（当前实现状态）
- BuildingQueryService
  - GetBuildings(playerId, type): List<BuildingData>（TODO：返回空列表占位）
  - CanPlace(type, position): bool（TODO：返回 false 占位；后续返回错误码）
  - ValidateUpgrade(id): Result<UpgradeInfo>（TODO：默认 null）
- BuildingConstructionService
  - StartConstruction(templateId, position): Result<int buildingId>
  - CancelConstruction(id): Result
- ResearchService
  - GetAvailableResearches(): List<ResearchTemplate>（样例数据占位）

四、与 SpatialIndex 的接口契约
- 放置校验参考：QueryBounds(bounds) 过滤类别=Buildings/Obstacles
- 碰撞策略：bounds.Intersects(other.bounds) 为基本规则；预留可配置 margin

五、错误码与最小返回（建议）
- E_PLACE_COLLISION, E_INVALID_TYPE, E_NO_PERMISSION, E_OUT_OF_BOUNDS
- Result<T> = { ok:bool, code:string, data?:T, message?:string }

六、验证
- 单测：编辑器下对 CanPlace/StartConstruction 打桩；结合固定 Bounds 样例