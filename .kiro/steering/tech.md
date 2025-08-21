# Technology Stack & Build System

## Unity Engine
- **Version**: Unity 2022.3.62f1 LTS
- **Render Pipeline**: Universal Render Pipeline (URP) 14.0.12
- **Target Platforms**: PC (primary), with mobile/web potential

## Key Unity Packages
- **Core**: TextMeshPro, Timeline, Visual Scripting
- **Development**: Test Framework, IDE integrations (Rider, VS, VSCode)
- **Custom**: NuGet for Unity, Unity MCP Bridge
- **Rendering**: URP with custom shaders and effects

## .NET & C# Dependencies
- **Microsoft.CodeAnalysis**: 4.14.0 (C# code analysis and generation)
- **System.Collections.Immutable**: 9.0.0
- **System.Reflection.Metadata**: 9.0.0
- **Runtime**: .NET Standard 2.1 compatible

## Documentation Server
- **Runtime**: Node.js with Express
- **Database**: Neo4j for project knowledge graph
- **Dependencies**: neo4j-driver 5.28.1
- **Development**: nodemon for hot reload

## Architecture Pattern
The project follows a **Service-Oriented Architecture** with:
- Manager classes as orchestrators
- Service interfaces for business logic
- Dependency injection for loose coupling
- Configuration externalization via ScriptableObjects

## Common Commands

### Unity Development
```bash
# Open Unity project (from project root)
open "Eternal Abyss 2.code-workspace"

# Unity command line build (if needed)
/Applications/Unity/Hub/Editor/2022.3.62f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget StandaloneOSX
```

### Documentation Server
```bash
# Start documentation server
npm start

# Development with auto-reload
npm run dev

# Install dependencies
npm install
```

### Project Management
```bash
# View project structure
find Assets/DeepAbyssHive -type f -name "*.cs" | head -20

# Search for specific patterns
grep -r "IService" Assets/DeepAbyssHive/

# Check Unity package dependencies
cat Packages/manifest.json
```

## Performance Considerations
- Use Burst Compiler for performance-critical code
- Implement LOD systems for large-scale rendering
- Utilize Addressable Assets for memory management
- Consider Unity Job System for multi-threading