# GHOST — Execution 06 engineering report

## A. Executive summary

The existing GHOST desktop app now has a working **Tracking lab**. An explicit start runs real local MediaPipe inference on a controlled frame sequence. Validated observations reach WPF over a named pipe. The UI shows detection and loss, Stop clears the current hand, and Restart waits for the previous worker to exit. This proves the observation lifecycle in the application. F06 live camera implementation and B1 acceptance remain open.

The QA matrix says **“Gate A — Native feasibility (must pass before webcam)”**. ADR-05 specifies **“Python MediaPipe sidecar only after Windows Gate A”** for the webcam path. The M03 dependency table requires M02 disposition. Execution 06 used the directive's independent-work allowance for a camera-free laboratory path. Device adapter integration and acquisition are gated. [QA matrix](QA_MATRIX.md) · [ADR-05](DECISIONS.md) · [M03 dependencies](../codex/MASTER-IMPLEMENTATION-PROMPT.md)

## B. Source and branch state

The working branch is `codex/a1-automation-a2-provisional` with an unborn HEAD. There is no commit SHA or baseline from which to create a conventional isolated branch. The existing uncommitted checkout was preserved. No commit, merge, or release was made.

| Area | Execution 06 files |
|---|---|
| State | `src/Pocket.Core/Tracking/TrackingSession.cs` adds failure and deadline transitions |
| WPF and supervision | `src/Pocket.App/Tracking/RecordedTrackingWindow.cs`, `RecordedTrackingController.cs`, and `RecordedWorkerJob.cs`. Existing app, main window and acceptance host integrate the lab and shutdown |
| Source and runtime | `tests/tracking/recorded_worker.py`, with `test_recorded_worker.py` |
| Native evidence | `tests/Pocket.Acceptance.Runner/RecordedTracking.cs`, runner entry point and `scripts/Run-AutomatedAcceptance.ps1` |

Final worker source SHA-256 is `53b7a7505fbea1d776b6a94d22b3c3e3c60310405e12b91550637c5742f77a1c`. The native results record the exact application, runner, and A2 binaries. The runtime and fixtures remain local, ignored development assets from Execution 05. [Runtime and model inventory](EXECUTION_05_REPORT.md)

## C. Camera acquisition architecture

Device acquisition is gated. The tested source constructs 640 × 480 sRGB frames from a hash-checked, attributed still and generated blank frames. The worker owns the current image, the MediaPipe model and the frame loop. It requests an 80 ms cadence and performs one synchronous VIDEO inference per frame. There is no application frame queue. Only bounded, newline-delimited observation and lifecycle records cross the pipe. The host holds one current observation and renders on an 80 ms WPF timer. This path keeps model work off the dispatcher and exercises the existing transport and session ownership before selecting a device backend.

The app creates a per-session, current-user named pipe and validates the connecting PID. The worker waits for a start message until Windows assigns it to a kill-on-close job. The parent closes the control and observation pipes on Stop and waits for exit. The job bounds failures inside native inference. An unconfirmed worker retains ownership and holds Restart. WPF does not treat the worker's Stopped message as proof of exit. [Windows job objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) · [Named-pipe control](https://learn.microsoft.com/en-us/windows/win32/api/namedpipeapi/nf-namedpipeapi-peeknamedpipe)

## D. Actual F06 interaction

Open **Tracking lab** in the existing GHOST controller. The lab says **Recorded source · Camera off**. Opening it leaves tracking off. **Enable recorded tracking** starts the worker. The status reports startup, detection with 21 landmarks, and absence with the previous observation cleared. **Stop** clears the current hand as it begins cleanup and shows Stopped after exit. **Restart recorded tracking** starts a new worker and session. Closing the lab, using the tray Stop command, and the application's Exit route request tracking teardown.

For a local checkout with the Execution 05 runtime and assets available, build Release and launch the app from the project root:

```powershell
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('run','--project','src/Pocket.App/Pocket.App.csproj','-c','Release','--no-build')
```

The lab expects `.tools/exec05-inference` and `.tools/exec05-model` in that checkout. Absent assets produce an unavailable state. This is a development setup.

## E. MediaPipe integration

Python 3.12.10 loads MediaPipe Tasks 0.10.32 in CPU VIDEO mode with a one-hand limit. The worker verifies the float16/1 model and fixture bytes using the Execution 05 qualification hashes. It converts in-memory sRGB pixels to a MediaPipe image and calls `detect_for_video` with strictly increasing monotonic millisecond timestamps. Exactly 21 finite landmarks with in-frame x/y become a tracked sample. Zero, ambiguous, malformed or out-of-frame results become loss. The adapter never clamps bad output. Valid optional handedness and score are retained. Model and runtime failures produce categorized lifecycle messages. The worker closes the model during normal teardown. The process boundary covers abnormal teardown. Frames are processed locally and are neither transmitted nor saved. [Google Hand Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker?tab=Python) · [Fixture and asset provenance](EXECUTION_05_REPORT.md)

