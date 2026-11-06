# POCKET × DUAL × GHOST
## Canonical Product Requirements and Technical Design — v0.1
**Date:** 2026-11-04  
**Status:** Proposed build specification; no prototype is represented as completed.  
**Platform:** Windows 11 x64 first. Other Windows configurations are compatibility targets, not initial promises.  
**Business constraint:** No paid APIs, paid cloud services, dedicated sensors, mandatory subscriptions or custom drivers. Existing laptop + webcam + physical mouse.  
**Project principle:** Preserve the user's original idea: a normal physical mouse and a **second, hand-controlled input**, operating at the same time. GHOST looks like an object, not necessarily a standard cursor, but visual styling does not circumvent Windows input constraints.

---

## 0. Thirty-second product definition

**POCKET** is a Windows desktop utility that stores selected regions of the screen in a hidden shelf and reveals them on demand. **DUAL** is its two-input architecture: the physical mouse remains the native Windows cursor while a webcam tracks an independent, hand-controlled pointer. **GHOST** is the animated object representing that second pointer. The hand can operate POCKET without taking over mouse coordinates or mouse button state. GHOST may additionally operate *supported* third-party controls via Windows UI Automation (UIA), with explicit capability and failure feedback.

**Signature demo:** A person starts selecting or dragging inside an ordinary app using the physical mouse, keeps that mouse action active, and uses GHOST to reveal a saved snapshot, move/resize/scroll a POCKET card, then hide it. The native mouse never jumps. In a second demo, GHOST independently invokes a supported button in a separate Windows test app while the physical mouse remains elsewhere; whether focus or the physical drag survives must be measured, not promised.

**Core product claim that can be guaranteed by our code:** simultaneous physical-mouse input to underlying apps and virtual-hand input to our own click-through POCKET layer, subject to real Windows overlay testing. **Not claimed:** two fully native mouse pointers that can arbitrarily drag any two objects in any two Windows applications.

---

## 1. Problems and jobs-to-be-done

- People temporarily need references (videos, instructions, diagrams, shopping information, notes, snippets) while actively doing something else.
- Alt+Tab and permanent always-on-top windows force a choice between interruptions and screen clutter.
- The other hand is often available for a short, coarse secondary action, but is not a good substitute for precision mouse input.
- Existing crop/floating-window applications provide related features; the proposed differentiating experience is **momentary recall and independent secondary manipulation without borrowing the physical mouse**. This remains a hypothesis to test against PowerToys Crop and Lock, OnTopReplica and other overlay utilities.

**Primary users:** ordinary Windows laptop users; students studying alongside content; people following tutorials; multitaskers; people checking reference snippets. Don't market the app as only for developers, videographers or gamers.

**Primary job:** "Keep this on hand without giving it permanent screen space or disrupting what I'm doing."

---

## 2. Names and mental model

- **POCKET** — the finished utility, tray app, and hidden shelf of saved items.
- **DUAL** — logical input engine, maintaining separate mouse and hand interaction state.
- **GHOST** — the on-screen object indicating the hand-controlled pointer. It is a software object, not a second native Windows cursor.
- **Peek** — temporarily reveal the shelf and auto-hide when the deliberate gesture ends.
- **Pin** — keep a particular card visible until dismissed.
- **Workspace** — optional normal POCKET window for mouse-assisted organization; in it mouse + GHOST can each grab a different POCKET item.
- **Adapter** — capability-limited pathway for actions outside POCKET (UI Automation, and later explicit browser integration).
- **Armed** — user has deliberately enabled GHOST interaction. Tracking alone does not trigger actions.

The real physical cursor is always rendered by Windows. GHOST has its own position, hover, press/release, drag ownership and target. Never implement it by repeatedly calling `SetCursorPos` or by swapping the global cursor between positions.

---

## 3. Product scope and exclusions

### P0: Feasibility proof — must precede full development

1. Windows overlay appears over ordinary applications, remains visible, is transparent to real mouse hit testing and does not steal focus in normal Peek mode.
2. Independently move GHOST via simulated coordinates; confirm the native mouse position stays unchanged.
3. UI Automation worker attempts to find an external control at GHOST's point, excluding POCKET's own overlay. Record actual compatibility, focus and timeout behaviors.
4. Invoke one supported control in a controlled test app; repeat while mouse is held in a different app. Report pass/fail, including whether the mouse drag was interrupted.
5. No camera, Pocket capture, AI or visual polish is required to complete this gate.

