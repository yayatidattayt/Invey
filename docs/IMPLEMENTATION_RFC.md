# Implementation RFC — Core mechanics and dangerous gaps
**Status:** v2 recommended architecture, with explicit experiments. Do NOT treat the contents as executed tests.

## 1. Project boundaries and executable layout
```
POCKET/                                   .NET solution root
  AGENTS.md
  docs/{MASTER_SPEC,DESIGN_SYSTEM,IMPLEMENTATION_RFC,QA_MATRIX,CODEX_PHASES}.md
  src/
    Pocket.App/                           WPF app: shell, tray, settings, app lifecycle
    Pocket.Overlay/                       Win32 styles, WPF overlay, GHOST rendering, shelf visuals
    Pocket.Core/                          pure C# reducer/FSM, gesture recognition, pointer ownership
    Pocket.Automation.Worker/             isolated helper process; COM MTA UIA
    Pocket.Automation.Contracts/          UIA RPC typed messages
    Pocket.Capture/                       frozen screen, selection, crop, preview
    Pocket.Storage/                       manifest + snapshot files, migrations
    Pocket.Tracker.Contracts/             strict tracker message validation
    Pocket.Common/                        cancellation, diagnostics, clocks, geometry
  tracker/
    pyproject.toml                         pinned after validating support
    main.py                                child process/watchdog interface
    camera.py                              owns capture, no recording
    mediapipe_live.py                      asynchronous hand landmarks
    protocol.py                            bounded JSON lines
    models/                                license-audited model asset
  tests/
    Pocket.Core.Tests/                     state, timing, filtering, ownership
    Pocket.Storage.Tests/                  persistence/corruption/migration
    Pocket.Automation.TargetApp/           deterministic WPF buttons/checkbox/sliders/scroll
    Pocket.Desktop.Smoke/                  desktop protocol, hand simulation, focus tests
  scripts/{setup-dev.ps1,run-dev.ps1,verify.ps1}
  packaging/{README.md,licenses/}
  design/{DESIGN_TOKENS.json,UX_CONCEPT.html}
```
Use `src/Pocket.App` for main entry point. Do not implement all modules before gate A. Build first the smallest functional skeleton and expand incrementally.

## 2. Window topology: why we need distinct HWNDs
**GhostOverlay:** full selected display top-level, `WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`, optional `WS_EX_TOOLWINDOW`, visible only when needed. WPF `AllowsTransparency=True`, `WindowStyle=None`; Win32 styles applied/verified after handle creation; `WM_NCHITTEST` should be considered but not assumed to independently solve every input issue. Verify `WS_EX_TRANSPARENT` plus layered window using 100 practical clicks; do not disable mouse pass-through when the hand hovers a Pocket button. The OS mouse never interacts with overlay controls in Passive mode; GHOST manually hits the Pocket state geometry. Persist a foreground HWND baseline and verify focus remains there during peek. Respect alt-tab and taskbar behavior. Ensure overlay does not cover secure desktop or broken fullscreen apps; stop/hide in unsupported contexts.

**WorkspaceWindow:** ordinary WPF window, can receive keyboard/mouse. Host separate pure interaction coordinator for both logical pointers. Do NOT rely on WPF synthetic `MouseDown` for hand; update the view model directly after hit-testing. For normal mouse, acquire WPF physical mouse events and normalize into pointer commands while retaining OS UI event semantics.

**CaptureSelectionWindow:** show a frozen screenshot, captured after overlay is hidden, NOT a live stream needing to be read during rectangle drag. It deliberately accepts normal mouse input and may become foreground. Esc cancels without persisting a screenshot. Provide single-monitor support first, with precise physical pixel/DIP transforms. On Ctrl+Alt+G conflict, show an error and offer tray access.

**SettingsWindow:** native keyboard accessible and focusable. No modal camera enable at startup.

### Reality check
An overlay can be visually transparent but still intercept real mouse events. Verify with `GetCursorPos`, foreground HWND and actual underlying button click. Do not measure only whether the pointer *looks* normal.

## 3. UI Automation is an adapter, not a generic cursor
A normal Windows user process can attempt accessibility operations on supported apps at same/lower integrity, but this is not universal. Build `Pocket.Automation.Worker.exe`: single owner for MTA COM UIA, no WPF UI, typed IPC, 250–600ms UX-specified response budget (not hard guarantee), terminate/restart process after provider stalls; cancellation of caller does not cancel unmanaged COM call automatically. No queries on pointer render frames.

