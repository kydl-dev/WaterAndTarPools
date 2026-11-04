# Dependencies

| Dependency | Version | Kind |
|---|---|---|
| BepInExPack_Valheim | 5.4.2202 | hard |
| Jotunn | 2.30.2 | hard |
| KydlCore | 1.0.0 | hard (shared UI, input lock, log, KY hub) |
| DrainTarPit | 2.0.0 | hard: the pump pours into the pools through DrainTarPit's `PoolHooks`; this mod references DrainTarPit.dll |

Network: `[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]` because pools are saved world state and the pool signs are pieces. All players need the same major.minor.patch. `InternalsVisibleTo`: KydlTools (the live water editor, never published) and the unit tests.