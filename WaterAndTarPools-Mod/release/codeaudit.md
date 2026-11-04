# Code audit WaterAndTarPools

## unused

== WaterAndTarPools: 7 never referenced
  WaterAndTarPools/WaterAndTarPools-Mod/src/Core/Architecture.cs:4 class Architecture
  WaterAndTarPools/WaterAndTarPools-Mod/src/Features/WaterAndTarPools/Infrastructure/Drain/PoolDebug.cs:169 member EmptyPumpPoolAt
  WaterAndTarPools/WaterAndTarPools-Mod/src/Features/WaterAndTarPools/Infrastructure/Drain/TarPitStats.cs:39 member BudgetText
  WaterAndTarPools/WaterAndTarPools-Mod/src/Features/WaterAndTarPools/Infrastructure/Drain/TarPitStats.cs:51 member Stats
  WaterAndTarPools/WaterAndTarPools-Mod/src/Features/WaterAndTarPools/Infrastructure/Drain/TarPitStats.cs:79 member Nearest
  WaterAndTarPools/WaterAndTarPools-Mod/src/Features/WaterAndTarPools/Infrastructure/Visuals/PoolWater.cs:61 member ResetLook
  WaterAndTarPools/WaterAndTarPools-Mod/src/Testing/TestHooks/ModTestHooks.cs:12 class ModTestHooks

## dupes
== same name in several files
  Apply                        SignFonts.cs:77, WaterLook.cs:93, PatchInstaller.cs:15
  Build                        PoolMenu.cs:56, PoolRegistry.cs:108, PoolMesh.cs:77
  Current                      PoolWater.cs:214, WaterTint.cs:21, WaterTint.cs:24
  Destroyed                    PoolVolumeHooks.cs:52, TarPitLedger.cs:147
  Forget                       RunoffCleanup.cs:40, PoolMesh.cs:36
  Parse                        SignStyle.cs:54, WaterLook.cs:153
  Refresh                      PoolMenu.cs:138, SignFonts.cs:30, PoolMesh.cs:57
  Register                     PoolVolumeHooks.cs:31, PoolMenu.cs:23, PoolSignPiece.cs:25
  SetWorldStyle                PoolMenu.cs:115, PoolRegistry.cs:69

== identical bodies (>=5 lines)