### P1: Minimum genuinely useful release

- Native Windows app with system tray, startup/pause controls, explicit webcam start/stop and no obligatory admin elevation.
- GHOST follows one hand at interactive frame rate, with calibrated positions and stable click/drag actions.
- Physical mouse and hand have independent input states in a POCKET workspace.
- One click-through overlay for pointer and POCKET shelf; arm/disarm; reveal, retract, pin, select and rearrange cards by hand.
- Create a snapshot POCKET through user-initiated rectangular screen selection; save cropped image locally; organize and delete.
- Keyboard fallback to reveal/hide and manage captured items even with webcam disabled.
- UIA read-only capability inspection, plus an intentionally small allowlist of user-triggered external actions on supported controls; clear unsupported states.
- Crash isolation: tracking process failure cannot lock mouse, hold cards, or trap focus.

### P2: After P1 works

- Multiple pockets in a stack, reordering, searching and source link metadata.
- Live window crops with explicit supported/unsupported source state, using DWM thumbnail / Windows Graphics Capture where technically appropriate.
- Supported video pockets that use legal official player integration or saved source links; do **not** assume arbitrary YouTube regions can be detached or interacted with.
- Per-app profiles; additional UIA capability actions; optional browser extension with explicit permissions and host bridge.
- Configurable multi-monitor placement and better capture behavior.

### Explicit non-goals

- No custom kernel driver, cursor hook that steals the system mouse, DLL injection, code injection into third-party apps, admin/UAC bypass, or protected-content bypass.
- No universal guarantee of drag-and-drop in arbitrary apps, games, fullscreen exclusive software, privileged Windows dialogs, or protected surfaces.
- No AI agent choosing and executing arbitrary actions on behalf of the user.
- No cloud service, account requirement, subscription, analytics SDK or remote camera stream.
- No always-on camera without explicit opt-in; no boot-time capture by default.
- No paid image-to-3D model, full browser editor, animation studio, or unnecessary new software category.

---

## 4. UX detail and state transitions

### 4.1 First launch

1. Welcome screen explains: real mouse untouched; GHOST works with POCKET and some accessible app controls, not every desktop element.
2. Choose **Try without webcam** (simulated GHOST via keyboard) or **Enable webcam**.
3. Explain camera processing is local; OS camera permission requested only when enabled.
4. Select camera, monitor, preferred tracking hand and mirrored/unmirrored control.
5. Calibrate comfortable hand range; save normalized bounds separately for each camera/monitor configuration.
6. Run guided practice: move GHOST, hover a large target, pinch to grab an example POCKET card, release, edge-pull to reveal, edge-push to hide.
7. Optional app adapters are off until user enables them. Show currently supported actions only, not a blanket "works everywhere" claim.

### 4.2 Tray controls

- **Enable/disable GHOST**, **Camera off**, **Reveal POCKET**, **Capture region**, **Open workspace**, **Settings**, **Exit**.
- Status indication distinguishes camera off, tracking lost, armed, pinching, dragging and UIA action unavailable.
- Optional hotkeys, user configurable: e.g. `Ctrl+Alt+P` toggle shelf, `Ctrl+Alt+G` start screen-region capture, `Ctrl+Alt+Space` arm/disarm. Register dynamically and report collisions; never assume a shortcut will always register.

### 4.3 Normal-use scenario

1. User works in Chrome with normal mouse and keyboard.
2. User explicitly invokes capture shortcut. Selection UI temporarily accepts the **physical mouse**; this intentional interaction may change focus, and must not be marketed as focus-free. User boxes a region.
3. Snapshot is cropped in memory, saved to POCKET, shown as a side-edge tab, with source process/window metadata when available.
4. User returns to Chrome. GHOST can remain available while physical mouse continues there.
5. User arms GHOST, performs a deliberate inward pull from a calibrated edge zone. POCKET appears as a click-through layer for the **physical mouse**, but directly interactive for GHOST.
6. GHOST pinches a pocket card, drags it, scrolls a built-in reference panel, or pins it. POCKET's interaction engine handles these events—not Windows mouse input.
7. User releases/returns hand to the edge. Unpinned cards retract. Existing physical mouse action is unchanged.
8. If camera turns off, tray shortcut still reveals the shelf in a normal controlled window.

### 4.4 Workspace mode

