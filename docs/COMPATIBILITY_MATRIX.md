# Windows compatibility evidence

Execution18: Windows11 build26200, pinned.NET10.0.403, available125% display, local model/persistence and WPF learning verified camera-free. Native standard38 PASS, Explorer shell case BLOCKED. No new physical accuracy or mixed-DPI claim. Camera-off startup remains valid; wrong device/source/hand/frame/mirror/display context uses original mapping. See EXECUTION_18_REPORT.md.

Execution 11 verified the recorded calibration panel on the available display, with deterministic coverage for negative desktop origins and 125% scaling. Physical mixed-DPI alignment, hand labels, two-hand detection and live practice remain open. Execution 10 verified existing Pocket.App identity and single-instance behavior, plus acquisition and release on the ASUS FHD setup. [Execution 11 evidence](EXECUTION_11_REPORT.md)

Execution 09 verified ASUS FHD webcam on Windows 11 x64 build 26200. The active reader exposed YUY2 1920 × 1080 at nominal 30 Hz and delivered RGB32 at about 10 frames/s in the bounded final test. The separate ASUS IR camera was outside the selected source. Physical loss, actual denial and other devices remain unverified. [Hardware report](EXECUTION_09_REPORT.md)

Execution 08 verified Media Foundation metadata enumeration and native H.264 MP4 to RGB32 decoding on Windows 11 x64 build 26200. A standard conversion succeeded where the advanced converter rejected this fixture. The pinned MediaPipe worker uses the same native reader for its selected camera source. Camera drivers, hardware permissions, format support and actual release/reacquisition remain device-unverified. [Execution 08 evidence](EXECUTION_08_REPORT.md)

Execution 07 verified the shared serial frame pipeline on the pinned Windows x64 environment. OpenCV 5.0.0 reported its Media Foundation backend present through the registry. This was a metadata probe. Real devices, permissions, negotiated formats and camera resource release remain unverified. Ordinary-app UIA remains hidden in the current build, with fixture results separate. [Execution 07 report](EXECUTION_07_REPORT.md)

Execution 06 verified the camera-free WPF observation lifecycle with Python 3.12.10, MediaPipe Tasks 0.10.32, float16/1 Hand Landmarker and .NET 10.0.14 on Windows 11 x64 build 26200. The CPU worker passed native named-pipe delivery, cancel and replacement tests. Capture backend, device permission and camera compatibility remain unverified. [Execution 06 report](EXECUTION_06_REPORT.md)

Execution 05 qualified MediaPipe Tasks 0.10.32 with Python 3.12.10 and the Hand Landmarker float16/1 model on Windows x64 build 26200, CPU, using still inputs. A2 exercised all four WinForms fixture patterns. Live camera devices, native capture backend and ordinary-app compatibility remain unverified. [Execution 05 report](EXECUTION_05_REPORT.md)

Execution 04 verified the four existing patterns on the controlled WPF and WinForms fixtures. Separate WPF peers reproduced late RangeValue, Toggle and Scroll effects. Generic Invoke has no independent readback. The tested display used 125% scaling. Ordinary-app compatibility and physical cross-app drag/focus remain open. [Execution 04 report](EXECUTION_04_REPORT.md)

## Execution03 controlled compatibility — 2026-11-05

Windows26200 x64, runtime10.0.14, primary1920x1200 at125%. Evidence artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json. See [Execution03 report](EXECUTION_03_REPORT.md) for all run IDs and exact limits.

| Controlled provider | Resolution / patterns | Focus / physical drag scope | Decision |
|---|---|---|---|
| WPF standard controls + controlled Slider peer | Under-overlay physical point Confirmed; four patterns independently verified; disabled/missing/duplicate/stale/replaced states reject | UIA foreground unchanged in fixture;100 injected starts/ends plus20 held-drag trials are separate A1 checks | Provisional fixture support |
| Standard WinForms/Win32 Button, CheckBox, HScrollBar, Panel | All four patterns and independent postconditions verified | No cross-app simultaneous physical drag/focus certification | Provisional fixture support |
| Custom WPF AutomationPeer, separate process | Slow/real blocked read query/cancel/exception/termination; worker recovery verified | Primary GHOST heartbeat0.59ms during blocked query | Read-query containment verified |