## F. Test matrix

| Requirement | Executed test and expected result | Observed result | Evidence class | Verdict |
|---|---|---|---|---|
| Opt-in and actual controls | Open the lab, press Alt+E, Alt+T and Alt+R, close with Alt+F4 | Off and no worker at open. Enable, Stop, Restart and Close acted on actual WPF controls | UI_INTEGRATION | PASS |
| Real model sequence | Blank, hand, movement, absence, return | 21 landmarks, measurable wrist displacement, cleared loss, return in the same session | RECORDED_SOURCE_REAL_INFERENCE + UI_INTEGRATION | PASS |
| Generation and transport | Reject stale, malformed, oversized or invalid messages. Fresh worker after confirmed exit | Core tests passed. Native malformed wire message faulted. Restart used new PID and session | DETERMINISTIC_PIPELINE + UI_INTEGRATION | PASS |
| Failure and cancellation | Crash, invalid model, startup timeout, cancellation, unresponsive worker | Correct failure states, stopped startup, forced termination and exit observed | CONTROLLED_FAILURE_INJECTION + UI_INTEGRATION | PASS |
| Window and app shutdown | Close the lab and then exit a separate active-tracking session | Owned process and windows vanished | NATIVE_SOFTWARE_TEST + UI_INTEGRATION | PASS |
| Bounds and negative control | Reject nonfinite or out-of-frame points. Remove validation in a disposable copy | Four normalization tests pass. Disabling validation fails two subcases. Restoring the validation passes | DETERMINISTIC_PIPELINE | PASS |
| WPF under load | Continue the sequence across repeated cycles | Accepted at least 92 additional observations in one session with live WPF polling | RECORDED_SOURCE_REAL_INFERENCE + UI_INTEGRATION | PASS, short run |
| Live Windows capture | Open an approved device, track, stop, confirm release and reacquire | No capture adapter or device session exercised | NATIVE_CAMERA_INTEGRATION / ENVIRONMENT_BLOCKED | BLOCKED |
| Physical-device validation | Opted-in user performs the full webcam journey | Zero camera frames acquired | AUTHORIZED_PHYSICAL_DEVICE | NOT RUN |

## G. Real inference evidence

