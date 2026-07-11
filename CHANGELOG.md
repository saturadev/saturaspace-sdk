# Changelog

## 1.1.23 — 2026-07-12

- Version aligned with the orchestrator release (app updates now restart without killing your Unity editors, agents, or terminals, and the license state is cached offline). No engine or SDK changes.

## 1.1.22 — 2026-07-11

- Version aligned with the orchestrator and engine release (hot reload now patches any user runtime assembly — asmdefs and embedded packages, not just Assembly-CSharp — plus MCP scene-edit preservation and the new `unity_tdd_logs` tool). No SDK API changes.

## 1.1.21 — 2026-07-11

- Version aligned with the orchestrator release (the bundled MCP binary now auto-rebuilds when it goes stale relative to the engine sources, so packaged builds can't ship a mismatched MCP). No engine or SDK changes.

## 1.1.20 — 2026-07-10

- Version aligned with the orchestrator release (shutdown and quit-confirmation reliability). No engine or SDK changes.

## 1.1.19 — 2026-07-09

- Version aligned with the orchestrator release (worktree create/remove reliability and start-screen refresh). No engine or SDK changes.

## 1.1.18 — 2026-07-09

- Version aligned with the orchestrator release (error reporting is now fully anonymous). No engine or SDK changes.

## 1.1.17 — 2026-07-09

- Version aligned with the orchestrator release (workspace overlay and resource-popover fixes). No engine or SDK changes.

## 1.1.16 — 2026-07-09

- Version aligned with the engine release (all editor operations now run through a single strict-FIFO command queue, so a Stop issued during a compile can no longer wedge the editor and compile errors are no longer lost across a domain reload). No SDK API changes.

## 1.1.15 — 2026-07-07

- Version aligned with the orchestrator release (packaged-app terminal spawn fix — node-pty `spawn-helper` path no longer double-unpacks). No SDK API changes.

## 1.1.14 — 2026-07-07

- Version aligned with the orchestrator release (packaged-app terminal daemon fix). No SDK API changes.

## 1.1.13 — 2026-07-07

- Version aligned with the orchestrator release (terminal daemon rework and Claude Code multiaccount switching). No SDK API changes.

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
