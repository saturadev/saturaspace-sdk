# Changelog

## 1.1.12 — 2026-07-06

- Version aligned with the orchestrator and engine release (update progress and durable terminal restoration). No SDK API changes.

## 1.1.11 — 2026-07-06

- `Inp` initialization no longer writes a routine diagnostic message to the Unity console.

## 1.1.10 — 2026-07-05

- Version aligned with the engine release (macOS editor reveal, graceful shutdown, single-primary relaunch). No SDK API changes.

## 1.1.9 — 2026-07-04

- `LogTdd.Clear()` now preserves `console.log` (and keeps its writer alive) while still resetting the TDD tag logs, so entering Play Mode no longer wipes the console history. The orchestrator rotates `console.log` (keeping the last 5 runs) only on editor restart.

## 1.1.8 — 2026-06-29

- Version aligned with the engine release (Windows multi-instance streaming stability). No SDK API changes.

## 1.1.7 — 2026-06-27

- Version aligned with the engine release.

## 1.1.6 — 2026-06-26

- Version aligned with the engine release (live streaming-FPS control). No SDK API changes.

## 1.1.5 — 2026-06-25

- Version aligned with the engine release. No SDK API changes.

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
