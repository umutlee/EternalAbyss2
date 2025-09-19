# 深渊巢穴 (Eternal Abyss 2) - CodeBuddy 开发指南

## 项目概述

这是一个Unity 2022.3 LTS游戏项目，包含一个Node.js文档服务器。项目采用中文开发，是一个深渊巢穴主题的RTS游戏，具有复杂的系统架构和模块化设计。

## 开发命令

### Unity项目
- **打开项目**: 使用Unity 2022.3.62f1打开根目录
- **构建项目**: Unity Editor → Build Settings → Build
- **运行测试**: Unity Editor → Window → General → Test Runner

### Node.js文档服务器
```bash
# 启动开发服务器
npm run dev

# 启动生产服务器  
npm start

# 安装依赖
npm install
```

## 核心架构

### 系统设计模式
项目采用高度模块化的管理器模式：

- **IManager接口**: 所有管理器都实现统一的生命周期接口
- **ServiceManager**: 提供服务注册、依赖管理和生命周期控制
- **GameManager**: 单例模式的核心游戏管理器，使用partial class分模块设计
- **配置驱动**: 所有系统都支持ScriptableObject配置文件

### 主要系统模块

#### DeepAbyssHive核心系统
- **Core/**: 核心管理器和服务系统
  - `GameManager` - 游戏主控制器（分为Core、Managers、Perf模块）
  - `ServiceManager` - 服务管理和依赖注入
  - 统一的接口定义和配置管理
- **Buildings/**: 建筑系统
- **Units/**: 单位系统  
- **Creep/**: 菌毯系统
- **SpatialIndex/**: 空间索引优化
- **Research/**: 研究系统
- **Terrain/**: 地形系统

#### 管理器实现模式
每个管理器采用partial class分模块设计：
- `Manager.Core.cs` - 核心功能和生命周期
- `Manager.Query.cs` - 查询和检索功能
- `Manager.Operations.cs` - 操作和修改功能
- `Manager.Events.cs` - 事件处理功能

### 性能优化策略
- **批量更新系统**: 限制单帧处理数量避免卡顿
- **空间索引**: 高效的空间查询和碰撞检测
- **性能监控**: ServiceManager提供实时性能统计
- **内存管理**: 完整的资源清理和对象池机制

## 文档系统

### Node.js文档服务器
port 5173的Express服务器，用于提供项目文档：
- 静态文件服务
- Markdown文件渲染
- 支持中文文档浏览

### 项目文档结构
`deep-abyss-docs/`目录包含完整的项目文档：
- 系统架构设计文档
- 技术实现指南
- 需求文档和里程碑规划
- 代码检阅和风险评估报告

## 配置系统

### 配置文件管理
- 所有配置继承`BaseConfigSO`
- 通过`ConfigManager`统一加载和管理
- 支持运行时参数调整和热更新

### 关键配置类型
- `UnitConfigSO` - 单位配置
- `BuildingConfigSO` - 建筑配置  
- `PerformanceConfig` - 性能参数配置

## 开发最佳实践

### 新系统开发
1. 实现`IManager`接口确保生命周期一致性
2. 使用partial class分模块组织代码
3. 集成配置系统支持参数调整
4. 实现完整的错误处理和性能监控

### 代码规范
- 使用中文注释和文档
- 遵循Unity C#编码规范
- 接口定义在单独的Interfaces目录
- 异常类型定义在Exceptions目录

### 调试和测试
- 所有关键操作包含异常处理
- 详细的调试日志输出
- 状态验证和边界检查
- ServiceManager提供性能监控数据

## 依赖和工具

### Unity依赖
- Unity 2022.3.62f1 LTS
- Universal Render Pipeline (URP)
- TextMesh Pro

### Node.js依赖
- Express 4.21.2
- Neo4j Driver 5.28.1
- Nodemon 2.0.22 (开发环境)

## 最近更新记录

### 2025-09-18: 建筑选择系统完整修复
- **问题解决**: 修复了 Tab/BackQuote 键不实时切换建筑预览的问题
- **编译修复**: 解决 TMPro 命名空间编译错误，在 Assembly Definition 中添加 Unity.TextMeshPro 引用
- **交互优化**: 修复建筑目录 HUD 的按钮事件处理，确保 Prev/Next/Close 按钮正常工作
- **预览同步**: 改进建筑预览同步机制，确保切换建筑时立即进入放置模式
- **用户体验**: 解决需要按两次 B 键的问题，现在 Tab/BackQuote 切换后立即可用
- **系统稳定**: 移除有问题的 uGUI 系统，恢复稳定的 IMGUI 建筑选择界面

### 核心修复文件
- `DeepAbyssHive.Runtime.asmdef` - 添加 TextMeshPro 引用
- `BuildingCatalogBinder.cs` - 添加自动进入放置模式功能
- `BuildingCatalogHUD.cs` - 修复按钮事件处理和预览刷新逻辑