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
