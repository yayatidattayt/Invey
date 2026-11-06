# POCKET × DUAL × GHOST — Product and Engineering Specification v2.0
**Status:** executable specification for Codex; the application is NOT yet implemented. **Date:** 2026-11-04. **Target:** Windows 11 x64 first, Windows 10 later only if tested. **Cost:** mandatory software/AI subscriptions ₹0; existing Windows computer, webcam and mouse. Developer time, code signing and optional accessories are not included.

## 0. Product in one minute
POCKET is a native Windows reference utility. A user can temporarily select a visible part of the desktop, save it to a hidden edge shelf, and recall it for quick consultation. DUAL is the simultaneous input architecture: the physical mouse remains the ordinary Windows cursor; a separate webcam-controlled logical pointer can operate POCKET while the mouse works in another app. GHOST is the minimal, animated visual representation of that hand pointer. GHOST may optionally perform *discrete, explicitly supported* third-party UI Automation actions; it is **not** a second globally native Windows mouse.

**Signature demonstration:** In a Windows text editor, begin an ordinary mouse drag (or continue typing with focus in that editor). Without moving the physical mouse, use the other hand to reveal a saved Pocket diagram, move it, scroll its contents, pin it, and dismiss it. Test independently both mouse-drag continuity and keyboard focus (do not infer one from the other). For Pocket Workspace mode, drag one Pocket object with the mouse while GHOST drags a different Pocket object concurrently. External app controls are shown as separately validated compatibility demos.

**User promise:** "Keep useful things one gesture away without putting down the mouse." **Not promised:** universal, simultaneous native drags across arbitrary Windows applications, protected windows, games or UAC surfaces.

## 1. Roles and operating modes
- **Physical mouse:** remains OS-owned, never teleported or simulated in GHOST's name.
- **GHOST:** independent screen-positioned app object; tracks hover, press, drag and source state. It can only operate *known Pocket targets*, or *verified external UI Automation capabilities* when explicit external-action mode is enabled.
- **POCKET:** shelf of snapshot/notes/link references. Snapshot is fully supported in v1; live/video are separate later adapters and must be labeled.
- **Invisible when not needed:** compact tab, optional ghost only when armed; no always-open dashboard.
- **Passive Overlay / Peek:** topmost, layered, non-activating, OS-mouse-pass-through. Ghost interactors manually dispatch to Pocket's own state machine. Never synthesize DOM or native clicks to itself.
- **Workspace:** conventional Pocket window that can receive physical mouse events and independent hand events; supports concurrent drags of different Pocket items.
- **Capture Mode:** explicitly invoked screen-selection process; may take input focus and does not promise simultaneous uninterrupted mouse drag during capture.
- **External Actions (experimental, off by default):** UIA probe + deliberate invocation of allowlisted patterns; no generic drag, keyboard typing, UAC, passwords, or purchase-like actions.
- **Camera Off / Mouse-only:** capture, store, browse and shortcut-recall remain useful without webcam permission.

## 2. Release contract: what counts as "every feature working"
A feature is either `SHIPPED`, `EXPERIMENTAL`, `DEFERRED` or `UNSUPPORTED`. Never present experimental behavior as functional. "Every feature working" applies to the **shipped v1 set** below across tested Windows 11 configurations, not every imagined future capability.

