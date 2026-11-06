# Design System v2 — "Quiet Instrument"
**Intent:** understated, premium Windows software: functional first, cinematic only at the moments of reveal. Looks authored by a thoughtful human product designer, *not* a generic generative-AI dashboard. Every animation must teach state, hierarchy or causality.

## Visual north star
Think a precisely machined instrument panel merged with an editorial reading app. Calm, solid, subtle detail. No neon cyberpunk theme; no glassmorphic rainbow gradients; no enormous cards, sparkles, floating AI mascot, oversized glowing orb, gratuitous animations, emoji-driven navigation, or fake charts. Avoid decorative blur behind every surface. Do not blindly clone Apple, Windows Fluent or Arc; use native Windows affordances where appropriate.

### Information hierarchy
- **Layer 0, normal desktop:** absolutely unchanged by default.
- **Layer 1, dormant:** optional 3px progress-like accent on a 10–14px right-edge grab tab, no GHOST rendered unless armed.
- **Layer 2, peek:** a single 348–396px floating tray, selected item large enough to read, low visual density; background desktop remains visible, native mouse input passes through it.
- **Layer 3, pinned:** one unobtrusive reference window with precise trim, subtle shadow, small handle and title. Multiple pinned items snap to edges and avoid overlap if space permits.
- **Layer 4, deliberate workspace:** normal accessible window containing library, settings and direct mouse+hand manipulation; traditional top bar, search, thumbnails and list density.

### Palette tokens (light/dark)
| Token | Dark | Light | Purpose |
|---|---|---|---|
| `canvas` | `#14191C` | `#F4F4F0` | Pocket workspace background |
| `surface` | `#202729` | `#FFFFFF` | Cards / panels |
| `raised` | `#283135` | `#E9ECE9` | Active tile / top bar |
| `border` | `#384347` | `#D3DAD7` | 1px dividers and outlines |
| `text` | `#EDEEE8` | `#182223` | Main text |
| `muted` | `#9DA9A8` | `#5A6866` | Secondary text |
| `accent` | `#96CDB9` | `#327B69` | GHOST active and selected controls |
| `accentText` | `#143C31` | `#FFFFFF` | On accent fills |
| `danger` | `#E09C92` | `#B84E46` | Delete/error only |
| `scrim` | `#071011` at <=14% | `#182220` at <=8% | Optional separation; never darken entire desktop in Peek |

Accent is reserved for active GHOST, current selection, progress and the one primary action. Most of the app should use neutrals. Contrast text and focus indicators to WCAG 2.2 AA or platform-equivalent targets; check actual colors with contrast tooling, especially small muted labels.

### Typography
- Primary: **Segoe UI Variable** (available on modern Windows; fallback Segoe UI and system sans); no bundled third-party font file needed.
- Display brand wordmark 18–20px semibold, restrained tracking; product UI headers 15px semibold; body 13–14px; compact metadata 11–12px; never use <11px for actionable text.
- Numerals tabular for timers and positions. Sentence case everywhere. Short functional words: "Keep nearby", "Show pocket", "Pinned", "Camera off", "Unavailable".
- Avoid giant hero marketing text inside a desktop utility. No explanatory paragraphs in routine overlays.

### Geometry, density and spacing
- Eight-point spacing scale: `4/8/12/16/24/32px`; use 4px subgrid.
- Shelf width 372px default, min 296px, max min(420px, 36% of monitor) on large screens; adapt to 1366×768 and scaling. Avoid full-height unnecessarily: content-height with max 78vh.
- Corner radii: tiny 6px; cards 11px; sheet 14px; large settings panel 16px; not 30px pill cards.
- Buttons: 34–38px height; icon-only hit targets ≥40px if hand-operated, ≥32px for mouse-only. GHOST-target collision regions can be 48–56px while visual controls are compact.
- Shadows: one soft directional shadow, no glows. Border 1px. Max one strong accent per visual group.
- Snapshot cards: actual captured crop as dominant information; compact source label, timestamp, pin icon, more menu; no unnecessary AI-generated image thumbnails.

## GHOST character design
GHOST should be more recognizable than a stock mouse arrow but far less distracting than a glowing assistant. Proposed design: 18px filled sea-glass ring with a 5px dark center, 1.5px keyline. On hover near actionable targets, a small 28px contextual bezel shows an icon. On pinch, ring compresses to 16px and becomes solid; while dragging, small connector line or tension indicator to the item, not a particle trail. When paused, ghost becomes a desaturated hollow outline for 180ms then hides. When lost, ghost freezes for at most a short grace window, visually signals loss and releases held targets; do not visually fake uninterrupted tracking. External unsupported target: subtle slash on the ring; do not display noisy toast for every hover.

Core states: `dormant` hidden, `ready` faint ring, `armed` crisp ring, `hover` bezel + small target halo, `pressed` compressed, `drag` tension, `lost` subdued + cancel, `blocked` subtle restriction indicator.

