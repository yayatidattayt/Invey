# Architecture Decision Records — initialize from v2 design

## Execution 10 — 2026-11-06

**ADR-37 — Separate geometry, practice and source activation.** Map to one display's local DIPs and physical bounds, with explicit mirror and invertible sensitivity. Persist only numeric calibration settings. A continuity break disarms practice, and a new valid open-hand dwell is required. Two detected hands produce an ambiguity marker. [Execution 11 report](EXECUTION_11_REPORT.md)

**ADR-36 — Separate product identity from compatibility identities.** Public brand is Invey. Preserve existing executable, namespaces, endpoints, mutex and repository path. The first F07 slice uses ephemeral landmark geometry with no raw pixels over the tracking pipe. [Execution 10 report](EXECUTION_10_REPORT.md)

## Execution 09 — 2026-11-06

**ADR-35 — Keep the verified Media Foundation worker and record staged local evidence.** The ASUS FHD camera passed the actual WPF/MediaPipe path with YUY2 to RGB32. Keep separate activation, reader, delivery and conversion failures. Diagnostic sessions use explicit Enable or Restart, a visible bounded run and image-free metadata. [Hardware report](EXECUTION_09_REPORT.md)

## Execution 08 — 2026-11-05

**ADR-34 — Provisional F06 authorization and single native source.** Execution 08 authorizes camera adapter and UI development ahead of the earlier development sequence. Gate A and M02 remain acceptance dependencies. Use one Media Foundation Source Reader inside the supervised Python worker. Establish COM MTA before MediaPipe imports and use the standard RGB32 converter that passed controlled-file validation. Keep device consent separate and enforce observed worker exit before replacement. [Decision and evidence](EXECUTION_08_REPORT.md)

## Execution 07 — 2026-11-05

**ADR-33 — Share the serial VIDEO frame path and review optional ordinary-app control separately.** Keep consumer ordinary-app UIA disabled and recommend formal deferral at the M02 review after A1. Use an explicit stale-result state independent of health. Prefer Windows Media Foundation for a single device adapter after the gate. Its device identity, negotiated formats, errors and teardown require authorized validation. [Decision and evidence](EXECUTION_07_REPORT.md)

## Execution 06 — 2026-11-05

**ADR-32 — Prove the camera-free observation lifecycle in the app.** Use the pinned CPU VIDEO worker with controlled frames and share the runtime-neutral session and wire validation. Wait for the start sentinel after Windows job assignment, validate the connecting process, and require observed exit before replacement. Keep only current observation state in WPF. Gate A and M02 disposition still govern device integration. [Architecture and evidence](EXECUTION_06_REPORT.md)

## Execution 05 — 2026-11-05

**ADR-31 — Qualify the tracker behind a runtime-neutral boundary.** Keep MediaPipe Tasks CPU as the M03 candidate after the exact Windows wheel and model passed a still-input qualification. Compare capture backends on authorized hardware after Gate A. Keep a bounded, landmark-only wire protocol and a session-scoped latest observation. Reconsider the native or ONNX path if deployment cost or observed performance warrants it. [Decision and evidence](EXECUTION_05_REPORT.md)

## Execution 04 — 2026-11-05

- **ADR-29 — Preserve the original mutation outcome.** Use explicit dispatch classification and separate read-only observations. One unresolved operation per fixture client holds further mutations until deliberate dismissal. At most three observations use the original identity and a bounded deadline.
- **ADR-30 — Quarantine on teardown uncertainty.** Preserve the process reference and block replacement until exit is observed. Discard stale capabilities and accept replies only on the active request's pipe.

## Execution03 reliability decisions — 2026-11-05

- **ADR-25 — Authorized root before element discovery.** Resolve descendants of an authorized fixture HWND; require one ID match, matching process lifetime and element state, root/element runtime identities, unchanged physical bounds and point containment. Duplicate IDs reject even when geometry might disambiguate. This is intentionally conservative. No desktop-global fallback; obscured/virtualized/unknown applications remain unsupported until an adapter is verified. Enumeration is confined to controlled small fixtures and bounded in time by the worker watchdog; this is not a general large-tree adapter.
- **ADR-26 — One active RPC, no waiting queue.** Busy for concurrent callers, write/read deadline, exact correlation, bounded history that fails closed at 4096 IDs, canonical GUID dedup, EOF/malformed/partial/oversized failure and observed process exit. Client retains request history and target start-time binding across worker restarts. Tokens exist only in one worker, expire after 300ms, and are removed before execution. There is no automatic action retry.
- **ADR-27 — Real provider containment, honest action uncertainty.** A guarded separate Pocket.App provider process blocks a real AutomationPeer query; GHOST's dispatcher stays independent. Query timeout/cancel terminates the disposable worker. Execute failure after dispatch reports OutcomeUnknown; process termination cannot promise rollback in the provider. Invoke remains Attempted; readback for other patterns must establish a change or requested value before Confirmed. Generic transactional action cancellation remains outside verified capability.
- **ADR-28 — Bounded profile evidence, no gate waiver.** Short/standard/extended use 20/60/100 cycles, seeded variable text, exact serial digest acknowledgement and lifecycle boundaries every20 cycles. Private-desktop budgets150/240/360s, restoration watchdog5s later. Standard WinForms/Win32 fixtures expand provider coverage without touching unrelated applications. Native, injected and deterministic evidence stay separate from physical acceptance. No B1/camera/capture work.


