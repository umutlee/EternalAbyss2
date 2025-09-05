---
description: 架構一致性檢查規則 - 防止同類代碼分散到不同目錄
globs: ["Assets/**"]
alwaysApply: true
tags: ["architecture", "consistency", "codebuddy"]
---

# 架構一致性檢查規則

## 🚨 **強制檢查清單**

在 **Step 1 讀取快照** 時，必須額外檢查以下架構一致性問題：

### 1. **目錄重複檢查**
- 檢查是否已存在相同功能的目錄
- 例如：`Core/Config/` 與 `Game/Config/` 都放配置相關代碼
- **原則**：同類型代碼必須統一放置，避免分散

### 2. **命名空間一致性**
- 檢查新建類的命名空間是否與現有架構一致
- 例如：配置相關類應使用 `DeepAbyssHive.Core.Config`
- **原則**：相同功能使用相同命名空間

### 3. **職責邊界清晰**
- **Core/**: 基礎設施、接口、工具類、配置管理
- **Specific Systems/**: 具體業務邏輯（Creep、Terrain、Units 等）
- **QA/Smoke/Dev/**: 開發工具、測試工具
- **原則**：不要在業務系統目錄下重複建立基礎設施

### 4. **配置系統統一**
- 所有 ScriptableObject 配置類放在 `Core/Config/`
- 所有配置資產放在 `Resources/Configs/`
- 使用統一的 ConfigManager 管理
- **原則**：配置系統集中管理，避免分散

## 🎯 **Step 2 制定計畫時的強制提醒**

當發現以下情況時，**必須主動提醒用戶**：

1. **"要創建新目錄放置 XXX 類型代碼"** → 檢查是否已有相同功能目錄
2. **"新建 Config 相關類"** → 確認是否應放在 `Core/Config/`
3. **"創建 Manager 或 Service"** → 確認是否應放在 `Core/`
4. **"新建工具類或開發輔助"** → 確認是否應放在 `QA/Smoke/Dev/`

## 📋 **標準回應模板**

```
⚠️ **架構一致性警告**

發現潛在的目錄重複問題：
- 現有位置：`Assets/DeepAbyssHive/Core/Config/`
- 計畫新建：`Assets/DeepAbyssHive/Game/Config/`

建議：
- 統一放置到現有的 `Core/Config/` 目錄
- 使用一致的命名空間 `DeepAbyssHive.Core.Config`
- 避免架構債務累積

是否同意修正為統一架構？
```

## 🔧 **已知架構規範**

### 目錄結構標準
```
Assets/DeepAbyssHive/
├── Core/                    # 基礎設施
│   ├── Config/             # 所有配置相關（ScriptableObject、Manager）
│   ├── Interfaces/         # 接口定義
│   ├── Services/           # 服務層
│   ├── Managers/           # 核心管理器
│   └── Utils/              # 工具類
├── Creep/                  # 菌毯系統
├── Terrain/                # 地形系統
├── Units/                  # 單位系統
├── Buildings/              # 建築系統
└── Common/                 # 通用組件（如 Placement）
```

### 配置系統規範
- **所有 ScriptableObject**: `Core/Config/`
- **所有配置資產**: `Resources/Configs/`
- **配置管理**: 使用 `ConfigManager` 統一載入
- **命名空間**: `DeepAbyssHive.Core.Config`

## 💡 **執行原則**

1. **發現問題立即提醒**：不要等到實作完才發現架構問題
2. **建議統一方案**：提供具體的目錄調整建議
3. **保持一致性**：新代碼必須符合現有架構規範
4. **記錄決策**：重要架構決策記錄到 Neo4j Change 中

---

**記住：架構債務比技術債務更難償還！**