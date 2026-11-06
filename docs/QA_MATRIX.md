# QA / Release Gates / Evidence Matrix
**No test in this document has been run on the user's Windows machine by the assistant.** Each test begins `NOT RUN` until Codex or the user records empirical evidence. Do not check boxes merely because code compiles.

## Release sequence
- **Gate A — Native feasibility (must pass before webcam):** click-through, foreground preservation, simulated independent ghost, deterministic Pocket action, UIA fixture probe without freeze. If critical Windows assumptions fail, redesign now.
- **Gate B — DUAL tracking:** camera running locally, smooth ghost, correct coordinates, deliberate pinch, cancel on loss, no native cursor motion.
- **Gate C — Pocket utility:** screenshot capture + store + hidden shelf + reveal/drag/pin/delete + keyboard fallback. First useful product.
- **Gate D — Premium polish + stabilization:** design compliance, motion/perf, installation, crash recovery, security and privacy.
- **Gate E — Optional live/video/UIA/browsers:** only one integration at a time, independent feature flag and compatibility matrix.

Execution 08 authorizes provisional F06 implementation and camera-free testing ahead of these acceptance gates. The original Gate A and M02 release dependencies remain open. Device tests still require an explicitly authorized source. [Development scope and evidence](EXECUTION_08_REPORT.md)

## Recording the test result
For each test record: `ID | Date | Windows version/build | Display configuration/DPI | App version/commit | Test target | Steps | Expected | Observed | Evidence path | PASS/FAIL/NOT RUN | Owner`.
Store a short anonymized screen recording or screenshot for visual Windows tests with test content, not user passwords or actual sensitive windows. Store CLI output for unit/build tests. Evidence paths are only created when actually verified. Do not fabricate.

## Gate A — OS integration
| ID | Setup and action | Required observed behavior | Initial |
|---|---|---|---|
| A01 | Launch app on Windows 11 x64, camera disconnected | Tray, shell and overlay can start; camera not mandatory | NOT RUN |
| A02 | Overlay covers a Notepad/test-app button, mouse clicks 100 times | 100/100 reach underlying app; no Pocket click steals the mouse | NOT RUN |
| A03 | Type continuously in foreground editor while GHOST shows/hides shelf 20 times | Keyboard input remains in original editor, no forced foreground | NOT RUN |
| A04 | Drag native slider/object with mouse; operate Pocket panel via simulated GHOST | Mouse drag continues; ghost operation separate | NOT RUN |
| A05 | Record `GetCursorPos` before/after 100 ghost movements with physical mouse stationary | Exactly unchanged native cursor coordinates | NOT RUN |
| A06 | Overlay topmost, test own UIA hit response at same coordinates | Worker identifies underlying test control reliably or reports Unsupported; no self-invoke | NOT RUN |
| A07 | UIA test app with Invoke, Toggle, Scroll, RangeValue and ExpandCollapse | Each supported action succeeds when deliberate and does not teleport cursor | NOT RUN |
| A08 | UIA target returns slowly/hangs, kill worker | Host remains responsive; worker restarts or target Unsupported | NOT RUN |
| A09 | UIA action performed in other app while mouse drag held in first app | Record whether focus/drag survives; feature not declared universal unless passes for target | NOT RUN |
| A10 | Alt+Tab, taskbar, focus switching with overlay enabled | GhostOverlay absent from Alt+Tab; settings window ordinary; no hidden focus capture | NOT RUN |
| A11 | High-DPI monitor 125% and 150% (if available) | GHOST visual and hit target align, documented tested setup | NOT RUN |
| A12 | Secure desktop/UAC/fullscreen protected apps | UIA disabled/unavailable, no bypass, not falsely interactive | NOT RUN |

## Gate B — Webcam/Gesture
| ID | Setup and action | Required observed behavior | Initial |
|---|---|---|---|
| B01 | Webcam opt-in disabled | No camera start, no frame capture, shortcuts usable | NOT RUN |
| B02 | Enable webcam, choose camera | Local tracking only, visible indicator, no upload | NOT RUN |
| B03 | Calibrate and draw rectangle route with fingertip | Cursor tracks screen correctly for selected display and mirror setting | NOT RUN |
| B04 | 30 deliberate pinch/down/up trials in two light levels | >=90% correct actions, zero stuck target | NOT RUN |
| B05 | Hold physical mouse drag + 30 hand drags on separate Pocket item | All independent, no cursor jump, no ownership crossover | NOT RUN |
| B06 | Abruptly move hand out of frame during drag 50 times | Ghost releases within configured grace interval, no permanent hold | NOT RUN |
| B07 | Two hands enter/leave camera frame | Locked hand not silently swapped; when ambiguous, disarm | NOT RUN |
| B08 | Camera disconnected/other app occupies camera | Clean error, stop handles, mouse-only fallback | NOT RUN |
| B09 | Kill tracker process while dragging | Host alive, hands released, error shown, no blocked mouse | NOT RUN |
| B10 | 20-minute ordinary desk usage without deliberate gestures | No more than one accidental shelf reveal after tuning | NOT RUN |
| B11 | 30-minute continuous run on laptop CPU | Measure tracking FPS, median/p95 latency, RAM/CPU drift; disclose values | NOT RUN |
| B12 | Explicit pause, resume, camera off | All ghost-owned actions cancel, camera actually closes, user controls restored | NOT RUN |