A conventional POCKET window where both input streams can manipulate POCKET-owned elements simultaneously. Each object may be owned by one pointer until release. Never silently allow conflicting drags on the same object. Later introduce explicit two-pointer transformations only if useful.

### 4.5 External action mode

GHOST hovers over an external supported UIA target. A contextual ring shows `Click`, `Toggle`, `Scroll`, `Adjust`, `Expand`, or `Unsupported`, based on *actually detected* supported patterns. Deliberate pinch activates one supported action. This may still affect app focus or selection; show a note and test it. GHOST never moves the physical cursor to execute the action. Restrict dangerous/irreversible actions and sensitive text controls; require user confirmation for destructive or purchase-related operations if ever added.

### 4.6 Unavailable states

- Camera blocked/occupied → keyboard/manual fallback with explanation.
- Hand not found → ghost fades/marks lost; no actions; any virtual drag is cancelled/released.
- Multiple hands visible → explicitly track only the selected hand or lock the tracked identity; never switch targets silently.
- UIA target unsupported → `No supported action` visual state, no synthetic mouse fallback.
- Snapshot capture fails/protected → explain, offer manually saved source link or image import.
- Live source minimized or disappears → freeze/mark unavailable, do not claim continuously live behavior.
- Screen layout changes → re-anchor saved overlays and recalibrate if needed.
- UIA provider hangs → time out via isolated worker process and restart; main window remains responsive.

---

## 5. Gesture specification (initial adjustable defaults, not measured truths)

**Coordinate mode:** absolute mapping from a calibrated comfortable camera region to the selected monitor; mirrored horizontal direction is user-adjustable. Add relative mode only if necessary after testing. Clamp positions; ignore low-confidence jumps.

**Hand pointer representation:** soft green circular GHOST object (not an imitation Windows arrow), contextual icon for supported action, halo while armed, distinct pause/lost/drag states. Keep low visual noise and offer reduced-motion option.

**Core gestures**

| Input | Proposed result | Guardrails |
|---|---|---|
| Index fingertip movement | Move GHOST | Smoothed, calibrated, only when armed |
| Thumb + index pinch | Press/select active target | Palm-size normalized ratio, press debounce and hysteresis |
| Maintain pinch and move | Drag owned POCKET target | Movement threshold; original offset preserved |
| Release pinch | Release target | Release debounce; do not release mouse-owned target |
| Open palm hold, then inward move at edge | Reveal shelf | Must start in zone, pass dwell and direction checks |
| Outward movement or release of peek hold | Retract unpinned shelf | Never auto-hide pinned cards |
| Hotkey | Arm/pause or emergency stop | Works without webcam |

**Initial tunable parameters:** tracking sample 15–30Hz target on an ordinary laptop; GHOST display at monitor refresh; pinch enter after ~100–180ms stable observation, pinch exit threshold sufficiently higher than enter threshold, hand-loss cancel ~200–350ms, drag threshold ~8–12 display-independent pixels, edge dwell ~250–450ms. These are starting values to tune empirically, **not guarantees**.

**State machine:** `DISABLED → READY → ARMED → PRESSED → DRAGGING`, with `LOST` and `PAUSED` interrupt states. Reacquisition after `LOST` returns to `READY`, never directly to a held drag. A pause or tracker crash generates `CancelAllHandInteractions` exactly once.

**Camera geometry:** use selected landmarks (index tip, thumb tip, wrist, thumb/index/pinky MCP joints). Normalize pinch distance by stable palm reference. Do not confuse handedness with mirrored coordinate conventions; MediaPipe may report handedness relative to image orientation. Provide calibration visualization.

**Smoothing:** an adaptive One Euro filter for x/y with speed-sensitive latency. Separate click gesture filtering from position smoothing to avoid fast movement causing false presses. Reject obviously stale, time-reversed or discontinuous frames. Use the latest frame; drop backlog.

**Ergonomics:** shoulder-height waving is not the goal. Support resting elbow, short bursts, larger target zones, clutch and keyboard fallback. UI size should favor rough midair pointing rather than tiny toolbar buttons.

---

## 6. Architecture and component boundaries

### 6.1 Overall boundary

```
Physical Mouse ──> Windows ordinary mouse input ───────────> Existing apps

Webcam ──> Python MediaPipe Tracker ──> IPC samples
                                          │
                                        C# DUAL Engine
                                          │
                                        GHOST object
                                          │
                               Capability-aware Action Router
                                 ├─> POCKET-owned UI
                                 ├─> UIA worker -> supported app control
                                 └─> Unsupported -> no action

Capture shortcut -> Selection overlay -> Local SnapshotStore -> Pocket cards
```

