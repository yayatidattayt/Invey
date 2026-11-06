# START HERE — POCKET × DUAL × GHOST v2.1 (Codex-ready)

**What this ZIP is:** A complete, research-updated **project specification and first-session Codex handoff** with a working browser-only design concept. **What it is not:** an already built or Windows-tested executable. Codex will build the real desktop application phase by phase.

**New master project context:** `CODEX_PROJECT_MASTER_PROMPT.md` is the comprehensive, persistent charter for Codex Project instructions. Use it when creating/configuring the project. `CODEX_START_PROMPT.md` remains the separate first *implementation* command, scoped to Gate A1. If project-instruction fields have length limits, keep the full master prompt as a repository file and ask Codex to read it before implementation.

## Do this first on Windows 11 x64
1. **Create** the folder `Documents\ghost`, and extract this ZIP **directly into it**. Make sure `Documents\ghost\AGENTS.md` exists (not `Documents\ghost\POCKET_DUAL_GHOST_V2\AGENTS.md`).
2. Open **`Documents\ghost`** directly in VS Code / Codex as the repository root.
3. Run the read-only developer check from PowerShell (Windows SDK/.NET checks, no screen or camera access):
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preflight-Windows.ps1
   ```
   If the .NET 10 SDK is missing, install it from the official Microsoft .NET download before the native build. Python is not required for the initial Gate A1 overlay prototype.
4. Open `CODEX_START_PROMPT.md`, **copy its entire code-block prompt into Codex** and let it implement Gate A1 on the Windows machine.
5. Run the phase's build, unit tests and actual Windows manual tests. Record real results in `docs/TEST_RESULTS.md` and `docs/COMPATIBILITY_MATRIX.md`. Use the REVIEW prompt from `docs/CODEX_PHASES.md` before advancing.
6. After A1 has been verified, continue with A2, B1, B2, C, D1 and D2, **one gate at a time**. Optional E integrations are deferred.

## Essential read order for engineering
1. `AGENTS.md` — nonnegotiable developer rules.
2. `docs/research.md` — **canonical research log**; 28 documented limitations, mitigations, official sources, regression tests and dated changelog. `research.md` in the ZIP root is a synchronized mirror.
3. `docs/MASTER_SPEC.md` — what the final product must do and deliberately will not do.
4. `docs/IMPLEMENTATION_RFC.md` — native Windows architecture, worker processes, capture, coordinate handling and security.
5. `docs/QA_MATRIX.md` + `docs/TEST_RESULTS.md` — acceptance criteria and evidence log (all native Windows tests initially NOT RUN).
6. `docs/DESIGN_SYSTEM.md` + `design/DESIGN_TOKENS.json` — minimal Quiet Instrument visual design and motion.
7. `docs/CODEX_PHASES.md` — detailed prompts for every stage, optional meta-prompt and REVIEW process.
8. `docs/ACTION_PLAN.md`, `docs/FEATURE_SCOPE.md`, `docs/GAP_AUDIT.md`, `docs/DECISIONS.md`, `docs/COMPATIBILITY_MATRIX.md` — scope, evidence and decision history.

## Optional: inspect the approved visual direction
Open `design/UX_CONCEPT.html` in a modern browser. This is a **visual/interaction mockup only**, not native Windows code; it does not use webcam tracking or Windows UI Automation. Compare native results with the two included PNGs during styling.

## Keep research updated
Append dated findings to `docs/research.md` when tests uncover new failures, with OS version, reproduction, source, remedy and test IDs. Then run `scripts/Sync-Research.ps1` to refresh the root `research.md` copy. Never overwrite failed tests with invented successes.

## Important limitations
- GHOST will control Pocket-owned actions independently of the physical Windows cursor. **It is not a second global native Windows mouse.**
- Actions in other apps are experimental UI Automation capability checks; some will fail or affect focus. Unsupported controls must be visibly unavailable, not replaced by native cursor hijacking.
- First release is snapshot-based; no unsupported live crops, arbitrary YouTube extraction or universal game interaction.
- No paid API, admin requirement, driver, cloud or extra hardware is mandatory for the targeted local-first MVP, assuming the user already has a Windows PC, webcam and mouse.

## Scope in one line
**First prove mouse-pass-through and simultaneous Pocket-owned GHOST manipulation on real Windows. Then connect webcam tracking. Then build the hidden snapshot shelf. Polish and package last.**
