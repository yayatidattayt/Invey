# GHOST — Execution 07 engineering report

## Part A — Initial state

The nested project checkout is on `codex/a1-automation-a2-provisional` with an unborn HEAD and untracked source. There is no revision SHA. Existing files and ignored evidence were kept in place. The initial process inspection found no running Pocket app or acceptance process. Execution 06 supplied a working camera-free WPF lab, a pinned local MediaPipe worker, a strict observation pipe, and tested Stop/Restart. [Execution 06 report](EXECUTION_06_REPORT.md)

Final SHA-256s are `50c2d608032c2040461fad12a6ec01507d5934bf26c420bee69779daa0a18bd6` for `frame_pipeline.py` and `d6eed80b8df2fb18a387ca004a36b8df2118b8cc658de746ca818a99b9b6fa51` for `recorded_worker.py`. The native report records the application, runner and A2 binaries. Final native results (`artifacts/automated-acceptance/20261105T165845687-97103b0ee2674f8c9169b75edce63b76/results.json`, local only)

## Part B — M02 and Gate A

- `docs/QA_MATRIX.md:5` says “Gate A — Native feasibility (must pass before webcam).” The controlling directive also says “do not skip OS proof and move to webcam” at `codex/MASTER-IMPLEMENTATION-PROMPT.md:335`. `docs/DECISIONS.md:58` puts the webcam sidecar after Windows Gate A in ADR-05. Together with the reviewed milestone dependency, these put webcam adapter integration and webcam operation after the gate, including provisional builds and virtual-device trials.
- `codex/MASTER-IMPLEMENTATION-PROMPT.md:327–328` requires reviewed A1 for M02 and “M02 disposition” for M03. F05 at line 202 says “Core may proceed after A2 if external capability is explicitly deferred and hidden.” `codex/EXECUTION-PHASES.md:9–11` requires fixture and ordinary-app evidence for supported targeting and explicitly permits a formal deferred disposition.
- The M02 decision is whether to ship any tested ordinary-app UI Automation capability or formally defer and hide it. The engineering disposition is **defer and keep consumer UIA hidden**. The current build already has that boundary. It retains fixture evidence and exposes no consumer ordinary-app action. Formal M02 closure remains pending reviewed A1 and a recorded A2 review disposition.

| Dependency | Classification | Finding |
|---|---|---|
| A1 physical input and cursor provenance | Evidence blocker | A02–A05 physical trials remain open. Injected fixtures and native samples establish software behavior |
| A1 Explorer shell and additional display evidence | Environment / evidence blocker | Private desktop has no Explorer. A01/A10 physical shell and A11 other available scales remain open |
| Ordinary-app A09 and optional F05 scope | Product decision / evidence blocker | Controlled four-pattern and recovery evidence supports fixture scope. Ordinary-app focus and concurrent physical drag lack evidence |
| Live capture and OS error mapping | Technical work gated by A1 and M02 | No product device adapter or hardware activation path |
| Live validation | Authorization / environment blocker | Requires an eligible build and a specifically authorized virtual source or explicit physical opt-in. No authorized source was available for this execution |
| Distribution | Evidence and technical blockers | A1/B1 acceptance, clean-host install, complete license/notice and device qualification remain open |

Allowed now are the standalone frame contract, compiling and testing conversion and failure behavior, real controlled-source inference, WPF gating, and a metadata-only backend registry probe. Product camera adapter implementation and compilation into GHOST, virtual-device tests and user-triggered acquisition require reviewed Gate A and the M02 disposition. A later device test also needs explicit consent for its source. Neither the M02 recommendation nor a device opt-in substitutes for physical Gate A evidence.

