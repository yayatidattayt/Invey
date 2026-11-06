# GHOST Execution 05 engineering report

## Part A — Executive engineering summary

Implemented the runtime-neutral hand observation pipeline in Pocket.Core and qualified a real Hand Landmarker model with local still inputs. The pipeline can start a simulated session, accept and expose a validated observation, clear it on loss or Stop, and admit a new session only after cleanup is acknowledged or the worker exit is observed. These results advance the next tracker boundary. Camera integration and the WPF lifecycle remain gated by QA Matrix Gate A, ADR-05 and the pending M02 disposition. [Canonical QA gates](QA_MATRIX.md) · [Milestone handoff](../codex/HANDOFF.md)

The existing checkout is on the unborn codex/a1-automation-a2-provisional branch. A2 implementation files were preserved. New core tracking files and tests are separate from the A2 worker. A separate Git worktree could not be based on this unborn HEAD. The deliberate defect tests used an ignored scratch copy of only the relevant projects.

## Part B — Technology decisions

| Candidate | Evaluation | Disposition |
|---|---|---|
| MediaPipe Tasks CPU in supervised Python process | Publisher supplies Windows x64 wheels and a hand-specific detector and landmarker. The exact qualification environment ran locally on Python 3.12.10 with MediaPipe 0.10.32 and the version 1 float16 bundle. The downloaded wheel set was 98,195,171 bytes before unpacking. Python and native package distribution remain material costs. | Retain as the provisional M03 runtime candidate. Test camera acquisition separately in the worker after Gate A. |
| ONNX Runtime C# CPU | .NET integration can reduce Python packaging overhead. An appropriate licensed hand model, palm detection, ROI projection and tracking pipeline would still need qualification. This execution measured no equivalent ONNX model. | Revisit if measured deployment or runtime costs justify the additional work. |
| Native camera plus in-process inference | Media Foundation offers Windows capture. In-process integration would need a native model adapter, camera lifecycle work and protection against native faults inside WPF. | Candidate camera backend for a future worker. Exact backend and error translation need authorized device tests. |

The model comes from Google's versioned Hand Landmarker float16/1 asset, 7,819,105 bytes, SHA-256 fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1. The published Hand Tracking model card identifies Apache-2.0 for the documented model family. The runtime wheel SHA-256 438d98977a4e1b0f88070ad16537a328912f847ce9debf236da047b0b483cbfd matched PyPI. The exact local dependency hashes are recorded in the wheel manifest. Local pip check reported no broken requirements. Wheel manifest (`artifacts/execution-05/wheel-manifest.json`, local only) Shipping requires an exact asset and native dependency notice review. This execution installed an ignored, test-only environment and distributed no runtime or model. [Google model guide](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker?tab=Python) · [Versioned model](https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task) · [Google model card](https://storage.googleapis.com/mediapipe-assets/Model%20Card%20Hand%20Tracking%20%28Lite_Full%29%20with%20Fairness%20Oct%202021.pdf) · [PyPI wheel](https://pypi.org/project/mediapipe/0.10.32/) · [ONNX Runtime C#](https://onnxruntime.ai/docs/get-started/with-csharp.html) · [Windows video capture](https://learn.microsoft.com/en-us/windows/win32/medfound/video-capture)

## Part C — Implementation details

- src/Pocket.Core/Tracking/TrackingProtocol.cs implements version 1, strict UTF-8, a 16 KiB byte limit, exact allowed fields and typed lifecycle messages. It accepts either exactly 21 finite normalized image landmarks or a loss report with zero landmarks. The only quality score carried is the optional handedness classification score. The schema rejects pixel payloads, duplicates, malformed lengths and non-finite values.
- src/Pocket.Core/Tracking/TrackingSession.cs owns a unique session ID, the current state, ordered sequence and source timestamps, one latest observation and monotonic deadlines. It clears stale observations, rejects old generations and retains Stopping when teardown is unresolved. An independent caller must observe exit or obtain a matching Stopped reply. The core has no camera or process ownership.
- tests/Pocket.Core.Tests/TrackingSessionTests.cs and TrackingProtocolTests.cs exercise the state and actual local-pipe framing with a synthetic producer. tests/tracking/qualify_hand_landmarker.py runs local real-model qualification without a device or product adapter.
- The A2 standard WinForms case now requires all four known fixture capabilities and independent postconditions. Missing support previously could be recorded inside an overall passing case. The consumer A2 action path was unchanged.

Capture-to-result age uses only the source clock. Host age starts on receipt and uses only the host clock. The two ages are distinct. The 250 ms limits are conservative observation policy, with 5 s startup, 3 s health and 2 s cleanup reporting deadlines. Timeouts are evaluated on ingest or snapshot reads. A future supervisor must schedule those reads during idle periods.

