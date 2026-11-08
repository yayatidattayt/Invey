# Demo feature inventory

Inspected from MainWindow/App, References, Tracking, Shortcuts, SimulationController and the existing acceptance runner on2026-11-08. Verification describes the inherited camera-free baseline; Execution17 final results are reported separately. “Remote safe” requires deliberate capture of non-private content and a working remote display.

| Feature | Open it | Baseline evidence | Hardware / remote limits |
|---|---|---|---|
| Main window and tray | Invey.exe; tray Open | Startup/shutdown and shortcuts integration | Tray/Explorer needs manual remote check |
| Pocket capture and display selection | Main Capture region or Pocket Capture | Actual GDI fixture capture at125% DPI | Display changes require refresh; remote content is real |
| Mouse/keyboard region, Preview, Back, Cancel, black confirmation | Capture window controls | Native capture/negative controls | No webcam; save approved crops only |
| Local store, persistent library, recovery | Pocket | Core storage and restart tests | Normal profile preserved; recovery errors shown |
| Rename, ordering, Delete, Undo, purge/repair | Pocket library controls | Existing WPF and core tests | Explicit deletion/repair actions; no automatic purge |
| Floating cards: Open, move, resize, zoom, Hide/reopen | Pocket Open reference | Existing card state/placement and restart tests | Offscreen placement is clamped to available work area |
| Pin/unpin and supported placement persistence | Card Pin | WPF card/pinned restoration tests | Remote monitor identities may change placement |
| Shelf reveal, tuck, restore | Main Pocket; Tuck shelf; shortcut/tray | Native visibility/hit-test tests | Tuck must preserve independently pinned cards |
| Shortcut configuration and conflict feedback | Settings | Native hotkey/conflict/write-failure/restart tests | Remote client can intercept chords; buttons remain available |
| Workspace actual references and A/B loading | Main Workspace | Native two-image decode tests | No camera; local library required |
| Selection, move, resize, Fit/100% | Workspace target grip and controls |30 controlled two-owner sessions | Ordinary mouse and keyboard supported |
| Independent logical Ghost channel | Workspace labeled simulated Ghost demo | Reducer and earlier fixture two-owner tests | New user button passed supplemental WPF control routes; native input remains manual; no live actuation |
| Recorded model tracking | Ghost Tracking → Recorded model sequence → Enable | Real local inference and WPF lifecycle | Needs existing Python, qualified model and sample; camera off |
| Recorded image/fingertip/skeleton views | Show local view; overlay selector | Correlated frame transport, sparse/ambiguity tests | Recorded image is explicitly not live |
| Controlled calibration practice | SIMULATED calibration practice → Enable → Calibrate | Earlier fixture Ready/hold/loss/cancel tests | New UI entry passed supplemental control routes; no video in this mode |
| Real webcam integration | Live camera → Find → select → Enable | Historical authorized sessions only | No Execution17 hardware test; creator must deliberately enable |
| Live preview, overlays, image mirror, frame timing | Tracking lab during chosen source | Execution16 hardware and synthetic evidence | Optional, experimental; remote camera may be unavailable |
| Experimental partial-index pointer | Calibrate → experimental preview | Sparse protocol and negative-control tests | Wrist-out tracking can fail; no inferred-point action authority |
| Selected display, working region, sensitivity, hand choice | Calibrate | Deterministic transforms/preferences and WPF tests | Physical selected-hand/mixed-DPI acceptance remains open |
| Independent horizontal pointer mirror | Calibrate | Mapping tests and earlier direction observations | Camera-image mirror does not change pointer mapping |
| Local Move/Pinch/Release practice | Calibration practice target | Recorded held/release/ambiguity/restart cases | Invey-owned practice only; no OS input dispatch |
| Settings persistence and write-error handling | Settings and Save calibration | Core and native write-failure/restart tests | Error feedback preserves existing values |
| Pause/Stop/Cancel, worker recovery | Main Pause, Tracking Stop/Cancel, tray | Recorded timeout/crash/protocol and cleanup tests | Camera guardian stays intact |
| Passive overlay, replay, controlled fixture, evidence export | Main Diagnostics | Inherited isolated A1/A2 tests | Advanced tools remain available; replay is labeled |
| Optional demo profile and samples | Launcher -DemoProfile | Explicit preparation/idempotence and two supplemental rehearsal seeds passed | Separate Invey-Demo/Execution17; no migration of real library |
| Demo guide | Main expandable guide | Text/navigation inspection | Does not activate, capture or seed normal data |

The baseline on ab619be passed100 core tests in each configuration,20 worker tests and37 standard Windows cases with Explorer blocked. Those results do not automatically validate this branch. See EXECUTION_17_DEMO_REPORT.md for new evidence and any manual checklist.