### v1 SHIPPED feature contract
1. Single-instance tray app; launch, reopen, show settings, clean quit, recovery if crashed.
2. Camera OFF by default, clear privacy indicator, explicit enable/disable; selected camera; no camera frame files/uploads.
3. Independent GHOST pointer in transparent, topmost, click-through overlay; mouse and keyboard remain usable underneath.
4. Simulated ghost mode for testing and for troubleshooting without a camera; keep it in developer controls, not as a confusing consumer feature.
5. Calibration, hand selection, orientation mirroring, filtering, pinching, drag release, safe loss/pause, clutch, sensitivity settings.
6. Explicit user-initiated **Snapshot** capture: freeze screen after hiding own overlays, select region, preview, cancel, save image + metadata in local user profile.
7. Hidden right-side shelf with cards; open on key or deliberate gesture, hide, pin/unpin, drag, resize, reorder, scroll contents, rename, delete with Undo where feasible.
8. Keyboard and mouse fallback for every core user action, even when webcam unavailable.
9. Pocket Workspace supports two independent simultaneous drags of distinct Pocket objects, with separate ownership.
10. Persist snapshots/pinning/layout after restart; recover gracefully from missing/corrupted files.
11. Status and error feedback, reduced-motion setting, keyboard shortcuts with conflict reporting, first-run onboarding, Settings and local data deletion.
12. Offline operation once model assets are installed; build a portable package or installer with all runtime pieces included, no admin requirement for normal use.
13. UI Automation support is an **optional, gated feature** until real compatibility tests pass. Show meaningful hover/unsupported feedback only if target detection has been validated. Include a controlled test target first.

### v1 non-goals (never fake them)
- Arbitrary live content in every cropped region; full fidelity YouTube extraction; virtual second OS mouse; generic drag-and-drop in other apps; cross-app clipboard automation; OS-wide gesture typing; support for fullscreen exclusive/protected windows; cloud sync; screen OCR; generic AI agent; macOS support.
- "Gesture opens a website widget" without a support contract; if video embedding disallowed, show link/open-original fallback rather than a dead video tile.

### v1.5 / v2 feature ladder (each behind a separate test gate)
- **Live crop:** separately tested DWM or Windows Graphics Capture preview with explicit source/app compatibility, missing source state, user permission and no fake interaction forwarding.
- **Video pocket:** local file playback or official allowed YouTube embedded player; explicit title, timeline, thumbnail, volume and Return to Source; never equate pixels with an interactive player.
- **App adapter:** well-scoped browser extension with explicit per-site permissions or UIA target patterns; preserve focus where possible, report when impossible.
- **Multi-monitor:** per-monitor DPI, negative coordinate origins, topology persistence and overlay surfaces.
- **Optional duet actions:** selected cross-pointer operations inside Workspace, only if testing proves useful.

## 3. Entire end-user journey
**First run:** Welcome → 20-second interactive explanation ("Mouse stays normal. GHOST operates your pockets") → choose "Try with shortcuts" or "Enable webcam" → camera permission only on click → calibration target at comfortable desk height → three large practice targets → ready state. Provide skip and redo; no tutorial that hijacks Windows pointer.

**Save reference:** Hotkey (configurable) → app hides its overlays → capture a frozen frame → interactive selection overlay accepts **normal mouse** just for this step → drag rectangle → confirm preview or Escape → store cropped image only → small, quiet confirmation and edge tab; return to previous app; don't claim capture itself is focus-neutral.

**Recall reference:** With GHOST armed, hold at the right-edge activation zone, deliberately pull inward → compact Shelf glides out (if no saved references, an elegant empty state and Capture control) → ghost moves over items → pinch to pick one / drag to peek surface / pinch pin icon → read or scroll (hand-based scrolling must operate our viewer, not arbitrary source app) → reverse pull or release hold to dismiss unpinned items.

**Independent work:** Physical mouse can keep selecting/editing in main app while GHOST controls Pocket panels. Keyboard focus under Passive Overlay should stay with main app in supported circumstances. GHOST actions affect only Pocket. Capture Mode, Workspace Mode and external UIA actions have different focus semantics; the user can see the difference.

**No camera:** Shelf via hotkey or tray; normal Pocket window for keyboard/mouse use; snapshot capture and storage still functional.

**External action:** User toggles Experimental External Controls ON in settings with clear notice → GHOST hovers over an accessible supported control → scoped ability icon appears after dwell → user deliberately pinches to invoke; UIA worker executes with timeout → success/failure indicator. Password/sensitive controls, apps at higher integrity and destructive actions are blocked. Never activate merely on hover.

