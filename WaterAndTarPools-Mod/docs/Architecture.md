# WaterAndTarPools architecture

KydlDev spec layout (Claude\MIGRATION_DECISIONS.md). One feature, `WaterAndTarPools`.

| Folder | Content |
|---|---|
| `src\Bootstrap` | `Plugin` (BepInEx entry, version, network rule, water bundle), `ModInstaller` (hub tabs + feature), `OldFiles` (carries files of older versions) |
| `src\Configuration` | `ModSettings` - the player config and the Tunables |
| `src\Core` | `Architecture` (marker the lab wrapper looks for), `Functions` (the IsOn rule) |
| `src\Features\WaterAndTarPools\Domain` | Pure logic without the game: `HeightField` (the liquid simulation grid) |
| `src\Features\WaterAndTarPools\Infrastructure` | Everything that touches Unity, the game or Jotunn: `Drain` (pools, deposits, walls, ledger, runoff, fish, jump), `Signs` (pool sign, menu, registry, placement, styles), `Visuals` (mesh, water shader, tint, look), `Ui` (sign menu view, sign style), `Debug` (pool probes and tests), `WtpText` |
| `src\Presentation\Hub` | The Pools page and the Settings tab in the KY hub (F6) |
| `src\Integrations\Harmony` | ALL Harmony patches (rule A5), as partials of the classes they belong to: `PoolFishPatches`, `PoolJumpPatches`, `PoolVolumeHooksPatches`, `TarPoolPatches`, `PoolMeshPatches`, `PoolWaterPatches`, `PoolSignPlacementPatches`, `PatchInstaller` |
| `src\Testing` | `InGameTests` (found by Kydl.GameProbe by name) and `TestHooks\ModTestHooks` |
| `tests` | `Unit`, `Integration`, `TestLab` (suite), `Common` |
| `assets` | The built DLL and the water asset bundle (rule A8; there is no `build\`) |
| `release\<version>` | The package that was published |

The Domain references no game or engine type. `tests\Integration\ArchitectureRulesTests.cs` enforces the layers, the network rule and the marker.