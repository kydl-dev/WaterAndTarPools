# Test strategy

Unit (100): the liquid simulation (`HeightField`), runoff maths, sign style, Tunable contract, `Functions`. Integration (42): architecture rules, settings rules, game-field and Harmony patch targets (checked against the game DLLs without the game), manifest / changelog, in-game test contract, TestLab suite rules.

TestLab suite `WaterAndTarPools`: startup-clean, hub-page, feature-toggle. Release gate: `dotnet test` (Unit + Integration), TestLab green, SonarQube without Medium/High impact issues. See `tests\COVERAGE.md`.