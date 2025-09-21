# Deep Abyss Hive — Dev Runbook (Short)

## Boot & Logs
- Play boot: Managers root -> CreepManager | UnitManager | SpatialIndexManager | TerrainManager.
- Smart Console categories: BOOT/CONFIG/HEALTH/... with toggle/solo/search.
- GameConfigProvider prints one-line summary on load.

## Hotkeys (per GameConfig)
- Build Placer: B
- Delete Building: Delete / F8
- Spawn Units: F6
- Batch Dispatch: F10
- RMB camera lock: GameConfig.rmbLock

## Common Issues
- Cannot delete building -> prefab must be on Building layer; placer enforces as safety.
- F6 not working -> ensure UnitDevSpawner enabled and spawnKey matches GameConfig.
- Preview mismatch -> use Placement Trace to compare ray/spacing/decision.
- Creep brush not working -> ensure CreepManager enabled, requireCreep, runner update, physics layers.

## Tests / CI
- Local: Window -> General -> Test Runner -> PlayMode -> run SmokePlayModeTests.
- CI: PR runs "PR: PlayMode (Headless)"; merge to main triggers "Main: Build & Gate".
- Unity License: use .ulf Base64 in UNITY_LICENSE (no need for email/password).

## Build & First-Launch Health
- Use "Main: Build & Gate" pipeline; first launch health check prints system/graphics/quality info (HEALTH/CONFIG).