**Decision for the A2 review.** Adopt “defer and hide ordinary-app UIA from the core v1 release” or keep the feature in scope and require scoped A09 evidence for each proposed supported target. Deferral keeps the fixture and its safety work, removes consumer claims, and allows core development after the foundation review. Keeping it in scope adds a real ordinary-app action with independent readback and separate physical focus and drag evidence. The recommended disposition is deferral. The exact review action is to record that choice as the M02 disposition after reviewing A1. An A09 execution under the other option requires its test target and benign operation to be explicitly scoped. [A1 evidence](A1_PHYSICAL_ACCEPTANCE_RESULTS.md) · [QA matrix](QA_MATRIX.md#gate-a--os-integration) · [Canonical dependency table](../codex/MASTER-IMPLEMENTATION-PROMPT.md#23-roadmap-and-stop-boundaries) · [A2 handoff](../codex/A2_HANDOFF.md)

## Part C — Architecture decision

Prefer a narrowly scoped Windows Media Foundation source for the first eligible live implementation. Enumeration can return named activation objects and video-device symbolic links. Source Reader supports negotiated output types and exposes native errors for deliberate categorization. This fits the one-worker ownership contract and allows the adapter to deliver a frame, an explicit failure, or end-of-source into the same VIDEO path. Device initialization must be behind Enable. The device and model must stay under the existing process and job lifetime. Exact interop binding and error translations need the eligible device spike. [Device enumeration](https://learn.microsoft.com/en-us/windows/win32/medfound/enumerating-video-capture-devices) · [Source Reader](https://learn.microsoft.com/en-us/windows/win32/medfound/processing-media-data-with-the-source-reader) · [Device loss](https://learn.microsoft.com/en-us/windows/win32/medfound/handling-video-device-loss)

The exact installed OpenCV 5.0.0 runtime reported its Media Foundation backend registered and available through registry calls. The probe made no VideoCapture instance. The public index-based API alone leaves device identity and precise cause reporting unverified. No new package or camera driver was installed. [OpenCV registry API](https://docs.opencv.org/4.x/de/db1/group__videoio__registry.html) · Local backend evidence (`artifacts/execution-07/frame-stages.json`, local only)

## Part D — Code implementation

| Module | Working change |
|---|---|
| `tests/tracking/frame_pipeline.py` | Pull-based source boundary and `RecordedFrameSource`. Validates uint8 three-channel frames and capture clocks, converts BGR when required, and owns the shared serial inference and emission path |
| `tests/tracking/recorded_worker.py` | Reuses the exact pinned model and worker containment, opens the source and feeds the shared pipeline. Guarded fixture can delay results after a real detection |
| `src/Pocket.Core/Tracking/TrackingSession.cs` | Distinct Recorded source and Stale result state. A heartbeat can keep a worker alive without extending a hand observation |
| `src/Pocket.App/Tracking/` | Recorded source labeling, live-camera unavailability explanation, and stale-result status in the existing lab |
| `tests/Pocket.Acceptance.Runner/RecordedTracking.cs` and guarded `AcceptanceHost.cs` commands | Detects stale result clearing and recovery in the same process and session. Tests Restart during teardown on actual controls |
| `tests/tracking/test_frame_pipeline.py` and `qualify_frame_pipeline.py` | Controlled frame inputs and cancellation, plus bounded measurement of the actual shared stages |

## Part E — Tracking lab

Open **Tracking lab** from GHOST. It identifies the recorded source and says why live camera is unavailable. There is no actionable live-camera control. **Enable recorded tracking** starts the local worker. The lab distinguishes detection, a fresh no-hand result, a result that has gone stale, an unavailable worker, and a stopped session. **Stop** clears the observation and waits for exit. **Restart recorded tracking** starts a new process and generation once cleanup is confirmed. The existing access keys and polite WPF live region carry the new status.

With the local pinned runtime and assets in place, start the built app using the [Execution 06 launch commands](EXECUTION_06_REPORT.md#d-actual-f06-interaction).

## Part F — Inference pipeline

The controlled source produces 640 × 480 frames from the pinned public still and blank samples. It requests an 80 ms cadence and never queues catch-up frames. The worker pulls one frame, checks capture ordering and format, converts to contiguous sRGB, then calls CPU `detect_for_video`. Synchronous VIDEO preserves ordered processing off the WPF thread. It shares the single-frame path with deterministic test inputs. Cancellation checked after inference prevents publishing an in-flight result. Missing input emits no result. Valid model output follows the existing normalized, 21-landmark contract over a per-session, PID-verified pipe. WPF retains one current hand. At 250 ms with no fresh observation, that hand clears independently of the worker health deadline. [MediaPipe VIDEO and callback behavior](https://developers.google.com/edge/api/mediapipe/python/mp/tasks/vision/HandLandmarker)

## Part G — Test matrix

| Test ID | Evidence category | Expected | Observed / verdict | Evidence |
|---|---|---|---|---|
| TrackingSessionTests | Deterministic | Stale, no-hand and fault remain distinct, reject reordered and old generations | PASS in both configurations | Final Debug and Release TRX |
| FramePipelineTests | Deterministic | Known BGR channel order, invalid shape and capture order rejected, cancellation emits nothing | Four PASS | Worker test log |
| Frame stage qualification | Recorded real inference | 120 ordered conversions, inferences and results, with both tracked and untracked output | 120/120 at all stages, 68 tracked, PASS | Frame stages JSON |
| F06_RECORDED_WPF_LIFECYCLE | Native UI + recorded inference | Real Enable, loss/return, Stop, new generation, window close | Observed all states, distinct PID/session and teardown, PASS | Final native results |
| F06_RECORDED_STALE_RESULT | Native UI + controlled failure | Delayed result clears the hand with distinct feedback and fresh results resume same worker | Cleared at 13 accepted observations, resumed at 14, same PID and session, PASS | Final native results |
| F06_RECORDED_WORKER_FAULTS | Native UI + controlled failure | Crash, malformed IPC, invalid model, timeout, cancellation and blocked Restart while old PID lives | All fault categories matched, Restart held and forced exit observed, PASS | Final native results |
| EXIT_CLEANUP | Native software | Active worker and owned windows exit with host | Worker and windows gone, PASS | Final native results |
| B01/B02/B08/B09/B12 actual device | Authorized integration | Explicit open, observe, handle errors and release hardware | BLOCKED, no device session | Parts B and H |

## Part H — Camera evidence

Deterministic array fixtures and failure injection are software evidence. The attributed-still sequence exercises real local inference and actual WPF with **zero camera frames**. Native software verification covers processes, transport and UI. Virtual camera and physical webcam cases were not run. No video was recorded, no camera configuration was changed, and no imagery was uploaded.

## Part I — Performance

Windows 11 x64 build 26200 and the pinned Python 3.12.10 / MediaPipe Tasks 0.10.32 CPU runtime. The separate shared-stage probe ran for **10.95 s**, acquired, submitted, processed and emitted **120** frames to an in-process test sink, and reported **10.96 frames/s**. Source setup took **20.32 ms** and model initialization took **136.58 ms**. Median/p95 were **0.23/0.44 ms** for frame validation and MediaPipe image construction, **16.80/27.93 ms** for inference, and **17.80/29.52 ms** from generated-frame acquisition to result. Zero application drops occurred and there is no pending frame queue. The sink measures emitted results from VIDEO rather than asynchronous callbacks. Stage measurements (`artifacts/execution-07/frame-stages.json`, local only)

The full private-desktop Release run used .NET 10.0.14 on an Intel i5-13450HX at 1920 × 1200 and 125%. The app showed a first hand in **1.96 s**. It accepted another **92 observations in 7.61 s** after the initial sequence, or **12.09/s**, and sampled 59 distinct displayed hands. The sampled source-to-result median was **10 ms** and the maximum sampled age since WPF host receipt was **91.19 ms**. Stop to confirmed exit took **132.84 ms** and Restart to the first hand took **1.92 s**. The worker used **0.83 s CPU time** in the 7.61 s window, about **10.9% of one logical core**. Working set was **162.87 MiB → 162.93 MiB** across two samples. These measures are from the WPF run with its own conditions and sample set. The test confirmed responsive keyboard and dispatcher state changes. It did not instrument compositor timing. Native measurements (`artifacts/automated-acceptance/20261105T165845687-97103b0ee2674f8c9169b75edce63b76/results.json`, local only)

These are short controlled-source runs. Device queueing, actual camera frame rate or drops, sensor-to-display latency and long-run memory stability remain unmeasured. The canonical camera-to-visible median/p95 targets of 120/220 ms require device evidence. [Performance targets](SPECIFICATION_REVIEW.md)

## Part J — Failure recovery

Missing or corrupt model, startup timeout, crash, malformed pipe records and unresponsive termination use the existing categorized failure and quarantine path. The new freshness state clears a stale hand while the worker remains alive. It accepts a fresh observation from the same active session and keeps rejecting old generations and reordered observations. Invalid input frames fail before inference. Stop prevents publication and closes the pipe. Termination by job remains independent of blocked inference. Camera denial, device busy and device loss are core fault codes with deterministic state coverage. Device-specific Windows error mapping and resource release remain untested and unimplemented.

## Part K — Test quality

The runner drives actual WPF access keys and examines rendered status and current state. It also queries the OS for process exit, compares distinct worker PIDs and session IDs, and observes window removal. The stale-gap test requires a live worker and absence of a current hand, then recovery in that same process. The frame-stage probe checks sequence and timestamp order and counts source acquisition, model calls and emitted records separately. Hardware claims are kept outside these verdicts.

In a disposable copy, removing the monotonic-frame guard let a backward frame through and made the ordering test fail with “ValueError not raised.” Restoring the original file yielded four passing tests and a matching source hash. The failure log is preserved. Fault-injected run (`artifacts/execution-07/negative-control-failure.log`, local only) · Restored run (`artifacts/execution-07/negative-control-restored.log`, local only)

## Part L — Regression

Locked restore and both builds passed with zero warnings/errors. Debug and Release each passed **47** core tests. All **eight** worker tests passed. Debug TRX (`artifacts/execution-07/final-Debug.trx`, local only) · Release TRX (`artifacts/execution-07/final-Release.trx`, local only) · Worker tests (`artifacts/execution-07/worker-tests.log`, local only)

The final short regression with seed 73021 reported **24 PASS, 0 FAIL, 1 BLOCKED, 1 INCONCLUSIVE**. A1 click-through, typing, held drag, idle/active cursor, lifecycle and current-DPI software checks passed. A2 targeting, transport, fixture postconditions, mutation uncertainty and late-result containment passed. The Explorer check remains blocked. The closed-app cursor control observed movement before GHOST started, with the injector inactive. It remains inconclusive. Three native recorded cases and active-worker exit passed. A separate audit confirmed the Default desktop and zero scoped app, runner or worker processes. No normal-desktop input was injected. Final native results (`artifacts/automated-acceptance/20261105T165845687-97103b0ee2674f8c9169b75edce63b76/results.json`, local only) · Cleanup audit (`artifacts/execution-07/final-cleanup.json`, local only)

## Part M — Product progress

| Capability | Current user workflow | Open scope |
|---|---|---|
| POCKET | See the passive panel and replay a controlled reference action | Capture, persist, retrieve and organize a user's screen excerpt |
| DUAL | Run independent app-managed simulation while ordinary Windows input remains available. Four UIA patterns and recovery work in controlled fixtures | Physical multi-input acceptance and any ordinary-app scope |
| GHOST | Run, observe, stop and restart real recorded-source inference inside GHOST | Live frames, calibration and gestures |
| Integration | Worker → pipe → session → actual WPF controls. App exit and lab close own teardown | No gesture-to-POCKET workflow or external Windows action from hand observations |
| Privacy/release | Local recorded processing starts only on Enable. Camera off. Existing app remains usable through conventional controls | Live opt-in/device proof, clean-host install, exact redistribution inventory and acceptance |

## Part N — Gate status

| Gate | State |
|---|---|
| A1 / Gate A | BLOCKED for physical and Explorer-shell evidence. Record available display coverage |
| A2 | PROVISIONAL controlled fixture and reliability evidence |
| M02 | Engineering disposition proposed as defer/hidden. Formal A2 review follows reviewed A1. A09 remains unrun |
| F06 / B1 | Recorded observation lifecycle verified. Live adapter and device acceptance are open |

## Part O — Remaining risks

1. **P0 gate dependency.** Supply physical input and shell evidence using the existing A1 protocol. Record the A2 disposition at review.
2. **P0 device privacy and ownership.** On an eligible build, verify opt-in, actual denial/busy/loss, bounded teardown and reacquisition of the same authorized source.
3. **P1 device performance and distribution.** Measure the complete capture-to-visible path and sustained CPU/memory. Verify clean-host dependencies and exact asset redistribution.

## Part P — Next execution

The next implementation slice remains **M03/B1 F06 device selection, explicit Enable, Observe, Stop and Restart**. F07 calibration follows the usable F06 device path. Proposed instruction:

> Review and record the missing A1 physical/shell evidence and the M02 optional-UIA disposition. Once eligible, connect one opt-in Windows Media Foundation source to the existing VIDEO worker and test release and reacquisition on a specifically authorized device. Stop at F06 review.