## 4. The Windows boundary and decisions
### Guarantee only what we own
- A graphics object in a layered overlay is NOT automatically a native Windows pointer. UIA is a semantic accessibility channel, not a complete input channel.
- Only Pocket-owned actions can have truly deterministic two-pointer ownership. App actions may change focus, app selection or foreground window, and may be inaccessible entirely.
- We do **not** use `SetCursorPos`, global mouse movement injection, low-level injection, protected-app bypass, hooks or driver installers as generic fallback. A separate technology spike may evaluate synthetic pen, but it is excluded from v1 product promise.

### Window design
- **GhostOverlay HWND:** topmost, non-activating, layered and verified mouse-pass-through. Use Win32 extended styles and test; do NOT assume visually transparent means input-transparent. `WS_EX_TRANSPARENT` with `WS_EX_LAYERED` has documented layered mouse-pass-through behavior. `WS_EX_NOACTIVATE` reduces foreground stealing. It may still be visible to UIA/WindowFromPoint, so design target filtering explicitly.
- **PocketWorkspace HWND:** conventional normal window with physical-mouse input, standard focus behavior.
- **Selection HWND:** temporary foreground surface that displays a frozen screenshot for precise mouse selection; show a visible Escape cancel affordance. Capture underlying frame BEFORE showing its own selection UI and hide GhostOverlay beforehand.
- **Settings HWND:** normal keyboard-accessible window.
- Avoid global keyboard hooks when registered hotkeys suffice; report hotkey registration failures.

### External UI Automation
- **Dedicated, restartable helper process** preferred. Its UIA calls run on their own COM MTA thread, not WPF UI thread; use memory/CPU watchdog + helper-process termination on a hung provider. An async `Task` timeout does not abort a blocking COM call; implement isolation, not merely cancellation tokens.
- If UIA `ElementFromPoint` hits our overlay, skip POCKET-owned HWNDs and implement/prove underlying target resolution (candidate HWND detection, element-from-handle and descendant hit-testing where supported). This may be incomplete on some apps. An excluded overlay does not guarantee a correct target under it.
- Probe at throttled rate / after dwell, never every tracked frame. Cache only current control role and supported action; never persist whole UI trees, private text or secure control contents.
- Allowlist supported patterns (Invoke, Toggle, Scroll, RangeValue, ExpandCollapse). Validate pattern existence at the moment of execution, not only on hover. UIA actions may move focus; do not call them "focus-free" without data.

### Display/coordinate guarantees
- Treat all coordinates separately: camera normalized → camera-local calibrated viewport → monitor physical pixels → Windows virtual-desktop physical pixels → WPF DIPs. Use Per-Monitor-V2 awareness, per-display scale factor and physical pixel origin; first release supports one selected display. Monitor changed, RDP or DPI changed → safe pause/recalibration. Handle reversed/mirrored camera correctly and do not rely on handedness label alone.

## 5. Architecture and worker supervision
```
Normal mouse ───────────────────────────── Windows ordinary input ──> external apps
Webcam ─> MediaPipe tracker process ─ IPC normalized hand samples ─> DUAL core
                                                              ├─ pointer/filter/FSM
                                                              └─ capability router
                                                                 ├─ Pocket-owned actions → Shelf
                                                                 ├─ UIA worker process → supported controls
                                                                 └─ unsupported → no action
Capture hotkey → hide overlay → snapshot → selection → crop/store → Shelf
```
- **App host:** .NET 10 + WPF Windows 11 x64, tray UI, windows, local store, keyboard control, explicit flags; confirm SDK and host packaging empirically.
- **Rendering:** begin WPF with minimal vector visuals and transform/opacity-only animations. **Perf gate:** if layered overlay fails to reach smooth response on test hardware, investigate Win32-hosted DirectComposition/WinUI composition for the overlay without rewriting the core. Do not switch frameworks just on aesthetic guesses.
- **Tracker:** Python + MediaPipe Tasks hand landmarker `LIVE_STREAM`, OpenCV video capture; model assets bundled only after checking redistribution permissions. Python packaging via controlled, version-pinned sidecar; test clean install/no Python dependency before claiming shipping-ready. Camera frames stay in process.
- **Tracker protocol:** bounded newline JSON from child stdout (or named pipe if proven necessary), monotonic time and sequence; authenticated to child-only local process; strict parse; heartbeat and exit watchdog; never log camera frames or capture images.
- **Core:** pure C# deterministic state machines, gesture classifier, smoothing, pointer ownership, Pocket state, no WPF types.
- **UIA helper:** independent restartable executable, COM MTA worker, strict IPC and allowlist; never do blocking UIA work on UI thread.
- **Storage:** `%LOCALAPPDATA%/PocketDual/`, atomic versioned JSON manifest plus `assets/{uuid}.png`, bounded cache, duplicate/reopen behavior, deletion and basic migration.
- **No network traffic:** no cloud inference, updates or telemetry in v1 by default; if optional update checking is considered later, make it visible and user-controlled.