### 6.2 Stack

- **Desktop host:** C# + WPF targeting `.NET 10` Windows Desktop (check installed SDK/Windows version before generating project). Windows 11 x64 target, Windows 10 best-effort only after explicit tests.
- **Overlay:** WPF/Win32 top-level transparent layered overlay with `WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`, hidden from taskbar as appropriate. Use native style interop and verify actual clicks land in the underlying application; a transparent appearance alone is insufficient. Overlay needs no keyboard focus in Peek mode. A *separate normal window* is used for settings and interactive mouse Workspace mode.
- **Tracker:** Python 3.x version pinned after compatibility check, `mediapipe` Tasks HandLandmarker in LIVE_STREAM mode, OpenCV for camera frames. Prefer CPU by default. Camera frames remain in tracker process and are never sent over IPC or disk by default.
- **Initial IPC:** Launch the Python sidecar as a child process. Receive newline-delimited JSON landmark samples from stdout asynchronously; send commands via stdin; log bounded stderr to a redacted local diagnostic log. Initial choice eliminates a separate transport dependency. On production hardening, use a Windows named pipe if sidecar lifecycle/security warrants it.
- **Engine:** `Pocket.Core` pure C# with deterministic state-machine and motion transforms. No WPF types in the core.
- **UI Automation:** separate worker thread **or preferably a short-lived restartable worker process** for potentially blocking third-party providers. Do not call UIA from WPF's main UI thread. Restrict to actual supported control patterns.
- **Capture:** explicit screenshot/region selection first. For MVP, hide own overlay before reading the screen, crop the chosen region, save PNG. More advanced capture using Windows Graphics Capture or DWM thumbnail integration is later and must be tested separately.
- **Persistence:** local `%LOCALAPPDATA%/PocketDual/` directory, JSON manifest with schema version and image files. Optional SQLite only when justified by performance/search needs.
- **Packaging:** packaged .NET app plus tracker sidecar and model asset; evaluate Python packaging/licensing, startup and security before distribution. No administrator privileges or custom driver required for normal scope.

### 6.3 UIA adapter, precise scope

At an external screen coordinate, attempt to resolve an accessible control using Windows UI Automation (e.g. `AutomationElement.FromPoint` or native `ElementFromPoint`). **Technical risk:** the click-through overlay could itself appear in the UIA tree. Test this explicitly; filter own-process elements and develop a reliable underlying-window resolution strategy before promising external hit testing. Screen point is in physical pixels; overlay layout may use WPF DIPs, requiring conversion.

| Detected control pattern | Possible explicit user action | Things not guaranteed |
|---|---|---|
| Invoke | Invoke button/action | May change focus / trigger app side effects |
| Toggle | Toggle a supported checkbox/switch | Not every custom control exposes it |
| Scroll | Scroll supported element | Can't assume generic webpages/custom canvases support it |
| RangeValue | Set supported slider numeric value | Range, units and orientation differ |
| SelectionItem | Select supported list item | May change app selection/focus |
| ExpandCollapse | Expand/collapse supported tree/menus | Providers vary |
| Window / Transform | Supported window operations | Not universal desktop dragging |
| No supported pattern | No action with clear feedback | Never counterfeit a click via moving native mouse |

**UIA safeguards:** query only on pointer dwell or at a throttled cadence (~5–10Hz maximum initial target); use timeout, cancellation, process isolation, skip password fields, do not indiscriminately read screen text, do not store UI trees, show a reason for unavailable actions, never invoke on hover alone. Some actions could move focus; a separate Windows foreground concept remains and cannot be duplicated automatically.

### 6.4 Input ownership invariants

- Logical pointer IDs: `MOUSE`, `GHOST`.
- Mouse state is derived from regular input only inside our Workspace window; physical OS input otherwise remains exclusively Windows-owned.
- GHOST state is from tracker/simulator only; NEVER change `GetCursorPos` result by GHOST actions.
- A target can be owned by at most one pointer at once in the first version.
- An action is accepted only if the pointer is armed, target capability is supported, and pointer's current press state authorizes it.
- `up`, `cancel`, `lost`, `pause`, `cameraStop`, `processExit` all release GHOST-owned targets deterministically.
- `Release(GHOST)` never touches `MouseOwnership`. `Release(MOUSE)` never touches `GhostOwnership`.
- External-app UIA actions are discrete operations; they do not establish cross-app persistent mouse dragging.

