# Gap Audit — previous version to v2
**Audit date:** 2026-11-04. **Labels:** `DESIGNED` = mitigation documented, not tested; `BLOCKED` = cannot universally resolve under constraints; `TO VERIFY` = OS experiment needed.

| Problem in earlier plan | Concrete repair in v2 | Status |
|---|---|---|
| Browser playground gave false confidence about Windows independence | Native click-through overlay and actual focus+mouse tests are Gate A, before webcam | DESIGNED / TO VERIFY |
| Treating two pictures of pointers as two real mouse devices | GHOST has its own app-level pointer state, hit-testing and ownership; mouse stays OS-owned | DESIGNED |
| Claiming GHOST can click/drag every Windows control | External actions capability-checked via UIA, experimental, skip unsupported; no global pointer teleport | BLOCKED universally |
| UIA `ElementFromPoint` may hit our own overlay | Underlay resolution experiment, process/handle filtering, explicit unsupported fallback | TO VERIFY |
| UIA provider hangs even when Task has timeout | Restartable dedicated helper process on COM MTA thread; kill/restart on watchdog expiration | DESIGNED / TO VERIFY |
| External app UIA might change focus during mouse drag | Measure keyboard focus and native drag separately per app; disclose incompatible actions | BLOCKED universally |
| Transparent windows might intercept clicks | Layered + transparent + no-activate styles and actual 100-event Windows test | TO VERIFY |
| Capture could include its own selection overlay | Hide overlay, capture frame first, THEN show frozen screenshot for selection | DESIGNED |
| Camera position may jitter, pinch misfire | Per-user calibration; normalized pinch; timestamp hysteresis; One Euro filter; oversized hit zones | DESIGNED / TO VERIFY |
| Camera can drop or swap hands | Single-hand locking, fresh-arm requirement after ambiguity, cancel-on-loss and process watchdog | DESIGNED / TO VERIFY |
| Gesture hold conflicts with same-hand pinch | Interactive Peek latches after pull; explicit dismiss/idle timer; separate passive Glance | DESIGNED |
| Midair two-finger scroll is less stable than mouse wheel | Use pinch-grab scrolling rail first; extra gesture optional | DESIGNED |
| UIA/probe or CV blocked UI rendering | Worker/process isolation, bounded latest-frame IPC, asynchronous render updates | DESIGNED / TO VERIFY |
| "Smooth" without frame data | Motion token budgets, compositor-friendly transforms, debug FPS/latency view, render benchmark and fallback renderer | DESIGNED / TO VERIFY |
| Generic neon AI-style visuals | Quiet Instrument typography, palette, minimal surfaces, GHOST states, tested mock | DESIGNED / preview smoke tested |
| Feature sprawl: live, video, arbitrary Windows control at launch | Snapshot-only fully delivered v1; other adapters gated individually | DESIGNED |
| Snapshot storage could leak full-screen images | Crop in memory, dispose full buffer, atomic local manifest, temporary in-memory pockets | DESIGNED / TO VERIFY |
| Python sidecar packaging assumed effortless | Clean-machine gate, pinned dependencies, model license audit, portable bundle, crash supervision | DESIGNED / TO VERIFY |
| Multi-monitor and DPI mismatches | Single monitor v1; per-monitor aware physical vs DIP conversion tested; multi-display deferred | DESIGNED / TO VERIFY |
| User must wave for every interaction | Mouse+keyboard-first; webcam optional, click-through, clutch/pause, large touch targets | DESIGNED |
| UI prototype could be mistaken for finished product | Explicitly labeled HTML design concept and separate native Windows build gates | DESIGNED |
| No objective proof that product beats existing tools | Benchmark with PowerToys Crop and Lock / OnTopReplica / mouse-only shortcut workflows | TO VERIFY |

## Limitations that no honest "perfect alternative" can erase
- Some Windows apps simply do not expose automation for arbitrary controls. The only safe general fallback is no external action and a clearly indicated unsupported state. Drivers, synthetic pen and input injection do not make universal compatibility trivial.
- GHOST and physical mouse can be truly independent within POCKET-owned UI; across arbitrary windows, compatibility and foreground behavior depend on each app and Windows.
- Captured screenshots are static; live crop requires a distinct API and cannot operate every app, minimized source, DRM-protected video, or window surface.
- Camera tracking depends on lighting/occlusion and is unsuitable for prolonged fine motor accuracy. Mouse/keyboard fallback is not optional.
- Motion smoothness varies by hardware. Promise measurement and graceful degradation, not universal 60fps.

## Non-negotiable decision logic
1. If overlay fails pass-through/focus tests, repair the window topology; do not implement webcam yet.
2. If UIA cannot resolve underlay reliably, disable external actions; Pocket must still work.
3. If tracking fails on a laptop, retain mouse-only mode and provide calibration; do not upload video to a paid service.
4. If WPF rendering stutters, measure and optimize. Only then benchmark DirectComposition for the overlay.
5. If live/video cannot be built correctly, label deferred and do not ship a nonworking button.