## Gate C — Pocket product
| ID | Setup and action | Required observed behavior | Initial |
|---|---|---|---|
| C01 | Shortcut capture on known test wallpaper | Screenshot taken before selection window displayed, no app overlay appears in crop | NOT RUN |
| C02 | Select rectangle after 125% DPI | Saved PNG has correct pixel coordinates and content | NOT RUN |
| C03 | Press Escape before confirming | No saved image, no stray full-screen screenshot | NOT RUN |
| C04 | Capture black/protected/unavailable region | Clear error/fallback and no fake success | NOT RUN |
| C05 | Save 10 snapshots, restart | All manifest records intact and files load correctly | NOT RUN |
| C06 | Tamper/corrupt one manifest/image, relaunch | Recover others; corrupted item marked unavailable; no crash | NOT RUN |
| C07 | Pin a card, hide shelf, relaunch | Pinned card persists and stays within display bounds | NOT RUN |
| C08 | GHOST grabs card while mouse is active in external app | Target follows hand, normal mouse works underneath | NOT RUN |
| C09 | Mouse and GHOST move separate cards in Workspace | Independent ownership & both drags simultaneous | NOT RUN |
| C10 | Interactive Peek: edge-pull, pinch a control, linger, dismiss | Shelf remains available while pinching; no open-palm hold conflict | NOT RUN |
| C11 | Glance variant / shortcut | Can consult and close without webcam and without broken state | NOT RUN |
| C12 | Delete, undo, empty library, delete all data | Honest confirmation, correct file removal, no unexpected private files | NOT RUN |
| C13 | Restore after screen resolution changed | Pocket cards clamped within selected screen bounds | NOT RUN |
| C14 | Rename, reorder, scroll, expand/collapse cards | All controls respond with GHOST and mouse/keyboard fallback | NOT RUN |
| C15 | Activate hotkey conflicting with another app | Explains conflict, allows remapping/tray action | NOT RUN |

## Gate D — Visual quality, performance, accessibility, distribution
| ID | Setup and action | Required observed behavior | Initial |
|---|---|---|---|
| D01 | Capture approved design reference on 1366×768 at 100%, 125%, 150% | Layout readable, no clipped text/actions, ghost targets large enough | NOT RUN |
| D02 | Open/close shelf repeatedly with FPS logging | Target close to display cadence; p95 frame pacing measured; note misses | NOT RUN |
| D03 | Move hand quickly across screen with latency probes | Median tracking-to-pixel <=120ms and p95 <=220ms as stretch targets; report actual | NOT RUN |
| D04 | Turn on Windows reduce-motion setting | No forced spring animation, accessible cuts/fades | NOT RUN |
| D05 | Keyboard-only navigation in normal Workspace/Settings | All settings, captures and saved items operable without webcam | NOT RUN |
| D06 | High contrast + scaling + left/right hand setting | Visual states understandable without accent color only | NOT RUN |
| D07 | Clean Windows 11 x64 machine, no developer Python | Portable build runs with runtime/model assets included | NOT RUN |
| D08 | Disconnect network before launch | Core features operate offline; no background outbound requests | NOT RUN |
| D09 | Exit app during camera run and snapshot write | Camera released; manifest atomic; no leftover window or blocked input | NOT RUN |
| D10 | Inspect artifact licenses + binaries + package hashes | Redistribution terms documented and included notices; no unverified restricted weights | NOT RUN |
| D11 | Two-hour mixed session with Pocket + Ghost | No progressive lag/leaks/crash; capture benchmark data | NOT RUN |

## Gate E — Optional integrations, never treated as shipped by default
- **E01 Live crop:** supported source selection, crop updates, source minimized/disappeared fallback, no false claim of live updates.
- **E02 Video:** permitted source/player API, playback resume, embedding blocked fallback, no ripping.
- **E03 Browser adapter:** explicit installation/permissions, supported actions, clear missing permission state, background process failure recovery.
- **E04 Monitor expansion:** negative coordinates, per-display DPI, rotation and disconnected display; explicit mapping tests.

## Release blocking failures (P0)
1. GHOST causes physical native cursor movement or blocks normal mouse input.
2. Overlay steals keyboard focus in Passive Peek during ordinary work.
3. Press/drag remains stuck after hand lost, pause or process crash.
4. UIA worker freezes host, or acts on unverified/sensitive external targets.
5. Camera or captured screen uploaded/stored without user permission.
6. Feature advertised as working but consistently nonfunctional on tested target OS.
7. Broken installer cannot start offline on a test Windows machine.

## Competition baseline
Before publicly claiming novelty, compare to Microsoft PowerToys Crop and Lock, OnTopReplica and MouseMux: (a) how quickly a reference can be recalled without mouse focus, (b) whether gesture actions preserve the physical cursor, (c) how accessible shortcuts compare, (d) whether the proposed workflow saves enough attention/time to justify camera use. This is a product validation test, not a patent/originality finding.