No third-party applications, private documents, elevated windows or secure desktop targeted. Other DPI/mixed-monitor NOT RUN. Native/injected results do not promote the historical physical-user rows below. Full A1 BLOCKED; full A2 NOT ACCEPTED. Mutation rollback, arbitrary virtualized trees and universal UIA compatibility remain unsupported/unverified.


## Autonomous Execution02 —2026-11-05

Controlled private-desktop fixture1920x1200/125%: injected100/100 panel clicks,100 drag starts/ends,560 chars exact over20 cycles,20 held drags all pass; native closed/idle/active cursor controls0 changes and cleanup pass. Evidence artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.json. These are INJECTED_GUI_TEST/NATIVE_SOFTWARE_TEST, not physical-user rows below.

Provisional UIA controlled fixture: Invoke/Toggle/RangeValue/Scroll postconditions pass; under-overlay point target Unsupported. No real external app/action, shell Alt+Tab, physical-user or other-DPI compatibility promotion. Prior rows remain scoped historical/full-physical acceptance unverified.

## Execution 01.5 — 2026-11-05

Independent current native software run at1920x1200/125%:20 show/hide cycles,900 events, foreground fixture retained, overlay never foreground, required styles/move/release/cleanup/single-instance and stop/restart checks pass. Native cursor changed, so overall run INCONCLUSIVE; no source of movement asserted. Evidence artifacts/a1-native/20261104T193854053-0b5a195da0b64c1b92166e8b8dbb3394/native-smoke.json. Physical mouse/typing/drag/Alt+Tab and remaining DPI cases still NOT RUN; detailed matrix A1_PHYSICAL_ACCEPTANCE_RESULTS.md. No third-party app physical compatibility or UIA claimed. Earlier rows/history retained.

## Latest observations — 2026-11-05

Bounded native A1 fixture run on Windows26200 x64,1920x1200/125%: required styles, two panel replay cycles/90 samples, fixture foreground/cursor retention, release and HWND destruction PASS. Second instance exited. Evidence artifacts/native-smoke.json. First run changed focus/cursor and was inconclusive; preserved native-smoke-01.json.

Full physical click/typing/drag, Alt+Tab and DPI100/150 remain NOT RUN. Original full compatibility rows below are not promoted. No real third-party app or UIA compatibility was tested.

| Target | Ghost Overlay | Physical mouse drag retained | Keyboard focus retained | UIA target resolved | UIA invoke | Notes |
|---|---|---|---|---|---|---|
| Controlled WPF test fixture | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN | Test first |
| Windows Notepad | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN | |
| Windows File Explorer | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN | |
| Microsoft Edge | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN | |
| Chrome | NOT RUN | NOT RUN | NOT RUN | NOT RUN | NOT RUN | |
| Full-screen game | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | Do not promise |
| UAC / secure desktop | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | NOT SUPPORTED | No bypass |

For every tested app, add Windows build, app version, scaling and video/log evidence. A passing Pocket-owned operation is not a passing UIA external action.

## 2026-11-04 — Specification audit, no compatibility promotion
Windows NT10.0.26200.0/x64 host and installed runtime metadata were inspected; no SDK is installed. All application rows above remain NOT RUN. Source findings: focus-dependent simulator, single global owner, net8 target drift, missing imports/configuration validation. None is a measured native compatibility result. Next: A1 timed internal replay plus ordinary-user fixture/Notepad with monitor/DPI, cursor, foreground and event-counter evidence. Separate external UIA compatibility stays A2/experimental. Multi-monitor/HDR/fullscreen behavior unverified.

## Execution 15 integration

| Scenario | Evidence |
| --- | --- |
| Native shelf and shortcuts at 125% | Hidden shelf, owned-fixture conflict, remap and restart exercised on the isolated desktop |
| Capture at 100%, 125% and 150% | Deterministic geometry at all three scales. Real WPF and GDI at the available 125% scale |
| Physical mixed DPI and Explorer | BLOCKED in the isolated environment |
| Camera diagnostics | ASUS FHD acquisition and shutdown observed. Cumulative test-budget failure and its containment are recorded in the [Execution 15 report](EXECUTION_15_REPORT.md) |

## Execution 16 continuation — 2026-11-08