## Part D — Actual user experience

The normal WPF app still opens the existing camera-off native input controller. There are no new camera buttons in the app. In tests, Enable creates a fresh Starting session, Ready permits input, Observe exposes one validated hand, Stop immediately clears it, and a cleanup acknowledgement permits a deliberate Restart. These are exercised core operations awaiting product camera and WPF wiring.

## Part E — Test quality audit

| Evidence class | What it independently establishes | Limits |
|---|---|---|
| Strong independent checks | Expected typing is generated by the test and compared to the actual editor digest. A2 uses provider-side counters and commit markers. Window/process cleanup uses HWND and process observations. | Windows routing on a controlled desktop has different scope from physical or Explorer testing. |
| Controlled integration | WPF/WinForms UIA, late provider effects and an actual local named pipe exercise real subsystem boundaries. The runtime independently inferred a hand from a public photograph. | No product camera producer or cross-process model adapter was exercised. |
| Mock-dependent | Tracker faults and time are driven by fake events and a virtual clock. A2 termination failure and late IPC reply use a fault double. | No proof of camera permission mapping, actual unplug or native kernel termination failure. |
| Environment-limited | Private desktop isolates test input. Explorer shell remains blocked and display evidence is scoped to the tested configuration. | No substituted hardware or shell result. |
| Missing | Physical Gate A, A09 ordinary-app focus and drag, authorized camera lifecycle, sustained video, WPF camera accessibility and full POCKET workflow. | Gates remain open. |

Two deliberate defects were introduced only in a scratch copy. Disabling DUAL Release left target “a” held and failed the release test. Retaining the hand on Stop failed the tracking test. Both failed at the externally visible assertions, then both passed with restored source. Restored scratch source hashes matched the project copies. Negative-control TRX (`artifacts/execution-05/negative-control.trx`, local only) · Restoration TRX (`artifacts/execution-05/negative-restored.trx`, local only)

## Part F — Test results

| Requirement | Method and observable expectation | Observed and verdict |
|---|---|---|
| F06 / S05 schema and stale state | TrackingProtocolTests and TrackingSessionTests. Decode complete fragmented messages, reject pixels, invalid points and stale epochs. | PASS in both configurations. Includes real same-user named pipe with synthetic messages. |
| F06 / B08, B09, B12 state policy | TrackingSessionTests. Fault and Stop clear the hand, suppress late frames and require teardown before restart. | DETERMINISTIC_SIMULATION PASS. Physical resource and device failures remain NOT RUN. |
| F06 runtime qualification | qualify_hand_landmarker.py. One hand with 21 in-frame points, zero on blank, model close/reopen, and corrupt model rejection. | REAL_MODEL_INFERENCE PASS. Raw edge fixture produced 10 out-of-frame points and remains invalid for the wire contract. |
| F01–F05 regression | Existing short isolated acceptance runner plus Core tests and strict WinForms capability assertions. | 21 passed, one Explorer case blocked, one closed-app cursor control inconclusive, zero failed. |
| F07 calibration | Requires F06 camera, consented preview and display alignment. | NOT_RUN. |

Commands executed from the project root:

~~~powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Debug','--no-build','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Release','--no-build','--no-restore')
& .\.tools\exec05-inference\Scripts\python.exe tests/tracking/qualify_hand_landmarker.py --model .tools/exec05-model/hand_landmarker.task --palm .tools/exec05-model/public-palm.jpg
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
~~~

Both final builds had zero warnings and errors. Each configuration passed 47 tests, 25 of them new. An initial test build rejected a malformed C# raw string. An initial test run timed out because the producer awaited its write before the pipe reader started. Starting the read and coordinating subsequent writes fixed the harness. The two earlier model fixture fits were invalid at the image boundary. The final run retained the original edge case and used a separately padded positive fixture. First fixture failure (`artifacts/execution-05/inference-first-failure.json`, local only) · Second fixture failure (`artifacts/execution-05/inference-inset-failure.json`, local only) Initial test failure (`artifacts/execution-05/core-debug.trx`, local only) · Debug (`artifacts/execution-05/final-Debug.trx`, local only) · Release (`artifacts/execution-05/final-Release.trx`, local only) · Model qualification (`artifacts/execution-05/inference-verified.json`, local only)

## Part G — Camera and tracking evidence