**Target discovery challenge:** our overlay might be topmost for hit testing. Build explicit test using Windows test fixture plus Edge/Notepad/File Explorer. First try `ElementFromPoint`. If own overlay returned, try resolving a candidate underlying HWND using visible window enumeration and Z-order filters while excluding our process, then `ElementFromHandle` and provider-supported descendant geometric search. These methods are *heuristics*, not a universal behind-window locator. If source cannot be reliably resolved, report unsupported. Never hide overlay to probe every 100ms (visible flicker and race). Do not use private UIA data to infer user intent.

**Actions v1 experimental:** `Invoke` (safe controls), `Toggle`, `Scroll`, `RangeValue`, `ExpandCollapse`. Actions require deliberate pinch, are pattern-validated at action time, allowlisted and scoped. Double-click, arbitrary drag/drop, keyboard typing and focus-free claims are not supported. Avoid purchase, deletion, security dialogs and password fields. Some Invoke methods do cause focus or state changes in external apps; show accurate compatibility info.

## 4. Tracker process and IPC
The Python sidecar owns the webcam and MediaPipe Tasks model. Use VIDEO or LIVE_STREAM mode depending on measurements; `LIVE_STREAM` accepts callbacks asynchronously and may drop frames when busy, which is desirable over backlogged latency. Capture at 640×480 or 720p depending on actual results. Cap camera buffer, latest-frame policy; no raw images over IPC.

**Protocol example:**
```json
{"v":1,"kind":"landmarks","seq":184,"tMonoMs":482391,"tracked":true,"hand":"right","confidence":0.86,"index":[0.52,0.34],"thumb":[0.49,0.36],"wrist":[0.45,0.75],"mcpIndex":[0.51,0.58],"mcpPinky":[0.34,0.62]}
```
`confidence` here is conceptual; do not mislabel an arbitrary UIA/MediaPipe proxy as calibrated confidence. If the actual API does not expose a single hand-confidence score, use explicit detected/quality metrics with documented derivation instead. Prohibit NaN/Infinity, impossible coordinates, oversized lines and decreasing sequence/timestamps. Sidecar sends heartbeat every 1s. C# detects no samples or missed heartbeat; after threshold cancels GHOST ownership exactly once, stops actions and shows tracking unavailable. Restart with bounded exponential backoff and easy user stop. Ensure stdout reading never blocks on stderr; keep logs limited.

**Testing:** Replay recorded *landmark numbers* from synthetic fixtures, not private camera frames. Test old frames, multiple hands, high speed, pinch jitter, tracker crash. Offer a visible camera preview only in calibration settings, default hidden during normal operation.

## 5. Deterministic DUAL core
State transitions and side effects use one pure reducer/event coordinator. Input ownership must stay separate for physical mouse vs ghost. Target IDs never refer to external app controls without a capability result. A release from GHOST cancels only ghost targets. Pointer has pressed/dragging independent booleans/ownership, and a target claim fails if another pointer owns target (in v1). No implicit takeover on pointer collision.

Press thresholds and stable-count based on timestamp not frame count; frame rates vary. Hover doesn't invoke. Armed state required. For gesture recognition, palm-normalized thumb-index ratio includes viewpoint failure detection. Reacquisition after loss goes to READY; no resurrected drag. Filter positional updates before routing; position samples can be dropped without losing deliberate pinch UP events—state-aware event transport.

**Scroll design correction:** v1 shelf scroll uses grabbing an oversized scrollbar thumb with GHOST and dragging it, or explicit up/down Pocket controls, both reachable by one deliberate pinch. Two-finger scroll is optional experiment. This avoids requiring complex continuous gestures at launch.

**Peek/interaction conflict correction:** a user cannot simultaneously keep an open palm to hold the shelf open and pinch to interact. Define two variants:
- `Glance`: hold edge gesture for temporary reveal; automatically hides on release, supports no actions.
- `Interactive Peek`: complete deliberate inward pull; shelf latches open for the interaction session, remains open while GHOST hovers/presses, auto-retracts only after >=12s idle (configurable) or explicit outward gesture. Pins survive. Make interactive peek default for v1; simple click/shortcut also supports latching.

