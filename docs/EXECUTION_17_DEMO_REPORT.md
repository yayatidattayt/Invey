# Execution 17 — demo stabilization

## A — Baseline

Started clean at ab619be1e3b44e70a7aca9945b58415de164fc71 on codex/execution-16-precision-finger-tracking. Fetched refs before editing. PRs1–5 were open, unmerged and at their expected heads. Created codex/execution-17-demo-stabilization from that feature stack, not main. No reset, clean, force-push or merge. An initial branch-command request was rejected before execution because its working directory resolved to the parent folder; using the explicit verified repository path resolved it without writing to the wrong repository.

Fresh baseline: locked restore; Debug and Release builds and100 core tests each;20 worker tests; standard Windows run20261108T143112995-3d6f1a1348d34b79841f0ea6129e8d58 passed37 cases with Explorer blocked. Earlier execution evidence remains intact.

## B — Scope

Four main destinations reuse existing windows: Pocket, Workspace, Ghost Tracking and Settings. Capture, Pause, tray and Advanced Diagnostics remain accessible. Added a short dismissible guide, explicit optional demo profile, synthetic samples, launcher, visible simulated Workspace control and recorded-practice chooser. No MediaPipe rewrite, design overhaul, schema replacement, B2 or OS-wide gesture actuation.

## C — Feature inventory

[Source-inspected inventory](DEMO_FEATURE_INVENTORY.md) records entry points, evidence and hardware/remote limitations for capture, library/recovery, rename/order/Delete/Undo, cards and placement, shelf, shortcuts, Workspace, Dual, tracking, calibration, practice, Stop/recovery and diagnostics. No existing feature was removed.

## D — Demo workflow

[Quickstart](DEMO_QUICKSTART.md) and [short script](DEMO_SCRIPT.md) cover launch → Pocket → actual capture/Preview/Save → Open/Pin/Tuck → Workspace A/B → explicitly simulated Ghost → recorded tracking → simulated calibration practice → Settings → Exit/restart/persistence. Live camera stays available only for later deliberate creator Enable.

## E — Confirmed functional repairs

The actual WPF accessibility Toggle provider changed the card checkbox without applying Topmost: pinning listened only to Click. Pinning now listens to Checked/Unchecked, verified through actual toggle, unpin, repin, shelf tuck and restoration.

Previously the normal UI had no entry to the existing controlled practice sequence or logical Workspace stream. Explicitly labeled controls expose those existing bounded paths. Workspace simulation ends after about two seconds, can be canceled with the same button and stops on close. It cannot dispatch native input.

Simulated practice is labeled landmarks-only, disables its image option and never fabricates video. Switching recorded sequences resets Enable labeling. Demo-only preparation validates its flag before resolving the normal profile. Missing optional tracking does not prevent Pocket startup.

## F — Pocket verification

The supplemental route invokes actual WPF automation providers and control selection state, not store mocks. It freezes known private-desktop fixture pixels using the real GDI pipeline, chooses a keyboard region, previews and saves the actual crop, decodes a saved reference, pins/unpins/repins, tucks the actual shelf and verifies the actual native card window stays visible with WS_EX_TOPMOST set, including after restart. Two sample images plus the approved crop survive a real process restart.

Native mouse/keyboard selection, black confirmation, rename/order/Delete/Undo and recovery have fresh pre-change baseline evidence and core coverage. The post-change native suite was blocked at its initial foreground check and did not re-exercise these cases. Do not substitute the baseline for a final native PASS.

## G — Workspace verification

The supplemental route loads two actual saved PNGs into A/B. It enlarges and moves B through controls while A's position stays unchanged, invokes Fit/100% and observes the labeled logical Ghost claim and movement on B before automatic release. Earlier native two-owner30-session evidence remains historical for this revision. Live tracking is not routed to Workspace.

## H — Ghost verification

Two seeded supplemental runs observe actual local MediaPipe recorded-model output and a real WPF image preview, Stop and independently confirmed worker exit. They then select explicitly SIMULATED practice through the actual chooser, observe Ready and Held, Stop and independently verify its worker exit. Settings opens/closes through its actual controls.

Live source selection, image/finger overlays, partial pointer, separate image/pointer mirrors, preferences and guardian remain present. No real webcam was activated under Execution17. Wrist-out index continuity, physical hand accuracy and physical ownership remain experimental.

## I — Startup and remote usability

scripts/Start-Invey-Demo.ps1 resolves its own repository root, requires the Release application/dependencies and compatible project-local frameworks, checks newer source, reports missing optional Python/model assets, refuses another running Invey instance and never starts a camera. It requires no admin or global policy change. Explicit -Rebuild stops on failed locked restore/build and never substitutes an older executable.

-DemoProfile selects %LOCALAPPDATA%/Invey-Demo/Execution17. -DemoProfile -PrepareOnly prepared two synthetic references on the laptop without opening UI or tracking. Repeating preparation leaves the manifest byte-identical. Preparation without explicit demo selection is rejected before normal-profile resolution. A temporary source freshness negative control was refused; its newly created probe was removed afterward.

The launcher recognizes RDP session naming when available. Other remote software may be invisible to that check; monitor/camera and remote-shortcut caveats are documented.

## J — Performance