## Autonomous Execution02 —2026-11-05

ADR-21: latest explicit creator instruction permits isolated test-only OS injection and eligible A2-PROVISIONAL engineering; it does not relax full physical/release gates. SendInput only in tests/Pocket.Acceptance.Runner, private desktop/foreground PID guarded, supervisory desktop restoration and job-owned process cleanup. No consumer pointer fallback or unrelated UI input.

ADR-22: machine-readable classes/provenance and separate executable-subset vs full-gate verdict. Cursor three-scenario no-injection control, independent fixture event counts and SHA256 text acknowledgment. Correct Unicode desktop-name marshalling and host DPI/manifest assumptions; per-chunk digest ack after preserved intermittent mismatch. Use manifest-bearing App.exe for test host; runner explicitly per-monitor aware.

ADR-23: fixture-only A2 client/contracts/worker with bounded IPC, MTA/no windows, fixed PID/window/control registry,300ms runtime-ID single-use tokens, revalidation and watchdog. Underlay Unsupported remains unavailable; no unsafe fallback. Worker timeout double is distinct from real-provider hang verification. Public UI/general app adapters deferred.

ADR-24: initialized local project repo on dedicated codex/a1-automation-a2-provisional, no commits/merges assumed; archived pre-A2 source snapshot for recovery. Per-command exact-root safe.directory only, no global trust or home-repo modifications.

## 2026-11-05 — Execution 01.5 diagnostic hardening

ADR-19: preserve native evidence per UTC timestamp/GUID run directory instead of replacing artifacts/native-smoke.json. Explicit native Status separates invariant failure from ambiguous cursor/foreground changes; overlay becoming foreground is a hard invariant failure. Native exit0 is not A1 sign-off. No input style/architecture change.

ADR-20: separate canceled/active physical fixture drag diagnostics and dispatcher-local replay observation event. Allows actual press-driven stop/restart native test without sleeps/hooks/injection. Two new pure tests cover opposite-owner release/cancel and non-resurrected released target;17 tests per configuration pass. Physical diagnosis still awaits human reports.

## 2026-11-05 — A1 implementation

ADR-16: verified project-local SDK10.0.403 and global.json, project-local CLI/NuGet wrapper. Avoid machine PATH changes. Interrupted extraction repaired with path-validated ZIP extraction. Debug/Release build and15 tests pass.

ADR-17: repair foundational per-pointer/per-target claims now because existing global Owner was incompatible with A1 ownership testing. Only one panel exposed; full dual Workspace remains later. Deterministic tests prove claim independence, offsets/clamp, occupied-target rejection and invalid input.

ADR-18: report native sampled styles/focus/cursor/teardown without certifying physical trials. Retain initial inconclusive smoke; later harness records exact baseline/fixture/overlay handles. Full A1 remains blocked. No injected input/camera/UIA.

| ID | Decision | Rationale | Validation needed |
|---|---|---|---|
| ADR-01 | Windows 11 x64 native .NET 10 WPF + Win32 styles first | Lowest integration uncertainty for classic Windows overlays | Layered/noactivate/pass-through HWND proof and frame pacing |
| ADR-02 | GHOST as independent app-owned pointer, not second OS cursor | Preserves mouse state without drivers; supports Pocket-owned actions | Dual drag in Workspace; native mouse no-jump tests |
| ADR-03 | Click-through Passive Overlay separate from normal Workspace | Prevents main desktop click/focus interception | 100-click, 20-focus and drag tests |
| ADR-04 | UIA capabilities through isolated helper process | Avoid UI blocking and COM hang poisoning | Fixture, cross-app focus test and hang recovery |
| ADR-05 | Python MediaPipe sidecar only after Windows Gate A | Fast CV iteration; optional later packaging optimization | Camera reliability, clean installer, model licensing |
| ADR-06 | Snapshot-only first release | Universal live crops/player extraction cannot be guaranteed | Confirmed crop workflow and persistence tests |
| ADR-07 | Interactive Peek latches temporarily | Open-palm-hold and pinch cannot be performed simultaneously with same hand | Usability test: pull → pinch card → close |
| ADR-08 | Small, quiet GHOST ring rather than neon cursor | Premium, usable, avoids distracting physical mouse tasks | Usability testing and contrast |
| ADR-09 | Transforms/opacity, perf budget and measured fallback renderer | Smoothness is testable, not assumed | p95 frame benchmark, visual regression |