## 6. Snapshot capture pipeline
```
Hotkey/tray → verify one display → hide GhostOverlay → capture pixels of selected display
→ present frozen-image mouse selection surface → user rectangles region → preview
→ crop in memory → atomically save PNG asset + manifest → dispose full-screen buffer
→ restore previous focus if safe/possible → edge tab confirmation.
```
Protect against capture of own screenshot selection UI (order matters). Handle screenshot failure, empty selection, very large image, monitor DPI, Windows protected-content black frames, and own window being accidentally included. Support Esc at any stage; leave no data on cancellation. Call it Snapshot, not Live.

**Storage protocol:** stable UUID, schema version, sanitized filename, relative asset path, atomic temp + rename, validate asset path stays under store root, image decode error fallback, clear/delete operations, limited thumbnail caches, migration tests. Store only selected crop, not full-screen frames. If capture includes personally sensitive info, provide warning and local delete. No telemetry, sync or automatic backups.

## 7. Live source and media adapters (do not block v1)
**DWM:** `DwmRegisterThumbnail` registers one top-level source HWND into a destination HWND owned by app. DWM thumbnails are visual, not guaranteed interactive; crop/visibility/minimized restrictions; test source rectangle and update semantics. Does not create an independent browser object.

**Windows Graphics Capture:** source selection can involve system picker and visible capture border; protected surfaces cannot be assumed capturable. Treat selection/permission and capture devices honestly, cleanly stop sessions.

**Video:** link to supported YouTube IFrame embed where allowed, or local video. Save `url + position` only when available through documented player; no bypassing embed restrictions or protected media. If arbitrary region selected over YouTube, offer Snapshot/Return to Source if player integration isn't possible.

## 8. Runtime, packaging, architecture alternatives
**Why start WPF?** Stable native Windows interop, source easy to inspect with Codex, familiar transparent window capabilities, accessible settings. UI overlay rendering is a performance hypothesis, not pre-certified. WinUI 3 has excellent compositor-based animations but introducing it immediately complicates HWND transparency and input architecture; benchmark one concrete spike before choosing. DirectComposition is a targeted fallback only if overlay rendering fails, not a premature rewrite.

**Why keep Python for webcam initially?** Fast integration with MediaPipe Tasks. Biggest liability: packaged app size, runtime dependencies, antivirus heuristics, model redistribution, startup time. Prove Python sidecar works in clean Windows VM with no development tools. If packaging/memory overhead exceeds target, evaluate on-device ONNX or MediaPipe C++/C API only with a confirmed licensed model and actual working Windows builds. Do not promise a seamless one-binary app prematurely.

**Installer vs portable:** ship portable self-contained bundle first for developer testing. Then package via MSI/WinGet/MSIX only after permissions and runtime checks. Normal operation no admin. Unsigned Windows downloads can produce SmartScreen warnings; code signing is optional but generally not free. Do not claim a public production installer is zero friction until tested.

## 9. Internal thread/process safety
- UI/render thread: input dispatch and animations only, no camera decoding, image processing, disk I/O, UIA COM calls.
- Capture worker: full-frame snapshot only on deliberate user request, bounded image size, cancellation.
- Tracker process: camera frames and landmarks, no screenshot access.
- UIA process: current user, no elevated permission, no private data persistence.
- Persistent IO worker: atomic snapshot manifest.
- Crash supervisor: app lives without tracker or UIA; camera off state and mouse-only fallback.
- Exit: release hand actions, close tracker, flush manifest, hide overlays, unsubscribe hotkeys, dispose HWND resources. Failure must not leave input blocked.

## 10. What cannot be 'fixed perfectly'
- Underlying Windows UIA provider compatibility, app focus/capture semantics, secure desktop, fullscreen exclusive and protected content.
- Visual tracker error with occlusion, changing light, worn cameras, glasses? (not relevant) and hand fatigue.
- A single screenshot cannot become live, nor can it provide an editable source element automatically.
- Windows SmartScreen reputation of unsigned installers, exact antivirus reaction, differing graphics drivers.

The product should acknowledge these limits in the interface and offer legitimate fallbacks, rather than resort to fake cursors or invasive system modifications.

## Official technical references
- `WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`: https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
- Layered window mouse hit testing: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
- UIA threading MTA: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading
- WPF transparent windows: https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.allowstransparency
- Windows.Graphics.Capture: https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture
- DWM thumbnail: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmregisterthumbnail
- MediaPipe live streaming: https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python
