# Changelog

## 1.1.0 — 2026-06-25

- `TddScenario` multiplayer identity: `IsHost` / `PlayerIndex` / `PlayerCount` / `Role`, injected per
  instance so one scenario class can branch host vs client when a scenario is run across instances
- `TddScenario` players now self-quit once their scenario finishes (no lingering player processes)

## 1.0.0 — 2026-06-24

Initial release.

- `LogTdd` — file-backed logging
- `Inp` — input snapshot facade (New Input System)
- `TddScenario` — scripted test scenarios
- `TddUI` — uGUI/TextMeshPro automation helpers