## Motion system — purpose and implementation
**Principle:** animations always accompany state changes; input-following must respond immediately and not trail behind a long easing animation.

| Event | Animation concept | Timing target | Implementation | Reduced motion |
|---|---|---|---|---|
| Shelf reveal | Translate X from edge 24–32px + opacity fade; subtle spring stop | ~220–300ms | compositor-friendly transform/opacity | <=80ms fade/cut |
| Shelf hide | Translate X 16–24px + opacity | ~160–220ms | transform/opacity | <=80ms fade/cut |
| Pocket card enter | Fade + 6px upward shift | ~140–180ms | transform/opacity | opacity only |
| Pocket card reorder | FLIP transform from old to new positions | ~170–240ms | transform, not full layout interpolation every frame | immediate reorder |
| Pin | Card moves smoothly to saved desktop anchor | ~180–260ms | transform/opacity, separate element ownership | immediate placement |
| Hover | Tint or 1px border transition | ~90–130ms | brush/opacity; avoid layout | immediate highlight |
| Press | GHOST ring compresses 18→16px | ~65–95ms | scale only | shape change without animation |
| Release | GHOST returns to 18px | ~100–140ms | scale only | instant |
| Unsupported | Quiet indicator appears | ~120–160ms | fade only | static badge/icon |

Use damped spring for shelf with no overshoot or only 1–2px intentional settle; if implementation using damped physics, cap timestep and snap on completion. A sliding panel must not oscillate as if made of jelly. Never animate `Width/Height` continuously when a translated/composited layer is sufficient.

**Performance engineering:** UI thread handles semantic state, workers handle tracking/UIA. Render on display cadence, paint latest filtered coordinates (no queue backlog), use visual transforms/opacity and cached asset thumbnails; do not resize PNG bitmaps on each frame. Use a bounded FPS debug overlay in developer mode showing tracking FPS, rendering FPS, input-to-display latency, frame-pacing p95, dropped samples, CPU/RAM. Default diagnostics off.

**Frame budget:** at 60Hz display, 16.7ms per frame total; our overlay layout+draw should stay well below this where possible. Precompute geometry, freeze immutable WPF brushes, use cached drawing resources. Target p95 frame time <=20ms on target hardware during shelf animation, not a promise across every GPU. Benchmark WPF layered overlay; if its frame pacing fails, consider DirectComposition as a targeted overlay renderer while keeping WPF settings/workspace. A WinUI 3 rewrite is not the default plan.

## UX micro-interactions
1. Edge indicator subtly brightens on armed hover, never expands until deliberate pull passes threshold.
2. Peek opens with a brief soft slide, reveals most recent card first; no forced selection every time.
3. A held gesture means ephemeral Peek; a deliberate pin means persistence. A keyboard toggle creates a pinned-ish shelf state only until next toggle.
4. Selecting a card lifts it 2px and outlines it; no rotation or exaggeration. On dragging, track GHOST precisely, no easing lag.
5. Deleting a card: inline action with short Undo toast where possible; never unexpected permanent deletion on a swipe.
6. Remember a panel's last size and anchored edge; when opening on a smaller monitor, constrain it fully on-screen.
7. If unsupported third-party target, no click simulation and no fake highlight implying success.
8. Camera privacy indicator in tray + settings, and optional subtle status dot in Pocket when camera on.
9. In accessibility mode, pointer may gain high-contrast outline and 1.3× size; no essential meaning conveyed by accent color alone.
10. First launch practice is interactive and can be skipped; never auto-open webcam after onboarding unless explicitly configured.

## Settings layout (single page, not a massive dashboard)
Tabs: **General**, **GHOST**, **Shortcuts**, **Privacy**, **Diagnostics**. General: startup policy, monitor, sidebar location, theme. GHOST: camera, mirror, hand preference, activate/pause, sensitivity and calibration. Shortcuts: reveal, capture, pause, workspace. Privacy: camera local processing, snapshot location, delete-all, no uploads. Diagnostics: check overlay, tracker health, automation compatibility with deliberate test button.

## Anti-slop review checklist
- Remove every glow, animated gradient and decorative geometric texture unless it communicates state.
- One high-quality visual idea per surface, not five.
- No stock "AI copilot" language, giant purple gradients, glass cards, rounded everything, or meaningless floating feature badges.
- No "loading..." spinner after every hover. Native OS short actions should feel immediate or show targeted state.
- No motion for motion's sake. Calm when idle, precise when active.
- Product screenshots must show real app content and validated behavior; never use fabricated results as if actual.

## Source notes
- Microsoft layered windows: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
- WPF interop/per-pixel alpha: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/technology-regions-overview
- Windows composition/performance: https://learn.microsoft.com/en-us/windows/apps/develop/composition/visual-layer
- Microsoft animation optimization: https://learn.microsoft.com/en-us/windows/apps/develop/performance/optimize-animations-and-media