Add ADRs for any framework or API change with date, experiment, observed outcome and rollback plan. DO NOT write unverified PASS.
| ADR-10 | Research log becomes a living, dated source-anchored risk register | New OS/media/packaging findings must modify tests and decisions, not disappear in chat | Compare research R01–R28 against actual Windows test artifacts; update docs/research.md after each gate |

## 2026-11-04 — Specification reconstruction decisions

- **ADR-11 — Preserve .NET10 baseline:** net8 source is drift, not approved architecture. Missing SDK blocks builds. Future A1 repairs target net10.0/net10.0-windows and valid solution/imports before native tests. Evidence: project files and `dotnet --info`; Microsoft support policy opened during review. No source repair executed here. Reversal requires an explicit supported-runtime ADR, not runtime availability alone.
- **ADR-12 — Prove simulation without foreground controller:** timed internal replay/countdown is the A1 proof mechanism; local keyboard nudges may remain developer convenience. Evidence: MainWindow PreviewKeyDown requires focus. A03–A05/S02 still NOT RUN. Roll back replay script on defect, never inject global input.
- **ADR-13 — Per-target claims and separate pointer state:** current single Owner cannot represent distinct-target simultaneous drags. Replace in future core implementation; invariant/property tests and C09/30 physical sessions required. Roll back implementation to last verified core if regression; do not weaken product concurrency requirement.
- **ADR-14 — Supplemental directive, preserved authority/history:** new codex directive and SPECIFICATION_REVIEW resolve documented conflicts while retaining active canonical master/research/evidence and original ZIP. GHOST broader brand does not add cloud/AI/accounts. Proposed tuning and conveniences remain proposals. Revisit branding only if creator requests material identity change.
- **ADR-15 — Explicit local transaction and privacy lifecycle:** PNG/manifest uses ordered commits, backup/generation and orphan recovery; durable tombstones define Undo retention and Delete all scope. Design mitigation only, S08/S09 unrun. Choose simple manifest before database; revisit on measured transaction/scale requirements.

## 2026-11-06 — Invey desktop identity

- **ADR-19 — Separate product, subsystem and compatibility identities.** Invey owns the executable, solution, shell identity and new profile root. Pocket, Dual and Ghost retain subsystem names. Keep the shared legacy mutex and reopen event across executable names so a transition cannot produce two writers in the same Windows session. Publish a verified copy of a legacy profile and retain the original. An existing Invey profile takes precedence. A rollback may run the old executable against its original profile after Invey exits. Invey-only edits stay in the new profile. The execution report records the fixture results and open acceptance cases.

## 2026-11-07 — Pocket integration

- **ADR-20 — Tuck a real native window.** Keep the library and floating cards independently owned. Tuck hides the shelf HWND, keeps its selection and leaves explicitly pinned cards visible. Prefer immediate transitions to invisible hit regions or asynchronous fades.
- **ADR-21 — Reserve shortcuts before committing.** Use a message-only native window and RegisterHotKey with MOD_NOREPEAT. Keep the previous map active while reserving replacement chords and publishing the validated settings file.
- **ADR-22 — Preserve Dual ownership in Workspace.** Bind both application-owned targets to the existing reducer. Production uses ordinary mouse and keyboard. The isolated logical Ghost stream proves independence without changing the physical cursor or sending actions to other apps.

## Execution 16 continuation — 2026-11-08

See [Execution 16 evidence](EXECUTION_16_REPORT.md) and [finger research](FINGER_TRACKING_RESEARCH.md). The real camera preview and aligned finger dots have user confirmation. Bounded capture, sparse model points, explicit per-finger states and an independently supervised versioned diagnostic budget are implemented. Full physical B1, Gate A/M02 and sustained finger-accuracy acceptance remain open. All failed runs and repairs are preserved. The final standard regression passed37 cases with zero failures and Explorer blocked; both configurations passed100 core tests and all20 worker tests passed. Draft PR5 is published and implementation455abf4 passed Windows CI. The user reported wrist-out index loss. Complete continuation acceptance remains CHANGES REQUIRED.

## Execution17 — camera-free demo stabilization (2026-11-08)

Main navigation, explicit isolated sample profile, launcher and labeled recorded/simulated entry points reuse existing implementations. Pinning now handles checked-state changes, including accessibility Toggle. Two seeded supplemental WPF-control/GDI/model/restart rehearsals passed. They do not replace native input acceptance: the final standard run was refused at foreground ownership by the existing safety guard. Debug/Release100 core tests and20 worker tests pass; readiness is PARTIAL with a manual remote-session checklist. No webcam acquisition or normal/legacy library migration was performed. See docs/EXECUTION_17_DEMO_REPORT.md and docs/DEMO_FEATURE_INVENTORY.md.