### 6.5 Suggested C# contracts (interface-level pseudocode)

```csharp
public enum InputId { Mouse, Ghost }
public enum PointerPhase { Move, Down, Up, Cancel, Scroll }
public enum GhostStatus { Disabled, Ready, Armed, Pressed, Dragging, Lost, Paused }
public enum TargetKind { Pocket, ExternalAutomation, Unsupported }

public record ScreenPoint(double XPhysicalPx, double YPhysicalPx);
public record PointerCommand(
    InputId Source, PointerPhase Phase, ScreenPoint Position,
    long Sequence, long TimestampMs, string? TargetId = null);

public record TrackerSample(
    long Sequence, long TimestampMs,
    bool Tracked, double Confidence,
    double IndexX, double IndexY,
    double ThumbX, double ThumbY,
    double WristX, double WristY,
    double IndexMcpX, double IndexMcpY,
    double PinkyMcpX, double PinkyMcpY);

public interface IInteractionTarget {
    string Id { get; }
    bool CanAccept(PointerCommand command);
    void Apply(PointerCommand command);
}

public interface IExternalActionAdapter {
    Task<TargetCapabilities> ProbeAsync(ScreenPoint point, CancellationToken token);
    Task<ActionResult> ExecuteAsync(TargetAction action, CancellationToken token);
}
```

The **tracker sends observations**, not click commands. C# owns the gesture state machine, making behavior deterministic, testable and robust to Python restarts.

### 6.6 Coordinate mapping and multiple monitors

Separate coordinate spaces: camera normalized [0,1], filtered logical hand coordinates, WPF DIPs, monitor-local physical pixels and Windows virtual-screen physical pixels. Use per-monitor-v2 DPI awareness in manifest; handle negative virtual screen origins, display topology changes, scaling changes and rotations. Initial P1 supports one selected monitor; multi-monitor is an explicit later milestone. No silent mixing of DIPs and pixels.

### 6.7 Screenshot/capture mechanism

Capture mode intentionally differs from click-through Peek mode. A temporary selection surface must accept mouse input, and can therefore interrupt another mouse task. The user activates it explicitly, selects a rectangle, confirms or cancels. Hide GHOST and POCKET overlay during capture; retain only cropped image (discard full-screen buffer after processing), name and store with a unique ID, dimensions and optional source-app metadata. Handle multiple monitors and sensitive/protected content conservatively. Never promise protected video captures work.

**Snapshot first.** Live window crops later: evaluate DWM thumbnail `rcSource`/`rcDestination` for window thumbnails, and Windows Graphics Capture where applicable. Neither ensures arbitrary video capture, click forwarding, protected content, or minimized-window updates. A live thumbnail is generally **not** an independently interactive source app.

### 6.8 YouTube/online video strategy

Do not "rip" or silently rehost selected videos. Later: save URL/ID and playback position where available; use official YouTube IFrame Player API in a supported embedded player only if video allows embedding; otherwise offer Return to Source / link. A snapshot region is not an interactive media element. Also allow user-supplied local video files if compatible. Video support is P2, not a blocker for first proof.

---

## 7. Data model and local storage

