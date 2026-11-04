# Migration to the KydlDev spec (2.0.0)

- Files moved with `git mv`; namespaces unchanged (`WaterAndTarPools.Drain`, `.Signs`, `.Visuals`, `.Ui`, `.Debug`).
- The 13 Harmony patch classes moved to `src\Integrations\Harmony`: nested patches became parts of partial parents, the two placement patches stayed standalone.
- `WaterAndTarPools-Tests` moved to `WaterAndTarPools-Mod\tests`; the tests were brought to the current template (architecture rules, suite rules, settings and manifest rules).
- `Assets\` became `assets\`, `build\` was removed (rule A8), `release\` became `release\2.0.0\`.
- New: Settings tab in the KY hub, `ModInstaller` + feature, TestLab tests `hub-page` and `feature-toggle`, network strictness Patch (was Minor), DrainTarPit 2.0.0 as dependency.
- Methods with more than 7 parameters were grouped (`FloodStep`, `StrokeAmounts`, `WallGrid`); SonarQube Medium/High impact issues: none.
- Not tested in game yet; automated tests pass.