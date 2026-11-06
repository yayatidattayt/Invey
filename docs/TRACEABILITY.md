# Invey — Requirement traceability

Execution 12 separates F06 worker, host and F07 UI calibration counts by session, tests controlled camera failure feedback and adds 100%/125% deterministic B03 route coverage. Physical B03, B07, the remaining real device lifecycle and full B1 stay open. [Execution 12 report](EXECUTION_12_REPORT.md)

Execution 11 adds selected-display geometry, local calibration preferences, practice and ambiguity cancellation to the F07 preview. The controlled rectangle route and recorded WPF mapping have evidence. B03 physical rectangle-route, B07 physical multi-hand cases and full B1 remain open. [Execution 11 report](EXECUTION_11_REPORT.md)

Execution 09 provides F06 real-device evidence for explicit ASUS FHD activation, local inference, Stop, clean release and same-device reacquisition. Recorded, injected and real-device evidence remain separate. Hardware denial, disconnect and full B1 acceptance remain open. [Hardware report](EXECUTION_09_REPORT.md)

Execution 08 implements provisional F06 device discovery/selection, explicit opt-in, native camera ownership and the existing MediaPipe-to-WPF lifecycle. Native file-reader and discovery evidence are separate from B01/B02/B08/B09/B12 real-device acceptance. F06/S05 now distinguishes partial observations without exposing out-of-frame coordinates and requires independent worker exit for restart. A1 physical acceptance and M02 remain open. [Execution 08 matrix](EXECUTION_08_REPORT.md#g-test-results)

Execution 07 adds deterministic frame format, capture order and cancellation evidence and a native real-inference stale-result/recovery test for F06/S05. Restart held while the old worker was alive. Controlled source and approved-device evidence remain separate. B01/B02/B08/B09/B12 device acceptance remains open. F05/A09 disposition is recommended deferral, pending reviewed A1 and formal M02 review. [Test and gate status](EXECUTION_07_REPORT.md)

Execution 06 advances the camera-free lifecycle subset of F06/S05. Real model output reached the existing WPF app through the bounded pipe and session validator. Native tests confirmed loss clearing, distinct restart generations, Stop during startup, invalid IPC, worker termination and app exit. B01/B02 actual opt-in, B08 camera disconnect/busy behavior, B09 kill during live dragging, F07 calibration and full B1 acceptance remain open. [Execution 06 test matrix](EXECUTION_06_REPORT.md#f-test-matrix)

Execution 05 added F06/S05 observation validation and session lifecycle foundations with synthetic tests over a local pipe and separate real-model inference. B08/B09/B12 have simulated state evidence only. F06 camera activation and F07 calibration remain open. F05 controlled WinForms support now requires all four independent postconditions in the regression. [Execution 05 report](EXECUTION_05_REPORT.md)

Execution 04 extends F05 / A07, A08 and S04 with explicit mutation outcomes, no automatic replay, identity-bound observations, and worker quarantine. `A2_MUTATION_LATE_EFFECT`, `A2_WORKER_QUARANTINE` and `A2_RECOVERY_UI` passed in the final private-desktop run. A09 and full A1 remain open. F06 and F07 are mapped in the B1 dependency audit. [Execution 04 report](EXECUTION_04_REPORT.md)

## Execution03 A2 contract evidence — 2026-11-05

Evidence **E03** = artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json; details [Execution03 report](EXECUTION_03_REPORT.md). A1 full physical acceptance BLOCKED; A2 controlled automated subset verified, full A2 NOT ACCEPTED. Existing F01–F19 identifiers and historical rows remain unchanged.

| Requirement / AC | Implementation | Executed tests / evidence | Status | Unsupported conditions / blockers | Acceptance decision |
|---|---|---|---|---|---|
| F05 / AC-F05 / A06 | Worker scoped Resolve; fixture physical geometry | E03 A2_PROVISIONAL_FOUR_PATTERNS point Confirmed; A2_EDGE_CONTRACTS duplicate/outside/missing/window recreation rejection | Controlled native subset PASS | Foreign/virtualized/irregular/large trees; other DPI; no global fallback | PROVISIONAL |
| F05 / AC-F05 / A07 | Worker Execute; WPF and standard fixtures | E03 FOUR_PATTERNS, EDGE_CONTRACTS, STANDARD_PROVIDER_COMPATIBILITY;4 independent postconditions,0/100/42.125,range rejection/read-only/ignored writes,toggle transitions,scroll boundary | Executed cases PASS | Every adverse provider state not exhausted; ExpandCollapse and other scroll directions unavailable | PROVISIONAL; not full A07 |
| F05 + F19 / A08 | Client process supervisor; adverse provider | E03 REAL_PROVIDER_RECOVERY, PROVISIONAL_WATCHDOG, IPC_FAULTS; real blocked query1826ms,heartbeat0.59ms; cancellation/exit/recovery | Controlled native containment PASS | No rollback guarantee for dispatched mutation; abrupt supervisor failure unverified | PROVISIONAL |
| F05 / A09 | Per-target fixture layer only | E03 foreground unchanged during fixture actions; no simultaneous external physical drag case | NOT RUN for canonical cross-app physical AC | Ordinary app allowlist and human/device drag/focus proof missing | BLOCKED |
| F05 + F19 / S04 | Contracts RequestLedger; Client; Worker capability identity |22 deterministic tests/config and E03 EDGE_CONTRACTS, IPC_FAULTS, REAL_PROVIDER_RECOVERY | Tested cases PASS | Actual OS PID/HWND reuse not forced; process lifetime check implemented | PROVISIONAL |
| F01/F02/F03/F19 / A1 subset | Existing app/overlay + runner profiles | Four E03 runs200 cycles7152 chars;100 clicks,100 starts/ends,20 held drags per run;stop/close/cleanup | Executable injected/native subset PASS | Full physical/shell/additional display gate open | Full A1 BLOCKED |


## Autonomous Execution02 traceability

F01/F02/F03/F19 executable injected/native subset now verified by isolated runner; full H/device/shell/display gate not accepted. F04 foundational logic remains deterministic-test verified; full dual Workspace later. F05 partial A2-PROVISIONAL implementation/native4-pattern fixture and watchdog-double results exist, but underlay Unsupported and real-app/provider compatibility pending. Nothing SHIPPED. See docs/AUTOMATED_ACCEPTANCE.md for per-case evidence classes and conditional authority.

## Execution 01.5 status

F01/F02/F03: builds/native lifecycle/20-cycle software observations exercised; human AC-A01/A02/A03/A04/A05/A10/A11 remains pending. F04 foundational independent cancellation now17-test regression coverage; full Workspace later. F19 sampled stop/restart/teardown passes. No SHIPPED claim. Current matrix docs/A1_PHYSICAL_ACCEPTANCE_RESULTS.md. F05/A2 handoff prepared only, not implemented.

## Implementation evidence — 2026-11-05

F01–F03 implemented at A1 scope and build/core/native-smoke verified; full AC remains blocked by physical checklist. F04 foundational pure claims unit-tested; full dual Workspace remains later. F19 replay stop/dispose/native-window cleanup implemented at A1 scope; worker recovery remains later. No feature SHIPPED. See TEST_RESULTS for exact evidence. Registry below is specification traceability, not current runtime certification.

All rows currently UNVERIFIED; source presence is not completion. Contract/AC source is [standalone directive](../codex/MASTER-IMPLEMENTATION-PROMPT.md) §15; UX source §§16–19, architecture §§7–13. Test definitions are [QA matrix](QA_MATRIX.md) and directive §22. No essential F row lacks a specification link. Proposed P rows intentionally have no implementation commitment.

| Requirement | Contract / acceptance | UX surface | Architecture owner | Task | Tests |
|---|---|---|---|---|---|
| F01 | §15 F01 / AC-F01 | tray/shell/exit | App lifecycle | M01 | A01,A10,S01 |
| F02 | §15 F02 / AC-F02 | passive overlay | Overlay/Win32/coordinates | M01 | A02,A03,A05,A10,A11 |
| F03 | §15 F03 / AC-F03 | developer simulator/panel | Core + timed replay | M01 | A04,A05,S02 |
| F04 | §15 F04 / AC-F04 | Workspace/claims | Core reducer | M04,M05 | B05,C09,S03 |
| F05 | §15 F05 / AC-F05 | experimental capability | Worker/Contracts | M02 | A06–A09,S04 |
| F06 | §15 F06 / AC-F06 | camera settings/indicator | Tracker supervisor | M03 | B01,B02,B08,B09,B12,S05 |
| F07 | §15 F07 / AC-F07 | calibration/practice | Core transforms/Tracker | M03 | B03,B07,A11 |
| F08 | §15 F08 / AC-F08 | Ghost arm/pinch/clutch | Core gesture FSM | M04 | B04–B07,B09,B12,S06 |
| F09 | §15 F09 / AC-F09 | edge/Interactive Peek | Core + Overlay | M04,M05 | B10,C10,C11 |
| F10 | §15 F10 / AC-F10 | selector/preview | Capture | M05 | C01–C04,S07 |
| F11 | §15 F11 / AC-F11 | save/recovery | Storage | M05 | C05,C06,C12,S08 |
| F12 | §15 F12 / AC-F12 | shelf/library/manage | App + Storage | M05 | C12,C14,S09 |
| F13 | §15 F13 / AC-F13 | viewer/pins/source | Core/Overlay/Storage | M05 | C07,C08,C13,C14 |
| F14 | §15 F14 / AC-F14 | dual Workspace | App/Core | M05 | C09,D05,S03 |
| F15 | §15 F15 / AC-F15 | shortcuts/normal controls | App hotkey service | M05 | C11,C15,D05 |
| F16 | §15 F16 / AC-F16 | onboarding/settings/privacy | App/Storage | M05 | B01,C12,C15,D05,S10 |
| F17 | §15 F17 / AC-F17 | diagnostics/native components | App/Overlay/instrumentation | M06 | D01–D06,D11 |
| F18 | §15 F18 / AC-F18 | install/uninstall | Packaging/runtime assets | M07 | D07,D08,D10,S11 |
| F19 | §15 F19 / AC-F19 | recover/error/fallback | Supervisors/writer/lifecycle | all gates | A08,B09,D09,S12 |

P01 search: PROPOSED, UX/library and Storage title index if accepted; needs its own accepted AC/tests before implementation. P02 quick slots/RAM/compare/magnetism: DEFERRED proposals, same rule. U01 exact wheels/models/licenses blocks release selection; U02 public branding does not block A1; U03 hardware/compatibility proof blocks feature readiness. I01 virtualization/cache is infrastructure inferred from bounded resource requirements, to validate with library-size performance fixtures.

Evidence location per executed row belongs in TEST_RESULTS with OS/app/build/display/integrity, command/steps, observed output, actual log path and status. Feature-state labeling remains independent of requirement classification and specification-review ratings.