Initial manifest at `%LOCALAPPDATA%\PocketDual\pockets.json`:

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "id": "uuid",
      "kind": "snapshot",
      "title": "Diagram reference",
      "assetRelativePath": "assets/uuid.png",
      "createdAtUtc": "2026-11-04T00:00:00Z",
      "source": {"kind": "window", "label": "browser", "url": null},
      "placement": {"monitorId": "primary", "x": 100, "y": 100, "width": 360, "height": 220},
      "pinned": false,
      "archived": false
    }
  ]
}
```

- Metadata only; no copies of underlying live websites by default.
- Store cropped snapshots in local user-only directories; apply best-effort appropriate ACLs; warn users snapshots may contain private information.
- Atomic write via temporary file plus rename; validate size/type, handle missing images gracefully.
- Export/delete a pocket and delete all pockets from settings. No uploads, account sync or telemetry by default.
- Versioned migration for future `live`, `video`, `link`, `note` types.

---

## 8. Repository architecture

```text
pocket-dual-ghost/
├── README.md
├── AGENTS.md
├── docs/
│   ├── MASTER_SPEC.md
│   ├── ROADMAP_AND_TESTS.md
│   ├── DECISIONS.md
│   └── COMPATIBILITY_MATRIX.md
├── src/
│   ├── Pocket.Host/                   # .NET WPF main/tray/settings
│   ├── Pocket.Overlay/                # transparent Ghost + shelf windows
│   ├── Pocket.Core/                   # pure states, gestures, routing
│   ├── Pocket.Windows.Automation/     # UIA worker client and contracts
│   ├── Pocket.Windows.Capture/        # snapshot selection and persistence
│   ├── Pocket.Storage/                # JSON and assets
│   └── Pocket.Contracts/              # typed events / IPC DTOs
├── tracker/
│   ├── README.md
│   ├── pyproject.toml
│   ├── main.py
│   ├── camera.py
│   ├── mediapipe_tracker.py
│   ├── protocol.py
│   └── models/                      # packaged after license verification
├── tests/
│   ├── Pocket.Core.Tests/
│   ├── Pocket.Overlay.IntegrationTests/
│   ├── Pocket.Automation.Tests/
│   └── Pocket.TestTarget/            # WPF app with known UIA controls
├── scripts/
│   ├── setup.ps1
│   ├── run-dev.ps1
│   └── verify.ps1
└── packaging/
    └── README.md
