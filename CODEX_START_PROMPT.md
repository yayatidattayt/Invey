# Start Codex here — FIRST implementation session

> This document is the immediate build prompt. Use it **after extracting the ZIP** and opening `Documents\ghost` itself as the Codex workspace. Do not upload an isolated copy of this prompt without the files it references.
>
> **Status: specification and verified mockup only. There is NO Windows application binary yet.** Your first real outcome is a Windows feasibility prototype, not a finished product.

## Copy everything below into Codex

```text
You are the implementation engineer for the Windows desktop project POCKET × DUAL × GHOST.

READ FIRST, FROM THIS REPOSITORY:
- AGENTS.md (binding development rules)
- docs/research.md (dated source-anchored risk register)
- docs/MASTER_SPEC.md (product contract)
- docs/IMPLEMENTATION_RFC.md (technical design and process boundaries)
- docs/DESIGN_SYSTEM.md (Quiet Instrument UI and motion rules)
- docs/FEATURE_SCOPE.md, docs/QA_MATRIX.md, docs/COMPATIBILITY_MATRIX.md
- docs/DECISIONS.md, docs/TEST_RESULTS.md, docs/ACTION_PLAN.md
- docs/CODEX_PHASES.md (full sequence and phase-specific prompts)
- VERIFICATION.md (truth about what has/hasn't been tested)

PRODUCT INVARIANTS:
1. The normal physical Windows mouse must remain fully OS-controlled. Never teleport it, swap cursor positions, or use global synthetic mouse clicks as a shortcut for GHOST.
2. GHOST is a separately tracked floating object with independent state, capable of operating POCKET's own UI while the physical mouse works elsewhere.
3. The product is a beautiful, restrained, real Windows desktop utility: store screenshot references in a hidden shelf and recall them by deliberate hand gestures or keyboard shortcuts.
4. External-app UI Automation is an optional, capability-limited, separately tested integration. Do NOT claim GHOST is a universal second native mouse.
5. Local-first, free to run without paid services, no mandatory admin permissions, no screen/camera uploads, no invasive hacks.
6. All features must be functional in their declared supported scope, with real failure states and no pretend buttons. The camera is OFF by default.

THIS SESSION'S ASSIGNMENT:
A. Inspect the real development machine and report Windows version/build, architecture, .NET SDK presence, available developer tooling. Do not start the camera or capture the screen without user opt-in.
B. Cross-check high-risk assumptions in docs/research.md and confirm the gate plan. If a documented fact is contradicted by your real environment, write a dated finding in docs/research.md and a justified ADR in docs/DECISIONS.md.
C. Implement ONLY Gate A1 from docs/CODEX_PHASES.md: smallest C# / .NET Windows app (WPF + Win32 as justified by an ADR) with a passive click-through, non-activating overlay; visible independently movable *simulated* GHOST; one POCKET-owned movable panel that GHOST can manipulate through internal hit-testing; native mouse remains untouched.
D. Write automated deterministic tests for the independent input state and one representative interaction, and provide exact build/run/test commands.
E. If on actual Windows, execute build and automated tests, then provide a reproducible human Windows QA checklist from docs/QA_MATRIX.md. Do NOT claim focus/drag independence PASS until it is actually observed, with app/build/monitor evidence recorded in docs/TEST_RESULTS.md and docs/COMPATIBILITY_MATRIX.md.
F. Review the diffs and report exactly what works, what doesn't, what was not tested, and the next release-blocking problem. Stop before webcam integration or A2 UI Automation; they are separate gates.

ENGINEERING QUALITY:
- Keep pure pointer/ownership logic testable and independent of WPF.
- Use defined typed coordinate spaces; do not silently mix WPF DIPs and physical pixels.
- Window geometry/Win32 styles must be asserted on Windows, not just named in source code.
- Avoid capture hooks, drivers, UIAccess, elevation bypass, global SendInput and anything that interferes with the Windows mouse.
- Process errors must be visible and recoverable. UI polish should follow the existing design tokens without delaying proof of pass-through and focus.
- Maintain a CHANGELOG entry and update test statuses accurately. Never fabricate test results, model FPS, or screenshots.
- If the tool environment is not Windows, do not simulate a Windows success: create/scaffold what is portable and provide exact Windows verification steps.

START NOW. Don't ask unnecessary questions. Do not generate a new abstract prompt pack instead of implementing Gate A1.
```

## What to do after Gate A1

Run the `REVIEW` prompt in `docs/CODEX_PHASES.md`, resolve P0 issues and record results in `docs/TEST_RESULTS.md`. Then paste the next phase prompt (`PHASE A2`). Do not ask Codex to implement A1 through E in one go.
