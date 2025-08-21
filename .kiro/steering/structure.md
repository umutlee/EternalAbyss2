# Project Organization & Folder Structure

## Root Structure
```
Eternal Abyss 2/
├── Assets/                          # Unity assets
├── deep-abyss-docs/                # Project documentation
├── .kiro/                          # Kiro IDE configuration
├── .codebuddy/                     # CodeBuddy AI rules
├── Packages/                       # Unity package dependencies
├── ProjectSettings/                # Unity project settings
├── Library/                        # Unity generated files (ignored)
└── Documentation server files      # Node.js server for docs
```

## Core Game Code Structure
The main game code follows a modular, service-oriented architecture:

```
Assets/DeepAbyssHive/
├── Core/                           # Core game systems
│   ├── Managers/                   # Manager orchestrators
│   ├── Services/                   # Business logic services
│   ├── Interfaces/                 # Service contracts
│   └── Config/                     # Configuration objects
├── Buildings/                      # Building system
├── Units/                          # Unit management
├── Creep/                          # Creep expansion system
├── Terrain/                        # Terrain & map management
├── SpatialIndex/                   # Spatial indexing for performance
└── Tests/                          # Unit tests
```

## Service Architecture Pattern
Each module follows consistent organization:
- **Managers/**: Orchestration and Unity MonoBehaviour integration
- **Services/**: Pure C# business logic, testable
- **Interfaces/**: Service contracts (IService pattern)
- **Data/**: Data structures and DTOs
- **Config/**: ScriptableObject configurations
- **Enums/**: Type definitions

## External Dependencies
```
Assets/RTS Engine/                  # Third-party RTS framework
Assets/TextMesh Pro/               # UI text rendering
Assets/Settings/                   # URP and rendering settings
Assets/Resources/                  # Runtime-loaded assets
```

## Documentation Structure
```
deep-abyss-docs/
├── 深渊巢穴项目开发蓝图_v2.0.md      # Master development plan
├── 深渊巢穴RTS游戏需求文档-*.md      # Requirements (A-L classes)
├── 建筑系统设计文档.md              # Building system design
├── 菌毯系统设计文档.md              # Creep system design
└── Various technical specs         # Implementation details
```

## Configuration Files
- **Packages/manifest.json**: Unity package dependencies
- **Assets/packages.config**: NuGet package references
- **package.json**: Node.js documentation server
- **.kiro/**: IDE-specific configurations and steering rules

## Naming Conventions
- **Namespaces**: `DeepAbyssHive.{Module}.{Layer}` (e.g., `DeepAbyssHive.Buildings.Services`)
- **Interfaces**: `I{ServiceName}Service` (e.g., `IBuildingQueryService`)
- **Managers**: `{Module}Manager` (e.g., `BuildingManager`)
- **Services**: `{Module}{Purpose}Service` (e.g., `BuildingConstructionService`)
- **Configs**: `{Module}Config` ScriptableObjects

## File Organization Rules
1. Keep related functionality grouped by module
2. Separate concerns: Managers orchestrate, Services implement
3. Use consistent folder structure across all modules
4. Place tests adjacent to the code they test
5. Configuration objects go in dedicated Config folders

## Development Workflow
The project is currently in **Phase 3.2** of service refactoring:
- Extracting business logic from Managers to Services
- Implementing dependency injection
- Maintaining backward compatibility during transition
- Following incremental refactoring approach (300 lines max per service)