See [Execution 16 evidence](EXECUTION_16_REPORT.md) and [finger research](FINGER_TRACKING_RESEARCH.md). The real camera preview and aligned finger dots have user confirmation. Bounded capture, sparse model points, explicit per-finger states and an independently supervised versioned diagnostic budget are implemented. Full physical B1, Gate A/M02 and sustained finger-accuracy acceptance remain open. All failed runs and repairs are preserved. The final standard regression passed37 cases with zero failures and Explorer blocked; both configurations passed100 core tests and all20 worker tests passed. Draft PR5 is published and implementation455abf4 passed Windows CI. The user reported wrist-out index loss. Complete continuation acceptance remains CHANGES REQUIRED.

## Execution17 — camera-free demo stabilization (2026-11-08)

Main navigation, explicit isolated sample profile, launcher and labeled recorded/simulated entry points reuse existing implementations. Pinning now handles checked-state changes, including accessibility Toggle. Two seeded supplemental WPF-control/GDI/model/restart rehearsals passed. They do not replace native input acceptance: the final standard run was refused at foreground ownership by the existing safety guard. Debug/Release100 core tests and20 worker tests pass; readiness is PARTIAL with a manual remote-session checklist. No webcam acquisition or normal/legacy library migration was performed. See docs/EXECUTION_17_DEMO_REPORT.md and docs/DEMO_FEATURE_INVENTORY.md.
# Execution19 measured compatibility — 2026-11-09

Existing Windows26200/.NET10.0.403 SDK/MediaPipe0.10.32/NumPy runtime retained. V1 full, v2 sparse and v3 pointer-only visual observations are supported by the updated host; an old host cannot read v3 and must be paired with its old worker or INVEY_LEGACY_POINTER=1. No new dependency installed. Generated-image continuation and isolated WPF pass; physical webcam, sustained resource behavior, cold fingertip acquisition and broader display configurations remain unqualified. See EXECUTION_19_REPORT.md.

## Execution20 isolated acceptance — 2026-11-09

Windows private-desktop suite:40 PASS,0 FAIL,1 BLOCKED because Explorer is unavailable in the isolated environment. Camera-free controlled product routes passed; physical camera and ordinary-desktop hand interaction not tested. Details: [Execution20 report](EXECUTION_20_REPORT.md).

## Execution21 controlled beta — 2026-11-09

Pinned Windows/.NET environment retained; no package or runtime added. Extended isolated desktop run `20261109T163409321-5611ac9b719c4475a7d7ee4700a732ba` passed all runnable cases including Pocket/Dual/recorded workflow and user-facing keyboard movement. Explorer/Alt+Tab tray remains BLOCKED without Explorer. Selected-monitor movement is deterministic in the private desktop; mixed-DPI physical hardware and ordinary-desktop focus remain unqualified. No camera was opened.

## Execution22 — 2026-11-10

Typed dispatcher gesture coordinator, neutral-before-pinch, Pause/gap/context/target binding, bounded open/pinch/release lesson and controlled mapped WPF gesture replay added. Final core167/167 Debug and Release; builds0 warnings/errors; worker29/29. Actual WPF E20 gesture/card route passed seed22003; final native regression BLOCKED by GameInputServiceWindow foreground, failure runs retained. Live authorization absent; camera ledger unchanged; no production action issuer. Physical/DPI/shell and final native/lesson acceptance remain open. See EXECUTION_22_REPORT.md and LIVE_GHOST_TEST_GUIDE.md; do not infer physical success or complete software acceptance.

## Execution23 — physical evidence and camera-free quality repair,2026-11-10

Full preparation native39 PASS/0 FAIL/1 INCONCLUSIVE/1 Explorer BLOCKED. Actual guided lesson and Camera-protocol routing passed. Creator confirmed usable scrollbar repair. One authorized physical session117094ms:1141 results,full1033/partial63/empty45;native/guardian release and worker exit confirmed. Real desktop Ghost moved correctly, but user reports jitter/choppiness/disappearance; quality NOT ACCEPTED and fingers-only NOT TESTED. Ledger11 released/926780ms/version5, historical ten rows unchanged. Camera-free cadence repairs pass172 Core tests/configuration and focused WPF seed23009. Preview250ms lifetime is separate from unchanged150ms gesture readiness;50ms interpolation is visual-only, not new observations or higher camera FPS. Raw source rate9.76 results/s unresolved; separate native Tracking run blocked again by service-window foreground on private desktop. Fresh physical retest approval pending; never infer it from prepared version6 code. See EXECUTION_23_REPORT.md.