```

The names above are target boundaries; Codex may propose minor adjustments justified by actual SDK/tool compatibility.

---

## 9. Security, privacy, accessibility, consent

**Privacy:** camera must be explicit opt-in; default off on startup in first release; no remote image processing; do not store webcam frames; display an obvious camera-active indicator; stop camera when paused/off. Screen capture only after explicit shortcut and confirmation. Local snapshots can include sensitive data—include clear deletion, previews and warnings.

**Security:** no admin or `uiAccess=true` manifest for normal product. Microsoft's UIAccess mechanism is reserved for legitimate assistive technology and has signing/install requirements; do not use it to evade app integrity restrictions. Never attempt interactions on protected desktop, UAC prompts or password entry fields. Do not bypass anti-cheat, DRM, or app restrictions. External actions only after deliberate user gesture; no continuous automatic button pressing. Avoid invoking destructive buttons without explicit confirmation in future versions.

**IPC:** sidecar is child of app, local-only; parse bounded line size; sanitize structured messages; validate numeric ranges and monotonically increasing sequence; do not accept network connections. If later switching to named pipes, restrict access to current user.

**Diagnostics:** timestamped event category, frame latency and errors; do not log screenshots, raw video, detected text or credentials. Allow logs to be disabled and erased. Keep log size limited.

**Accessibility/ergonomics:** everything important has keyboard/mouse fallback; no mandatory camera; reduced motion, adjustable cursor size/contrast, left/right-hand mapping, target magnification, clear state indicators, user-controlled activation. Minimize fatigue.

---

## 10. Performance and reliability targets — NOT measured claims

- GHOST visual overlay: target >= 60Hz where compositor permits; tracking target >= 15Hz and preferably ~20–30Hz on test laptop.
- Tracking-to-visible-motion: initial practical target <150 ms median; measure end-to-end, don't invent benchmark.
- Overlay pointer-to-underlying mouse hit test: 100% of 50 scripted click trials on supported Windows test rig.
- Native cursor coordinates: exactly unchanged for every GHOST-only operation (compare before/after, with native pointer deliberately stationary).
- False activation: <1 unrequested Pocket reveal per 10 minutes of normal movements after tuning (a target; validate in study).
- Hand pinch: >=90% of deliberate 20-trial press/drag/release exercise in ordinary room lighting, with zero stuck dragged targets.
- Camera loss: safe cancel within configured threshold, no indefinite hold.
- UIA timeout: action worker can't freeze UI; failure yields visible feedback.
- Storage: a snapshot persists across restart and remains deletable without cloud access.
- RAM/CPU budget: benchmark on available machine; set thresholds from measurements rather than marketing numbers.

---

## 11. Compatibility and technical risk register

| Risk | Severity | Detection | Mitigation / decision |
|---|---|---|---|
| Second pointer mistaken for actual native Windows cursor | Critical | Requirement review | Never make that claim; app-level Ghost and capability adapters |
| Overlay steals real mouse clicks | Critical | Day 1 native test | Layered/no-activate/click-through styles; separate normal Workspace window |
| UIA overlays hide underlying element from FromPoint | High | Day 1/2 UIA fixture | Test provider resolution; filter own HWND/process and research underlying-window approach; do not fake success |
| UIA action changes focus or cancels native drag | Critical to "simultaneous" claim | Two-app concurrency matrix | Limit claims to tested combinations; use Pocket-owned UI for hard guarantee |
| Provider blocks/hangs UIA call | High | Test unresponsive provider | Isolated worker, timeout, restart, backpressure |
| Webcam jitter, motion fatigue | High | 20-trial user protocol | Calibration, smoothing, large targets, clutch, optional hand mode |
| Multiple camera hands / left-right flipping | Medium | Multi-hand manual test | Lock intended hand identity, explicit mirror config |
| Python sidecar packaging and model license | High near shipping | Fresh clean Windows VM test | Pinned compatible versions, license inventory, packaged asset checklist |
| Mixed DPI and external monitors | Medium/high | 100/150/200% monitors | Physical px/DIP boundaries, PMv2; defer multi-monitor support until tested |
| Protected content cannot be captured | Medium | Protected video test | Respect source; unsupported indicator/link fallback |
| Snapshot stores confidential material | High | Privacy review | Local-only, explicit capture/delete, no analytics |
| No keyboard focus while pocket visible | Medium | Notepad typing test | No-activate overlay, mouse pass-through; direct Ghost actions only |
| OS and security apps block overlay | Medium | Standard/non-elevated apps | Document compatibility; no integrity bypass |

**Hard stop:** if the overlay fails to pass real mouse input or prevent focus stealing in P0, fix platform integration first. Don't polish tracking to distract from the problem.

---

## 12. Acceptance matrix (must record evidence)

| ID | Scenario | Expected result | Phase |
|---|---|---|---|
| T01 | Move simulated Ghost while native mouse stationary | Native `GetCursorPos` unchanged | P0 |
| T02 | Click/drag with native mouse under overlay | Underlying app receives input; GHOST doesn't consume it | P0 |
| T03 | Type in Notepad while overlay present | Key input stays with Notepad | P0 |
| T04 | Probe a WPF fixture button under Ghost | Capability `Invoke` detected, or documented probe failure | P0 |
| T05 | Pinch/invoke fixture button while mouse elsewhere | Fixture action occurs; cursor stays still; log focus changes | P0 |
| T06 | Hold native mouse drag in App A, invoke UIA in App B | Record success/failure; no assertion of universal compatibility | P0 |
| T07 | Move tracked hand | Ghost responds without moving mouse | P1 |
| T08 | Pinch/release with 20 repetitions | >=90% successful, no stuck Pocket drags | P1 |
| T09 | Lose hand during active drag | Ghost action cancels; mouse unaffected | P1 |
| T10 | Mouse drags Pocket item A, hand drags item B | Both drag states coexist and release separately | P1 |
| T11 | Reveal shelf with edge-pull while mouse active elsewhere | Reveal works without native mouse movement | P1 |
| T12 | Cancel unpinned Peek | Shelf hides, mouse focus remains where it was | P1 |
| T13 | Capture rectangle | Cropped snapshot stored, displayed, survives restart | P1 |
| T14 | Camera off / tracking worker crash | Hotkey Pocket usable and no ghost stuck interactions | P1 |
| T15 | UIA unsupported control | No action, clear unavailable state | P1 |
| T16 | Protected/minimized source | Honest unsupported / degraded state | P2 |
| T17 | Multi-DPI external display | Mapping accurate in tested supported configuration | P2 |
| T18 | Permission & privacy review | No cloud upload or covert recording | All |

Every acceptance result should include OS build, SDK version, test app, pass/fail, log / screenshot/video evidence where appropriate, and known caveats.

---

## 13. Milestones and decision gates

**Gate A — Native feasibility (priority #1, ~2–4 developer days):** WPF host and transparent overlay; simulated GHOST; native mouse and focus untouched; UIA fixture target; record simultaneous-action test. If the overlay or external UIA fails, redesign accordingly before investing in webcam polish.

**Gate B — Reliable hand engine (~3–5 additional developer days):** Python MediaPipe sidecar integrated via IPC; hand tracking; calibrated movement; debounce; safe release; keyboard fallback; bounded latency and FPS measurements.

**Gate C — Pocket usefulness (~4–7 additional developer days):** real screenshot crop, cards, local storage, hidden edge shelf, Peek/Pin, independent hand interaction. This is the first real consumer-useful build.

**Gate D — Product stabilization (~1–2 more weeks):** configuration, multiple pockets, accessibility, crash recovery, manual tests, UIA compatibility matrix and installer. Optimize only from profiling.

**Gate E — Optional live integrations (later within ~45 days if stable):** DWM live source, supported embedded player, browser adapter, multi-monitor and native control expansion. Each behind user-visible capability checks.

These are scope targets, not guaranteed completion times. Prioritize passing Gates A-C over feature accumulation. **If external UIA never proves reliable, Pocket's two-channel workflow can still ship, but marketing must not describe universal Windows dual cursors.**

---

## 14. Product and competition position

**Direct/adjacent alternatives:** MouseMux for independent multi-mouse Windows behavior (proprietary and app-dependent); Microsoft PowerToys Crop and Lock for snapshot/live cropped regions; OnTopReplica for window-region mirroring; browser clips and floating player utilities; free webcam gesture-pointer demos.

**No proven global-first claim.** Candidate advantage is the combination of hidden short-lived screen pockets, two simultaneous inputs, and capability-aware actions. Validate in a task comparison: open a reference, return to task, manage reference, hide it; compare POCKET to hotkey-only and existing floating-window workflow. Ask people whether it reduces interruptions rather than whether it looks futuristic.

**Commercial hypothesis:** privacy-friendly consumer utility with no hardware purchase. No business model or price is assumed at this stage. A free first version with optional later paid features is only a possibility; do not design artificial limits before user value is demonstrated.

---

## 15. Developer workflow with Codex

1. Start a Git repo. Put this spec in `docs/MASTER_SPEC.md`, the execution plan in `docs/ROADMAP_AND_TESTS.md`, and a concise `AGENTS.md` at the repo root.
2. Do **not** ask Codex to implement the entire 45-day product in one giant turn. First ask for a **read-only architecture preflight** and Phase 0 POC.
3. Require every phase to produce a running binary/test fixture, commands used, test results, screenshots or logs as available, and a concise list of remaining problems. No claiming something works if it wasn't executed.
4. Keep the native pointer independence invariant in every prompt. Ban fake second-mouse solutions: `SetCursorPos`, rapid global cursor teleportation, or background `SendInput` used as ordinary click fallback.
5. Ask Codex to create isolated, testable interfaces before adding webcam inference. Use simulated hand samples first.
6. Save a compatibility matrix after each real Windows test. Update decision log when changing the architecture.
7. Move to the next phase only after its acceptance gates pass; otherwise repair or explicitly revise the feature promise.
8. Model choice is a separate tool setting; GPT-6 Astra with High reasoning where the Codex client offers it can be useful for tough architecture and Win32 interop. Use the available model settings; avoid assuming account-specific access.

---

## 16. Official documentation and evidence links (verified 2026-11-04)

1. Windows layered-window click-through behavior: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
2. Windows extended window styles (`WS_EX_NOACTIVATE`, `WS_EX_TRANSPARENT`): https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
3. UI Automation control patterns: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-controlpatternsoverview
4. UI Automation threading: https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-threading-issues
5. UIA element by point: https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.frompoint
6. UIAccess security requirements (do not use for ordinary application): https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-securityoverview
7. Windows PMv2 DPI: https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context
8. Windows DWM thumbnail properties: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ns-dwmapi-dwm_thumbnail_properties
9. Windows WPF overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
10. .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy
11. MediaPipe hand landmark Python guide: https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python
12. YouTube supported IFrame Player API: https://developers.google.com/youtube/iframe_api_reference
13. Windows synthetic pen/touch API (NOT a free universal second mouse): https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-injectsyntheticpointerinput
14. OpenAI Codex long-horizon spec/plan workflow: https://developers.openai.com/blog/run-long-horizon-tasks-with-codex
15. OpenAI Codex Astra prompts and concise AGENTS.md: https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra

---

## 17. Definition of "finished" at the first proper release

A normal user on a supported Windows 11 laptop can install and open POCKET, deliberately enable camera tracking, see an independent green GHOST object, capture a selected region of their display, tuck it away, retrieve it through an edge gesture, reposition or scroll that Pocket card using GHOST while still controlling the ordinary Windows mouse in another application, and shut off tracking immediately. With camera disabled, hotkeys and mouse remain viable. External UIA controls work *only where tested and marked compatible*. Every claim has a reproducible acceptance test, and there are no paid AI calls or secret screen uploads.