The integrated test observed blank → hand → translated wrist → cleared loss → return. It drove the actual MediaPipe model with an in-memory sequence assembled from the public photograph “Human Palm.jpg” by ESEMES and generated blanks. The motion is a translation of the still. The model produced the measured landmark positions for each frame. This evidence covers observation delivery and clearing under a continuous controlled sequence. It does not establish performance on articulated motion or device video. [Public fixture](https://commons.wikimedia.org/wiki/File:Human_Palm.jpg) · [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)

## H. Live camera evidence

**BLOCKED.** Gate A and M02 disposition are outstanding. No authorized virtual source was identified for this execution and no physical device test was authorized. No camera was opened. Real device permissions, absent/busy device detection, supported formats, unplug/replug and confirmed device release require an eligible implementation and a separately authorized test source.

## I. Performance

Windows 11 x64 build 26200, .NET 10.0.14, Release, Intel Core i5-13450HX. The final short private-desktop run used seed 73021 and sampled 63 distinct displayed hand observations. Final run (`artifacts/automated-acceptance/20261105T162333916-e5faee384bda46c081ce462be083b822/results.json`, local only)

| Measured interval or resource | Observed |
|---|---:|
| Enable to confirmed blank status / first hand | 1.38 s / 2.05 s |
| Enable through blank, motion, loss and return | 4.39 s |
| Wrist x displacement | 0.0159 normalized units |
| Sustained accepted observations | 92 in 8.57 s, 10.73/s |
| Source-to-result median over sampled hands | 10 ms |
| Maximum sampled host observation age | 91.35 ms |
| Worker CPU time over sustained window | 1.45 s |
| Worker working set at start / end of sustained window | 170.26 MiB / 170.32 MiB |
| Stop to confirmed exit and cleared state | 115 ms |
| Restart to first new hand | 1.94 s |

The source-to-result interval includes frame construction, MediaPipe image conversion, inference and normalization. Host age is measured since receipt at WPF sampling. The intervals have separate clock domains. Stage-level conversion, standalone inference and WPF compositor timing were not instrumented here. A serial worker and single latest observation keep the application backlog bounded. Camera rate, camera drops and sensor-to-display latency have no evidence. Two working-set samples and the short sustained profile do not establish memory stability or a throughput guarantee. Execution 05's still-image model-call baseline was 20.43 ms median and 23.87 ms p95. [Prior benchmark](EXECUTION_05_REPORT.md)

## J. Reliability

Stop during startup, unexpected exit, corrupt model, invalid IPC and a worker that ignored Stop all have native test evidence. Startup timeout is 5 s. Health timeout is 3 s. Teardown uses a 2 s graceful wait followed by job termination and a bounded exit confirmation. Delayed or wrong-session records cannot revive a stopped observation. Restart stays held if exit is unconfirmed. The core tests cover device-shaped status codes and timeout transitions using controlled inputs. Windows device error translation, disconnect and reacquisition remain untested.

The initial integrated run and first targeted run timed out starting real inference. Replacing the concurrent blocking stdin reader with a nonblocking parent-pipe check between frames allowed the worker to initialize and complete the lifecycle. The run also corrected the short interval clock to use the high-resolution performance counter after the first successful run reported a zero-millisecond median. Original evidence was retained. Initial full run (`artifacts/automated-acceptance/20261105T161350699-97730a07b65142499ebbea0910a9de9a/results.json`, local only) · Initial targeted run (`artifacts/automated-acceptance/20261105T161814199-7f53637135d34f6a9f89dbb4e0042af0/results.json`, local only) · Targeted passing run before timing correction (`artifacts/automated-acceptance/20261105T162007544-9dc11c1bc2c34c9ba0d1f69da745cd3c/results.json`, local only) · Negative control failure (`artifacts/execution-06/negative-control-failure.log`, local only) · Restored control (`artifacts/execution-06/negative-control-restored.log`, local only)

## K. Regression

Locked restore and Debug and Release builds passed with zero build warnings or errors. The final unit test runs had 47 passes per configuration. The Python worker normalization suite had four passes. Debug tests (`artifacts/execution-06/final-Debug.trx`, local only) · Release tests (`artifacts/execution-06/final-Release.trx`, local only) · Worker tests (`artifacts/execution-06/worker-tests.log`, local only)

The final private-desktop run recorded **23 PASS, 0 FAIL, 1 BLOCKED and 1 INCONCLUSIVE** across A1/A2 and recorded tracking. A2 fixture targeting, four WinForms postconditions, mutation uncertainty, worker recovery and malformed IPC passed. The overlay lifecycle, idle and active-simulation cursor checks, held drag and exit passed. The Explorer shell case was blocked on the private desktop. The inconclusive cursor baseline saw movement before GHOST launched while the injector was inactive. The harness verified exit while recorded tracking was active, including removal of its worker and window. A separate audit found the Default desktop restored and zero remaining scoped test processes. Full A1 physical acceptance stays blocked and A2 stays provisional. Final native regression (`artifacts/automated-acceptance/20261105T162333916-e5faee384bda46c081ce462be083b822/results.json`, local only) · Cleanup audit (`artifacts/execution-06/final-cleanup.json`, local only)

Reproduce the targeted or full native run from the project root on the qualified Windows host. The runner creates a private desktop and restores the original one.

```powershell
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021 -TrackingOnly
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
```

## L. UI and accessibility

The lab uses a compact native WPF window, system controls, a persistent source label and textual state feedback. It has keyboard access keys and a polite live region that changes on status transitions. Start is disabled while a worker is active or cleanup is unconfirmed. The private-desktop test verified the actual WPF keyboard actions, rendered text and disabled Start. A dedicated assistive-technology, theme and multi-DPI review remains pending. No motion effects were added.

## M. Product completeness

| Area | Current behavior | Remaining scope |
|---|---|---|
| POCKET | Passive panel and an internal simulated reference action | User capture, cropped storage, library and recovery are later milestones |
| DUAL | App-managed simulated channel, controlled focus/cursor tests and isolated WPF/WinForms UIA fixture evidence | Physical simultaneous drag/focus, Explorer and ordinary-app disposition |
| GHOST | Explicitly started real inference from controlled frames, validated state and visible lifecycle in the app | Live Windows capture, calibration, device lifecycle and authorized-device evidence |
| Integration | Worker → validated named pipe → session → WPF. Window and app teardown control the worker | Hand observation remains isolated from Windows input and POCKET manipulation. The complete camera journey is unavailable |
| Release | Local development checkout and exact-environment evidence | Accepted native gates, safe packaging, redistribution and asset notices, device and accessibility qualifications |

## N. Remaining blockers

1. Close A1 physical-input and Explorer shell checks and the outstanding display coverage. Record M02/A09 ordinary-app evidence or its formal supported/deferred disposition.
2. Once eligible, integrate one Windows camera source with explicit opt-in, typed acquisition errors and confirmed release. Run authorized device startup, loss, failure and reacquisition cases.
3. Measure sustained device throughput, latency, drops and resource use. Review packaging, asset notices and accessibility before distribution.

## O. Next milestone

The next eligible implementation remains **M03 / B1 — finish F06 live camera acquisition and lifecycle** after Gate A and M02 disposition. F07 calibration follows the usable F06 observation path within B1. B2 gesture work is outside this execution. [Canonical phases](../codex/EXECUTION-PHASES.md)