## 6. Input contracts and gesture engine
```csharp
public enum PointerOwner { Mouse, Ghost }
public enum GhostState { Disabled, Ready, Armed, Pressed, Dragging, Lost, Paused }
public enum PointerPhase { Move, Down, Up, Cancel, Scroll }
public sealed record PointerSample(PointerOwner Owner, PointerPhase Phase,
    double XPhysicalPx, double YPhysicalPx, long TimestampTicks,
    long Sequence);
public sealed record TargetClaim(string TargetId, PointerOwner Owner, double GrabOffsetX,
    double GrabOffsetY);
```
- Use **idempotent ownership transitions**: only one owner per target for v1; releasing mouse never releases ghost and vice versa; `Pause`, `Lost`, `CameraOff`, `TrackerExit` generate exactly one cancel of all GHOST-owned targets.
- Tracker emits image observations, **not** click or app actions. C# owns state machine; unit test via synthetic samples.
- Pinch normalized by palm measurement. Press and release thresholds differ (hysteresis); require stable observations; release on loss safely. Avoid infinite hold when one fingertip disappears. Distinguish drag from click via movement threshold.
- Keep index tip as positional reference. Hand lock: do not swap tracked hands across frames unexpectedly; if identity ambiguous, disarm and require re-arm.
- **Clutch:** explicit armed/paused state, reasonable default disabled until user enables. Allow hand rest; 2D pointer is not continuously active by merely observing a hand.
- Filter coordinates adaptively with One Euro or equivalent; exclude large impossible jumps; drop stale frames; never queue old camera results waiting to render.
- **Reveal gesture:** not a random swipe. Require (1) armed state, (2) origin in right-edge zone, (3) stable dwell, (4) inward direction + distance threshold, (5) cooldown; always provide shortcut fallback.
- Start settings are **provisional**: tracking desired 20–30Hz, pointer animation at display cadence, pinch confirmation 100–180ms, hand-loss cancel 200–350ms, reveal dwell 250–450ms. Tune with trial data; don't bake unmeasured claims into marketing.

## 7. Visual identity and smooth motion
See `docs/DESIGN_SYSTEM.md` for full, measurable spec. Visual direction: **"Quiet Instrument"** — ink charcoal, warm off-white text, one muted sea-glass accent; restrained typography, precise alignment, soft geometry, no animated gradient wallpapers, no glowing orb, no robotic AI assistant aesthetics.
- GHOST: 16–22px crisp disc or rounded diamond with 1.5px ring, action glyph only in expanded state; grows modestly during pinch, no particle effects; distinct lost/paused state.
- Shelf: folded side-tab at edge (about 12px hit region visually), 340–400px standard width within safe screen bounds; up to three cards visible, scroll for more; snapshot, text, link are visually differentiated by small icons and metadata, not giant badges.
- Motion: spring-like shelf pull/retraction (~200–320ms perceptual target), tiny hover feedback (~100–140ms), focus fade (~120–180ms). Use translation/opacity, not animated layout recalculations. New position follows tracking immediately with limited smoothing, not a 300ms CSS-style transition that lags behind the hand.
- Respect Windows reduced motion, high contrast and 125–200% DPI. No blink/flash/persistent glow; subdued opacity only when not active.
- 60fps aspiration is **not guaranteed**; measure dropped frames and end-to-end latency on real laptop. Smoothness is an acceptance gate, not a slogan.