## Execution23 final retest and recovery —2026-11-10

Second separately approved session127297ms released; ledger12 released,total1054077ms,0 pending,original session cap exhausted. Creator reports worse/jittery/glitching pointer and missing preview; physical quality FAILED, wrist-out NOT TESTED. Removed candidate interpolation and its feature-specific test, preserved inherited suite. Deduplicated unchanged WPF preview frames/landmarks/transforms; placed image before calibration and made Live selection default local preview without activation; exposed mapping rejection reasons. Final171 Core tests/configuration,0 build warnings/errors, worker29/29; final focused WPF/native image+product route seed23010 passes. Raw source about9.8/s unresolved; final physical benefit unverified. No release/live gesture qualification. Full native preparation39pass with cursor/shell gaps remains historical source evidence. See EXECUTION_23_REPORT and leading handoff; no further camera starts without new explicit policy decision.

## Execution24 camera-free capture candidate —2026-11-10

Direct RGB copy synthetic1080p7.90→6.16ms, identical pixels; offered-only30Hz format policy with explicit size/rate negotiation and legacy fallback; bounded PTS/delivery diagnostics. No inference resizing adopted: larger still fixtures lost detection, retained as negative evidence. Core171/config,workers32,native focused product PASS; broader Pocket native blocked by service foreground,3 failures preserved. No physical30FPS/live improvement claim, no camera or ledger change. Presentation wrapper and guide supplied. See EXECUTION_24_REPORT.md; camera cap remains exhausted.

## Execution25 —2026-11-10
Native-file raw→RGB conversion verified on same MF reader. Raw includes native reader/driver scheduling. Actual ASUS cadence/negotiation, mixed-DPI, stationary jitter and finger-only acceptance UNVERIFIED. WPF event delivery independent of label/image polling; no ordinary-desktop input/live action issuer. See docs/EXECUTION_25_REPORT.md for full evidence.

Execution25 final physical evidence: approved raw/conversion check28.606/29.370 unique frames/s; final full capture/model29.492/29.485/s, host4256 accepted, logical pointer3052 updates including loss intervals. Creator confirmed image/dots/Ghost, improved motion/jitter and mouse independence, but unstable tracking and wrist-out froze/disappeared. Physical quality CHANGES REQUIRED, B1/live gestures NOT accepted. Both native/worker/guardian releases, light-off/Exit confirmed. Final ledger14 released/1262999ms/0pending, all starts consumed. Camera-free weak-anchor retention preserves original600ms deadline and requires fresh unique image match; old-code negative control fails,39 worker tests pass. No interpolation/new model/60Hz capability or physical post-fix claim. Details: EXECUTION_25_REPORT.md.

Execution25 final post-feedback:184 core/config,39 workers; native extended25007=38PASS/1FAIL(A03 focus precondition before injection)/1cursor INCONCLUSIVE/1Explorer BLOCKED. Actual product/recorded/cleanup and60Hz software-delivery cases passed. No service/guard changes or clean-run fabrication. Physical quality remains CHANGES REQUIRED; ledger14/1262999ms released, no authorization remaining.

## Execution26 —2026-11-10
Existing opencv-contrib-python5.0.0.93 used locally; same Windows wheel hash-pinned for CI, no local install. Missing optical package explicitly falls back to original patch tracker. Real-image transformations and generated image tests only; physical wrist-out/cold acquisition/longer continuation remain unqualified. Same capture/conversion/IPC/provenance/selected-hand/display/mirror/personal correction pipeline retained.


## Execution27

Execution27: Tier A Pocket controlled route preserved; Tier B six semantic patterns verified on separate Invey.ExternalDemo process, actual counter/checkbox/slider/scroll/expand/selection state and independent native cursor. Toggle during native Mouse hold changed focus and canceled its Workspace claim safely: simultaneous held-drag compatibility NOT SUPPORTED for that case. Genuine winver.exe test BLOCKED before launch because no linked medium token. Calculator/Notepad/browser UNVERIFIED. Tier C pen/touch deferred, Tier D protected/elevated/unsupported targets denied.
