# Changelog

## 2.2.0 — 2026-10-30

- Version aligned with the orchestrator and engine release (staging prefabs with scenario reports, a start screen that creates Unity projects, and framed screenshots for agents).
- **Staging prefabs** — derive a script from `SaturaStaging`, mark coroutines `[SaturaScenario]` and `Check` what should be true; scenarios run in Play Mode, several at once, and `Loop = true` repeats one on the same instance.
- **Scenario reports** — `Plot`, `Metric`, `Track`, `Step` and `Snapshot` on `SaturaStaging` record what a scenario does, the Staging panel draws it, and `handles.call` returns it as numbers.
- **Handle groups** — handles are grouped by the class that declares them (`Fishing/Pond/Drain`), `SaturaHandles.List(group)` shows one group, and a bare name still works while it is unique.
- **SaturaAnimator, Satura IK and cutscenes are gone** — the Tuner works on Unity's own Animator, Animation and `.anim` clips, the way Unity's Animation window does.

## 2.1.3 — 2026-10-24

- Version aligned with the orchestrator and engine release (clearer merge conflicts, Windows player and Unity 6000.3 video fixes, and animation fixes).
- SDK: `SaturaAnimator` supports manual ticking, and `SaturaCutscene` resolves track paths correctly.

## 2.1.2 — 2026-10-24

- Version aligned with the orchestrator and engine release (the Tuner tracks scene and asset changes again on Unity 6000.5 and newer).

## 2.1.1 — 2026-10-24

- Version aligned with the orchestrator and engine release (fixes a crash on launch on Windows and exec snippets and reports that failed to compile in installed builds).

## 2.1.0 — 2026-10-24

- Version aligned with the orchestrator and engine release (Drift, the guide that walks you through the app; animation and cutscene timelines in the Tuner; a live project dashboard; one `satura_exec` tool for agents; a guard that waits for a signed-in Unity Hub; and a round of Windows fixes).
- SDK: `SaturaAnimator` and `SaturaAnimatorController` play animations on a PlayableGraph, with `SaturaIK` goals applied in the same frame by an IK pass layer, humanoid IK goals as GameObjects keyed like transform curves, and Animation Rigging rigs built into the graph.
- **Handles replace scenarios** — mark a public static method with `[SaturaHandle("what it does")]` and agents call it with `handles.call(name)` in `satura_exec`, one step at a time; `TddScenario` and `TddUI` are gone.
- **Coroutine and multiplayer handles** — `handles.call` waits until a handle that returns `IEnumerator` finishes, and `{ player: 'all' }` starts a handle on every player at the same moment.
- **`LogTdd` is now `SaturaLog`** — same API and the same `.sspace/.mcp_logs` files, read with `unity.logs({ source: 'tags' })`.
- **Stopped runs say stopped** — `handles.stop(run)` now returns `status: 'stopped'` instead of an error.

## 2.0.0 — 2026-10-02

- Version aligned with the orchestrator and engine release (the Tuner panel with Unity's real inspector, a Git panel, scratchpad projects, Claude session resume and a round of Windows fixes).
- SDK: the SDK and the engine now keep `.sspace/.gitignore` set to `*`, so a project's Satura Space state never ends up in git.

## 1.1.39 — 2026-07-29

- Version aligned with the orchestrator and engine release (a second MCP server, `satura-space-assets`, that lets agents search free 3D asset stores in a hidden browser, stage downloads under `.sspace/assets/`, and import them into `Assets/ThirdParty/` with licence and attribution records; agents can also register any other asset site as a searchable store, and sign-ins and purchases always happen in a browser window the user drives).
- No SDK API changes in this release.

## 1.1.38 — 2026-07-28

- Version aligned with the orchestrator and engine release (agent git worktrees with their own headless editors, MCP sessions that freeze instead of driving a project with a stale post-update server, engine resync after an app update, a shared 48h licence grace so a long sleep no longer kills streaming, entitlement recovery after going offline, layout recovery on reveal, and Windows log/worktree hardening).
- SDK: scenarios declared inside a namespace now run when triggered by their short class name (as the app's scenario picker and agents pass them) — the runner falls back to searching all `TddScenario` subclasses by name, and reports an explicit error listing the candidates when two namespaces share a scenario name.

## 1.1.37 — 2026-07-25

- Version aligned with the orchestrator and engine release (reveal modes for the real Unity editor, pause/step controls, streamed input for legacy Input Manager and IMGUI/uGUI projects, modal-dialog detection and resolution, a boot progress strip, and a round of editor-wedge and update-safety fixes).
- SDK: scenarios fail fast instead of timing out — the runner now drives nested `yield return` enumerators itself, so an exception anywhere in the chain FAILs the scenario immediately; a stall watchdog (`StallTimeoutSeconds`, default 30s, auto-extended by timed waits) aborts a scenario that stops making progress and quotes the pending step and recent errors; and a 1s heartbeat distinguishes still-running from never-started. `LogTdd` truncates the structured console log on each Play Mode entry and at editor-session start, so a run only shows its own lines. `Inp` samples every connected keyboard and mouse rather than the current device, so a streamed session's synthetic devices no longer mask real ones, and input messages that arrive within one frame are unioned so a press and release inside a single frame still registers.

## 1.1.36 — 2026-07-16

- Version aligned with the orchestrator and engine release (the engine package is now swapped into a project atomically, so moving a project between the dev and release builds of the app can no longer leave both variants mixed in `Packages/com.saturaspace.engine` and break compilation; a deleted engine is re-injected without an app restart; relaunching agent terminals wait for the project's MCP config to be written; and Windows reveal/open/trash paths and worktree removal are hardened). No SDK API changes.

## 1.1.35 — 2026-07-16

- Version aligned with the orchestrator and engine release (the free trial is now 7 days and carries its remaining time into a subscription, PRO covers 5 machines, subscription dates read correctly during trial carryover, and find-in-files focuses its input reliably). No SDK API changes.

## 1.1.34 — 2026-07-16

- Version aligned with the orchestrator and engine release (a new multi-window model — multiple primary windows with workspaces you can move between them, and a project can no longer be opened in two windows at once; a new `unity_screenshot` MCP tool captures the game view in headless editors; more reliable clone asset sync, headless-graphics window hooking, and recovery from renderer GPU context loss; hardened multi-window moves, graceful shutdown, and access-revoked teardown). SDK: `TddUI` element ids widened to `long`.

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