- DETERMINISTIC_SIMULATION passed session and failure tests and a synthetic producer over a Windows named pipe.
- REAL_MODEL_INFERENCE detected one hand with 21 points from the padded public still and zero hands on blank input. Fifty repeated still samples used increasing VIDEO timestamps. Returning to blank cleared the result. No landmarks or raw frames were written by the qualification script. Source still “Human Palm.jpg” by ESEMES, CC BY 3.0. The harness resized and padded it in memory. [Image provenance](https://commons.wikimedia.org/wiki/File:Human_Palm.jpg) · [License](https://creativecommons.org/licenses/by/3.0/)
- NATIVE_CAMERA_INTEGRATION is ENVIRONMENT_BLOCKED by Gate A. AUTHORIZED_DEVICE_TEST is NOT_RUN. No camera was opened.

## Part H — Performance and resource behavior

The Intel Windows 11 build 26200 test host ran the model with CPU delegation. Model initialization was 44.49 ms, first IMAGE inference 24.40 ms, close 8.91 ms, and VIDEO reinitialization 41.13 ms. Fifty held-image VIDEO calls had 20.43 ms median and 23.87 ms p95. VIDEO model close was 9.13 ms. These measurements cover the model call and cleanup only. The Release synthetic burst retained one of 10,000 inputs, superseded 9,999 unread observations, and took 21.65 ms including test overhead. Device startup, frame rate, camera frame drops, camera-to-display latency and sustained native memory remain unmeasured.

## Part I — Failure conditions

Synthetic tests map missing, denied, busy and disconnected devices, unsupported format, invalid model and runtime failure to unavailable after cleanup. Startup/heartbeat timeout invalidates state and requires teardown. Unexpected process exit and protocol error have distinct signals. Invalid records and late callbacks are rejected. The actual corrupt model was rejected by MediaPipe. Hardware-specific diagnosis and device reconnect require the later authorized camera adapter.

## Part J — Regression results

The short profile matched all 744 characters across 20 replay cycles and passed click-through, held-drag, overlay teardown, A2 mutation/IPC recovery and all four WinForms postconditions. The cursor was unchanged in both idle and active app samples. The separate closed-app control detected movement before GHOST launched and remains inconclusive. Explorer Alt+Tab/tray remained blocked on the private desktop. The harness exited 0. An independent audit observed Default restored and no scoped app, worker, runner or inference process remaining. Ordinary shell and single-instance behavior were outside this private-desktop run. Native results (`artifacts/automated-acceptance/20261105T101056505-0f438a56911d4afaa59f0f4467003b7e/results.json`, local only) · Cleanup (`artifacts/execution-05/cleanup.json`, local only)

## Part K — User interface

The existing Quiet Instrument direction and camera-off controls remain the product surface. A future camera control needs truthful Ready/Observing/Lost and Stopping/Unavailable language, explicit enable and restart, a privacy indicator and native keyboard navigation. The current acceptance run supplies no screen-reader evidence for this future view.

## Part L — Product completeness

POCKET has a simulated panel that can move under GHOST in the test overlay. Real capture, persistence and reference retrieval remain future work. DUAL has independent logical claims, deterministic release and controlled click-through, focus and drag evidence. GHOST has timed replay, a verified observation pipeline and separately verified still-image model inference. The available workflow is a controlled interaction laboratory. The first useful integrated workflow still requires live F06, F07 calibration, B2 ownership and intentional gestures, then Gate C capture, save and recall.

## Part M — Known limitations

1. Gate A physical input and shell coverage plus the M02 ordinary-app disposition remain the first dependencies for the webcam sidecar.
2. Device access, capture formats, startup/cancel/exit, UI responsiveness under camera load and privacy indicator truthfulness remain unverified.
3. Edge model outputs can exceed the image bounds. The protocol rejects them. Transport age before host receipt remains unmeasured and can be addressed by the future supervisor. Broad accuracy, hands under occlusion, varied lighting and performance under real camera load remain unmeasured.
4. The offline dependency inventory is not a release redistribution audit. The candidate's native package size and asset notices still matter.

## Part N — Milestone readiness

| Level | Status |
|---|---|
| Implemented | Runtime-neutral F06 observation foundations |
| Deterministically verified | Yes for the state and validation contracts |
| Integration verified | Local pipe with synthetic input, plus separate real-model qualification |
| Native camera verified | ENVIRONMENT_BLOCKED |
| Authorized camera device verified | NOT_RUN |
| Fully accepted | Gate A and the B1 product lifecycle remain open |

## Part O — Next engineering execution

> Resolve the Gate A physical and Explorer checks and record the M02 ordinary-app disposition. Then implement the first F06 slice with the pinned local Hand Landmarker candidate and a supervised, opt-in camera adapter. Verify enable, first observation, stop and restart on an explicitly authorized camera before beginning calibration or gestures.