Supplemental fixture-to-ready timings were166.38 ms (seed73021) and193.58 ms (seed99173) in the expanded rehearsal. These are not cold app launch benchmarks: the fixture host was already running. No new physical camera FPS, motion accuracy or thermal claims are made. The verified executable starts successfully for bounded demo preparation with exit0.

## K — Debug/Release

Final pinned Debug and Release builds passed with zero compiler warnings/errors and100/100 core tests in each configuration. Locked restore passed. Detailed logs/TRX remain in ignored artifacts/execution-17. A transient test build failed on missing System.IO imports in a new diagnostic test; repaired and retained in readiness-route-build.log, with the successful rerun also retained.

## L — Worker tests

20/20 passed, no skips, including the generated native Media Foundation file fixture. No physical camera is used. Final log: artifacts/execution-17/delivery-worker.log.

## M — Native Windows acceptance

Native attempts are retained as FAIL, not converted to skipped tests. Runs144355571,144532765,144703725 and144820542 stopped at activation/foreground ownership. The richer guard identified a different foreground process; read-only inspection identified GameInputSvc. No input guard was disabled, no input was sent to the wrong process and that service was not modified.

Final standard run20261108T150105106-d999c63fe7e54c02beca6d5782dc0746 passed its closed-app cursor control, then failed initial foreground validation before scenario execution. The current full native regression is therefore NOT PASS. Do not infer that an unexecuted feature regressed, or claim native demonstration readiness from supplemental control checks.

## N — End-to-end rehearsal

Supplemental ACTUAL_WPF_CONTROLS + GDI_CAPTURE; NO_NATIVE_INPUT:
- Seed73021:20261108T150629861-8c6da1be54ca43029e59f906e798a91f — PASS.
- Seed99173:20261108T150842613-dbfd0189fbb043afa7d27abbe55857e5 — PASS.

These use actual window/control instances, WPF Invoke/Toggle providers, real capture/PNG save/decode, real model inference, explicit simulation, Settings and actual host-process exit/restart. They do not use store/model mocks. They do not establish native focus, mouse handling or remote-client shortcut compatibility. Native rehearsal remains MANUAL VERIFICATION REQUIRED.

Earlier supplemental attempts are retained:145324854 disconnected at Preview; the helper was updated to wait for a real nonempty selection before invoking Preview.145639904 reproduced the pinning issue.145835481 and150029210 passed the initial repaired route; the later two seeds expanded it with unpin/repin and independent control movement. No failure was erased.

## O — Cleanup

The existing isolated harness restores the original desktop and bounds owned process cleanup. The read-only --environment-audit returned InputDesktop=Default after testing. Both successful supplemental rehearsals verified recorded/practice worker exit and closure of the card and both application processes. Final scoped process audit is recorded with delivery. Test data remains in ignored fixture directories as evidence; no unrelated apps, Explorer or Windows services were closed.

## P — Privacy and safety

Camera startup is off. No Execution16 device permission was reused. Its durable ledger remains five released sessions totaling307999 ms. No raw webcam frames or hand-landmark histories were recorded or uploaded.

Demo samples are labeled synthetic and initialized only after explicit demo selection, outside normal and legacy libraries. Before/after SHA256 checks of the normal/legacy manifests and normal calibration/shortcut files matched (four paths; absent files remain absent). No migration or deletion of the normal library occurs in demo preparation.

## Q — Known limitations and manual pre-demo checklist

1. Launch the demo in the intended remote session and verify mouse/keyboard focus manually.
2. Open Pocket, capture harmless content outside Invey, Preview/Save/Open/Pin; tuck/reveal and verify card access.
3. Load two references in Workspace, move/resize/zoom and try the explicitly simulated Ghost control.
4. Start the recorded sequence, Stop, select simulated practice, Enable/Calibrate, then Stop.
5. Check Settings and any shortcuts used by the remote client; button navigation remains the fallback.
6. Exit and relaunch to check pinned placement on the actual remote display. Confirm tray behavior separately.
7. Use a real camera only through deliberate creator selection/Enable when physically appropriate. No physical camera claim follows from this execution.

Remote display/camera enumeration, native foreground behavior, tray/Explorer, physical tracking and release acceptance remain open. A title-bar close can hide to tray; use Exit to finish. The separate demo profile persists edits and captures, rather than resetting at each launch.

## R — GitHub delivery

Stabilization branch: codex/execution-17-demo-stabilization, stacked on PR5's feature branch. Exact implementation commit, PR and final CI are recorded during delivery below. No parent PR is merged or rewritten. Hosted CI covers build/core/worker/native-file tests, not the blocked local native rehearsal.

## S — Launch

Use [DEMO_QUICKSTART.md](DEMO_QUICKSTART.md). From the repository folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Invey-Demo.ps1 -DemoProfile
```

Verified application path: src/Pocket.App/bin/Release/net10.0-windows/Invey.exe. Script works from any folder when passed by absolute path. Omit -DemoProfile for normal data; Exit before switching profiles. All optional tracking prerequisites were present at delivery preflight.

## T — Final demo readiness

PARTIAL. A launchable camera-free build, isolated two-reference demo profile, clear guide and twice-passing actual-control rehearsal are available. Core/worker tests are clean and pinning is repaired. The final native suite is blocked at foreground ownership in this remote environment, so the creator must perform the short manual rehearsal before presenting. This is not a release-readiness, physical-camera or complete native-acceptance PASS.
