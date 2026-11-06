# Feature inventory, product improvements and scope discipline
The product is a **useful utility with a clever second input**, not a gesture-showcase application. Every feature must earn its place by reducing tab switching, permanent screen clutter or interruption.

## The five defining actions
1. **Keep** — save a user-selected screenshot to a Pocket card. User confirmation required.
2. **Tuck** — hide the Pocket shelf without deleting items. Visually leave only a slim edge handle.
3. **Peek** — reveal saved references through a deliberate GHOST pull gesture or hotkey. Supports read-only glance and interactive latched peek.
4. **Work simultaneously** — GHOST manipulates a Pocket item while physical mouse continues ordinary desktop work. Within Workspace, both streams can drag separate Pocket objects concurrently.
5. **Recall / Pin** — bring an item back where it is useful; keep one panel visible, then dismiss it without disturbing the mouse.

## Supporting features that belong in v1 if feasible
| Feature | Why it belongs | Implementation / test boundary |
|---|---|---|
| **Quick-slot 1–3** | Frequent references should be one action away | Numbered Pocket cards/shortcuts; explicit ordering and simple hover/select state |
| **Temporary Pocket** | Some screen details shouldn't live on disk | RAM-only card; marked temporary; cleared on exit; never leak into persisted thumbnails |
| **Smart edges** | Floating panels shouldn't vanish offscreen | Clamp, snap-to-edge, remember placement; only selected monitor in v1 |
| **Quick compare** | Two snapshots are often consulted together | Workspace split-view of two saved images; mouse pans one, GHOST pans the other (optional after v1 core test) |
| **Source breadcrumb** | Users want to remember where a snapshot came from | Store process/window label with explicit caveat; clickable link only when verifiably available, never invent URL |
| **Pause on demand** | Webcam fatigue/privacy and accidental inputs | Accessible global hotkey and clear indicator; camera off when requested |
| **No-camera mode** | Broad audience beyond gesture fans | Full capture/library/shortcuts normal UI |
| **Session restore** | People expect references to persist | Atomic local assets and manifest, missing-source recovery |
| **Hand target magnetism** | Small controls hard to grab midair | Expand virtual hit regions around eligible Pocket targets; never magnetize hand to external app actions without target validation |
| **Action feedback** | No one should wonder whether a pinch worked | 1 quiet state change, no AI toast, separate success/unsupported |

**Feature priority order:** desktop independence, useful Snapshot flow, minimal hidden shelf, stable gesture recall, reliable pin/drag, keyboard fallback, then these supporting conveniences. Do not sacrifice the core to implement a dozen shortcuts.

## Explicitly deferred features
- Live updating window preview (after DWM/Windows Graphics Capture compatibility test)
- YouTube/local video (after official supported player experiment)
- Fully customizable gesture macros (risk of accidental destructive actions)
- Semantic third-party app control beyond the A2 fixture and verified allowlist
- Any general "AI can see what you're doing" claims
- OS-wide arbitrary click/drag/scroll by GHOST
- Collaboration, accounts, cloud sync, public marketplace, monetization

## Feature state labels in settings/help
- `Available`: implemented and passed stated tests for specified Windows configuration.
- `Experimental`: opt-in, exact supported apps/actions listed, may affect focus.
- `Coming later`: roadmap only, no visible fake control.
- `Unavailable`: no supported technical route or current state failure; helpful explanation and alternative.

## User experience examples for broad audiences
- **Studying:** excerpt from PDF remains hidden; reveal briefly while answering questions in a different window.
- **Shopping:** save size chart and product specs separately, compare in a workspace while browsing other pages.
- **Following instructions:** keep a photo of a setup step, use physical mouse in the app being configured, reveal reference by hand.
- **Cooking/DIY:** one reference image and checklist visible on demand (not an automatic video extraction promise).
- **Office work:** keep an internal diagram accessible without permanent always-on-top clutter.

## Why not ordinary PiP/PowerToys only?
Microsoft PowerToys Crop and Lock already does snapshots and cropped live windows. OnTopReplica already supports cropped live previews and click-through. POCKET cannot claim these individually as inventions. The user-value hypothesis is *transient recall + an independent GHOST channel that can manipulate references without borrowing the mouse*. Benchmark it, and provide keyboard mode for users who find gesture control tiring.

## Potential future competitive benchmark
Give five users three tasks: create reference, retrieve reference while keeping a physical mouse action active, and dismiss it. Compare with PowerToys crop + window switching and normal keyboard shortcut approaches. Measure time, errors, preference, hand fatigue, and whether they use the webcam again next day. If no clear benefit, retain Pocket as a keyboard-first utility and reconsider DUAL's role.
