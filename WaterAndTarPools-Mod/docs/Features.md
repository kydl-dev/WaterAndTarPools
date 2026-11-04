# Features

One feature, id `waterandtarpools` (`WaterAndTarPoolsFeature`), `IsEnabled` = `EnableAll`, DrainTarPit on, and at least one of [Pools], [Effects], [Visuals] `Enable`. `Start`/`Stop` only set the static Running flag; the section switches gate inside the working code (read every time, so they work live).

| Part | What it does | Config sections |
|---|---|---|
| Pools | Deposits of the pump become real pools (dug holes, moats, walled basins), walls, overflow, runoff, films, fish stay in, jump boost | General, Pools |
| Water | Pumping water, water styles (Game / Tropical / Lake / Ashlands), pool water shader and tint | Visuals |
| Signs | Pool sign piece, sign editor, pool registry, per-pool style per player | - |
| Hub | Pools page (F6) and Settings tab | - |
| Effects | Water splash sound, water in see-through hoses | Effects |

Deviation from the one-feature-per-concern rule: the parts share one pool registry and the same patches, so splitting would add no isolation.