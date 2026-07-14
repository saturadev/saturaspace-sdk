# Changelog

## 1.1.33 — 2026-07-14

- Version aligned with the orchestrator release (the engine-injection completeness check now matches how the engine ships — compiled assemblies in release builds, C# sources in development — instead of always requiring the sources, which made packaged builds fail to inject the engine). No engine or SDK API changes.

## 1.1.32 — 2026-07-14

- Version aligned with the orchestrator release (interactive hidden mode was reworked on both platforms: the Windows hook is injected at the executable entry point before Unity runs any application code and a hidden launch that cannot be hooked is terminated rather than shown, macOS suppresses AppKit window ordering and exempts hidden editors from App Nap so streaming does not stall in the background, and blocking modals reveal the editor for a human answer and re-hide afterwards). No engine or SDK API changes.

## 1.1.31 — 2026-07-13

- Version aligned with the orchestrator and engine release (the engine's streamed keyboard and mouse are now removed when play stops instead of leaking one pair per play — a long-lived editor no longer accumulates hundreds of synthetic devices, which broke every keyboard binding with `NotSupportedException: Control count per binding cannot exceed byte.MaxValue=255`). No SDK API changes.

## 1.1.30 — 2026-07-13

- Version aligned with the orchestrator and engine release (multiplayer clones now reload a scene the primary rewrote instead of playing a stale in-memory copy, and hidden editors no longer surface Unity's busy-progress dialog). No SDK API changes.

## 1.1.29 — 2026-07-13

- Version aligned with the orchestrator and engine release (the hardened engine no longer renames Unity's message methods, so streamed input and the stream's own update path keep working in packaged builds; machines whose hardware id can't be read fall back to a per-install id for licensing, and the entitlement state machine locks trials and grace periods correctly even offline). No SDK API changes.

## 1.1.28 — 2026-07-13

- Version aligned with the orchestrator release (opening a project now re-pins the SDK ref and re-injects the engine, so a project opened without launching Unity — or restored after an update — no longer keeps the previous release's `com.saturaspace.sdk` ref). No engine or SDK API changes.

## 1.1.27 — 2026-07-13

- Version aligned with the orchestrator release (entitlement now fails closed: an unverified or rejected license locks the app instead of unlocking it, trials and grace periods lock the moment their window ends, and update checks respect the plan gate). No engine or SDK changes.

## 1.1.26 — 2026-07-12

- Version aligned with the orchestrator and engine release (license-issuance failures are now surfaced with subscription status in Settings, and the engine ships as merged, hardened obfuscated assemblies). No SDK API changes — the public SDK carries none of the licensing code.

## 1.1.25 — 2026-07-12

- Version aligned with the orchestrator and engine release (a 3-day free trial with server-issued, machine-locked license tokens; the engine and native components now independently verify entitlement). No SDK API changes — the public SDK carries none of the licensing code.

## 1.1.24 — 2026-07-12

- Version aligned with the orchestrator and engine release (assets an agent creates now reach running multiplayer clones without a restart, entering Play no longer flashes a spurious Compiling state, Codex and Cursor get the Unity MCP registered automatically, and reveal reliably brings the editor window to front). No SDK API changes.

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