## 8. Privacy, security and capture
- Camera off on startup; explicit consent; local inference; camera indicator, close camera handles on pause/off/exit. No remote data, telemetry or raw-frame storage.
- Capture explicitly triggered, preview before saving; discard full-screen screenshot buffer after crop. Warn about sensitive information in screenshots; local delete-all controls; no clipboard or password capture by default.
- App cannot interact with protected/UAC surfaces, higher-integrity apps in normal mode, game anti-cheat, DRM-protected content, or secure password input. Never try to bypass those.
- No admin manifest, custom driver, code injection or code signing required for development; public distribution on Windows can benefit from paid code signing and reputation. Unsigned installers may produce SmartScreen warnings—document honestly.
- UIA targets with destructive outcomes blocked by default; action is always user-initiated and no background action policy.
- Model weights and bundled codecs/licenses audited before release. Free code doesn't imply free redistribution for every asset.

## 9. Performance/quality release gates
These are **test targets**, not achieved results:
- **Mouse pass-through:** 100/100 test clicks/drag-starts to underlying ordinary apps in Passive Overlay, including while shelf visually overlaps; check real native cursor never jumps due to GHOST-only interaction.
- **Keyboard focus:** typing in test editor continues after repeated GHOST Pocket show/hide. External UIA actions tested separately; not automatically focus neutral.
- **DUAL concurrent interaction:** 30/30 independent drag sessions inside Pocket Workspace; no swapped/released targets, no stuck drag. Separate 20-trial drag in external app + ghost Pocket input test and record observed compatibility.
- **Gesture:** ≥90% success on 30 deliberate pinches across at least 2 lighting conditions, no stuck press in 100 release/loss trials, ≤1 unwanted reveal in 20 minutes of ordinary desk motion during tuning.
- **Latency:** initial target visible response median ≤120ms; 95th-percentile ≤220ms on target laptop. Test camera capture-to-pixel not just rendering loop. Record CPU, RAM and frame pacing.
- **Rendering:** target 60fps while shelf animates on chosen Windows 11 test laptop; monitor p95 frame time ≤20ms at 60Hz. If high refresh display, target pacing relative to display. Degrade transparencies/shadows before reducing interaction responsiveness.
- **Persistence:** save/restart/delete/refetch validated, bounded disk cache, atomic saves.
- **Offline:** camera and snapshot work with network disconnected after assets are installed.
- **Clean machine:** app launches without installed Python SDK/IDE and without admin elevation.
- **Crash:** kill tracker/helper repeatedly, verify overlay/recovery and native mouse unaffected.

## 10. Definition of Done
No phase marked complete without: source committed, deterministic automated tests for pure logic, Windows reproducible manual test protocol, observed result evidence (video/logs without sensitive user content), feature flag and fallback, updated compatibility matrix, no known P0 crash/focus/cursor violations, and performance measurement on test hardware. Codex must never write PASS if it did not actually execute the test on Windows.

## 11. Decisions still requiring actual test data
A. WPF layered transparent overlay and click-through behavior on Windows 11, including UIA target exclusion.
B. UIA action compatibility/focus preservation in chosen app targets.
C. Rendering performance of WPF on integrated-GPU laptop; switch overlay backend only if data justifies it.
D. Python MediaPipe packaging/version/license availability and camera stability.
E. Screen capture source restrictions, scaling and multi-monitor behavior.

## 12. References (official technical docs)
- Win32 extended styles: https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
- Layered windows/hit testing: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
- Microsoft UI Automation threading: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading
- WPF layered windows: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/technology-regions-overview
- Windows Graphics Capture: https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture
- MediaPipe Python Hand Landmarker: https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python
- Composition visuals/performance: https://learn.microsoft.com/en-us/windows/apps/develop/composition/visual-layer

**Project governance:** This file and `AGENTS.md` supersede v1. `archive/v1/` is historical context only, excluded from the Codex handoff ZIP.
