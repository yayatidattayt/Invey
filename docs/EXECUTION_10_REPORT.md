# Invey — Execution 10 engineering report

## A — Initial state

The existing checkout is on unborn branch `codex/a1-automation-a2-provisional`. GHOST was the former product name. Pocket, Dual and Ghost remain subsystem names. An ignored ZIP checkpoint contains 138 prechange source and documentation files. Execution 09 verified ASUS FHD capture, 379 converted frames, 378 accepted observations and explicit Stop/Restart. The Execution 10 baseline reproduced 51 core passes per configuration, 14 worker passes and 27 isolated native passes with one blocked Explorer case. [Checkpoint](../artifacts/execution-10/checkpoint-hash.json) · [Hardware baseline](EXECUTION_09_REPORT.md) · [Native baseline](../artifacts/automated-acceptance/20261106T052630607-0586042a275743d7b000083fcc356c14/results.json)

## B — Testing infrastructure audit

The controlled runner independently checks PIDs, session generations, Stop, stale observations, fixture counters, UIA mutation readback, cursor coordinates and actual WPF keyboard actions. The private desktop and owning Windows job bound the run. The prior IPC timeout abandoned the wait while the read remained active. A subsequent command could reuse a connection with an outstanding read. A separate cleanup exception could prevent results from being written. Recorded and native-file evidence remains separate from real camera evidence.

## C — Defect ledger

| Priority | Finding | State |
|---|---|---|
| P1 | Test IPC timeout left a pending read on the reusable connection | Underlying read and write now use a cancellation token. Timeout closes the pipe |
| P1 | Host cleanup exception could prevent evidence output. Worker stop signal could throw during teardown | Cleanup reports a distinct failure and bounded host wait. Tracking closes the job and confirms exit despite expected pipe-close failures |
| P2 | Stop accounting lacked explicit cancellation reasons and per-stage timing | Added validation, cancellation, transmit interruption, acceptance and latency telemetry |
| P2 | Final real frame rate was about 10/s with a nominal 30 Hz source | Measured stage breakdown below |
| P2 | Isolated typing soak intermittently substituted the previous chunk's last character for the first character of the next | Exact length and focus matched, but digest reconstruction located one substitution at index 199. The test injector now sends layout-resolved ordinary keys |
| P2 | Explorer acceptance lacks an isolated shell. Physical and authorization coverage is separate | Open |
| P3 | Legacy product strings and metadata | Rebranded product surfaces. Internal Windows identities retained |
| Dependency | MediaPipe emits feedback-tensor and projection warnings on the pinned model | Preserved in controlled profiling output. Feedback tensors are disabled by the runtime. Geometry is validated independently. Edge/detection quality still needs broader acceptance |

## D — Confirmed repairs

`Program.cs` bounds the command and invalidates the test transport on timeout, records case times and preserves evidence through controlled cleanup failures. `RecordedTrackingController.cs` keeps capture cleanup reachable after a pipe-close failure and counts rejected observations. The camera diagnostics and shared frame loop distinguish pre-submission cancellation, post-inference cancellation and pipe failure. Core calibration tests isolate mapping, session freshness, loss and invalid geometry.

The first combined run failed `A03_TYPING_SOAK` at batch 0, cycle 5. The reconstructed expected digest matched the saved expected value. Replacing the first G at index 199 with the previous chunk's period reproduced the observed digest exactly. The fixture was still focused and the length matched. This isolates the recorded failure to the seeded input content. The specific Windows packet mechanism remains an inference. `Input.cs` now resolves the fixture's keyboard layout and sends virtual-key down/up pairs on the isolated desktop instead of using Unicode packet injection. The existing per-chunk exact digest and focus assertions still govern the result. [Preserved failed regression](../artifacts/automated-acceptance/20261106T053658401-2c20f4a988c240928ccdf37d060d9a00/results.json) · [Digest reconstruction](../artifacts/execution-10/typing-analysis.json) · [Windows keyboard APIs](https://learn.microsoft.com/en-us/windows/win32/inputdev/keyboard-input)

## E — Safe test execution

Baseline and final regression ran on the existing isolated desktop with a job-owned tree and deadline. The one separately authorized ASUS FHD check used the normal visible Invey lab and diagnostic cutoff. No camera pixels or landmarks were saved. Cleanup was checked by owned process identity and session.

## F — Invey rebrand

The main title and wordmark, settings and About copy, tray menu and camera status, application product metadata, user-facing fixture title and Tracking lab now use Invey. Pronunciation is in-vay. The app remains native WPF with the existing controls and restrained typography.

## G — Rebrand migration testing

`Pocket.App.exe`, assemblies, namespaces, solution, manifest identity, named pipes, mutex, launch arguments and diagnostic environment variables retain their existing compatibility identities. No persisted preference or Pocket reference store is implemented in this build. Therefore no settings or data migration was applied. The recorded, native-file and camera paths continue using the same worker and protocol transport. The new parser accepts legacy observations without dimensions. The preview requires the optional frame dimensions added by the updated worker.

The normal app exposed the Invey window and settings, with Invey product metadata on the existing executable. Launching another instance returned exit code 0 while the original stayed running. It created no additional camera session. The tray strings are verified in source. Actual Explorer shell acceptance remains blocked. [Visible launch](../artifacts/execution-10/visible-launch.json) · [Single-instance check](../artifacts/execution-10/single-instance.json)

## H — Camera pipeline investigation

The synchronous Media Foundation reader owns one sample and borrowed buffer at a time. It validates dimensions and stride, copies the source, releases the buffer and submits one normalized RGB image to MediaPipe VIDEO. No application frame queue is present. A session-bound pipe transmits geometry metadata and landmarks only. New image-free measurements distinguish native read wait, owned copy, RGB preparation, inference, IPC write and WPF acceptance.

## I — 379 versus 378 accounting

Execution 09's final cutoff session recorded 227 converted, 226 submitted, 226 processed, 226 sent and 226 accepted. The two explicit-stop sessions accounted for the remaining 152 conversions and 152 accepted results. The one-frame difference occurred before submission, consistent with the Stop check after a frame is returned. Execution 10 adds an explicit cancellation counter at that boundary. For a successful session, converted frames reconcile with submissions plus conversions canceled before submission. Emitted observations reconcile with accepted and rejected observations, allowing a write that was interrupted to be counted separately.

This execution observed the other Stop boundary. All 247 converted frames were validated, submitted and processed. Cancel arrived after the final inference and before send. The worker counted one `cancelAfterInference`, transmitted 246 results, and the host accepted all 246 with zero rejects or application skips. The recorded cancellation test failed when its Stop guard was removed in a disposable copy, then passed after restoration. [Worker accounting](../artifacts/execution-10/31332329-f1e0-4d0d-8182-e79951202e3f.worker.json) · [Host accounting](../artifacts/execution-10/31332329-f1e0-4d0d-8182-e79951202e3f.host.json) · [Negative control](../artifacts/execution-10/negative-cancel.log) · [Restored test](../artifacts/execution-10/negative-cancel-restored.log)

## J — Webcam performance

One authorized ASUS FHD session on Windows x64 build 26200. The negotiated 1920 × 1080 YUY2 stream advertised 30 Hz and produced RGB32 for processing. It converted 247 frames over 12.71 seconds from first to last frame, about **19.4 frames/s** across 246 intervals. No acquisition default changed. The earlier roughly 10/s and this run used different scenes and durations, so the difference does not establish a speedup.

| Stage | Median / p95 |
|---|---:|
| Media Foundation ReadSample | 15.65 / 21.51 ms |
| Native sample copy | 2.49 / 3.79 ms |
| RGB preparation and image construction | 12.58 / 18.14 ms |
| Inference | 15.82 / 36.04 ms |
| Pipe write | 0.14 / 0.28 ms |
| Estimated source-to-result age | 68.58 / 97.97 ms |

Read wait, pixel preparation and inference contributed materially. The named pipe was small by comparison. The worker delivered its first frame in 2.21 s, and the host accepted its first result in 2.53 s. Stop cleanup took 477 ms including confirmed process exit. Worker CPU time was 13.77 s in a 15.28 s worker lifetime, with a 192 MiB working-set sample at Stop. Physical sensor-to-display latency, exposure effects and driver drop counts were not measured. The reader exposed one native mode in this run, so there is no measured lower-resolution alternative. [Stage measurements](../artifacts/execution-10/31332329-f1e0-4d0d-8182-e79951202e3f.worker.json)

The separate native-file benchmark processed 120 of 123 samples in 11.20 s. Three nonincreasing timestamps were deliberately skipped. Median/p95 inference was 12.62/18.92 ms. Treat this as a controlled source check with a different resolution and pacing. [Controlled benchmark](../artifacts/execution-10/native-file-perf.json) · [Runtime warning log](../artifacts/execution-10/native-file-perf.log)

## K — Memory and resource stability

Capture retains one borrowed sample and one owned frame before inference. COM activation, reader, sample and buffer references use existing scoped teardown. Invey keeps the current observation only. Calibration stores the previous normalized fingertip and readiness counters in memory for the open panel. Short-run memory samples and confirmed worker exit do not establish long-run leak freedom.

## L — B1 calibration implementation

F06 was reviewed against the prior actual-device evidence. This first provisional F07 slice adds **Calibrate** to the existing lab. A letterboxed frame diagram draws the current index fingertip and adjustable working region. The user can mirror the presentation, select Any/Left/Right, or adjust the inset. Readiness requires six distinct fresh frames with no gap above 220 ms and no per-axis jump above .14 normalized units. These are provisional continuity thresholds. Stale, absent, partial, out-of-region and wrong-hand samples clear readiness. The pinned single-hand inference setting leaves simultaneous-hand disambiguation unverified. Cancel stops the source. It stores no settings or hand geometry on disk. Video preview, display routing, sensitivity, persistence and Move/Pinch/Release practice remain future F07 work. [Canonical F07](../codex/MASTER-IMPLEMENTATION-PROMPT.md#f07--calibration-ac-f07-b03b07a11)

## M — Calibration verification

The deterministic tests validated letterboxing, invalid frame size, finite geometry, mirror X, hand selection, edges, repeated frame identity, stale input, empty result and generation reset. The recorded WPF test waited for Ready on actual model observations, independently compared mirrored X to the unmirrored fresh index landmark, and checked worker exit and empty observation on Cancel. The same flow passed in the targeted run. [Targeted native run](../artifacts/automated-acceptance/20261106T053232545-bea5ac53d80c4290a24f478351a4387d/results.json)

On the one authorized webcam check, the actual lab showed Camera active, no hand and then a detected hand. Calibration opened and showed both waiting and working-region feedback. Live Ready was not observed in the bounded session. Cancel immediately removed the preview, cleared the hand, completed native shutdown, and returned the lab to Camera off with confirmed worker exit. No second hardware session was run. Same-device Restart remains verified by Execution 09 and independently covered in the current controlled native suite.

## N — Complete test matrix

| Verification | Observed result |
|---|---|
| Locked restore and Debug/Release builds | Passed. Zero compiler warnings or errors |
| Core tests | 54 passed per configuration |
| Worker tests | 14 passed. Deliberately removed cancellation guard emitted an unwanted observation. Restored copy passed and matched source |
| Targeted tracking and calibration | 7 passed, including Ready, mirrored point and worker exit |
| First combined run before typing repair | 27 PASS, 1 FAIL for exact typed text, 1 BLOCKED |
| Combined rerun, seed 73021 | 28 PASS, 0 FAIL, 1 BLOCKED |
| Combined rerun, seed 73022 | 27 PASS, 0 FAIL, 1 BLOCKED, 1 INCONCLUSIVE |
| Typing after repair | 20 exact digest acknowledgments per seed |
| Authorized webcam | 247 converted, 246 accepted. Explicit Cancel, native release and confirmed worker exit |
| Explorer | BLOCKED. Default desktop has a shell, but the isolated runner has none and this execution's normal-desktop authorization covered the Invey camera and controls |

[Debug tests](../artifacts/execution-10/final-Debug.trx) · [Release tests](../artifacts/execution-10/final-Release.trx) · [Worker tests](../artifacts/execution-10/worker-tests-final.log) · [Seed 73021](../artifacts/automated-acceptance/20261106T054037373-b6b311ce355949f289adb1729be27892/results.json) · [Seed 73022](../artifacts/automated-acceptance/20261106T054236093-f0c297e9822048e2a024b185800ecc9c/results.json)

Reproduce the camera-free checks from the project root:

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
foreach ($mode in 'Debug','Release') {
  & .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c',$mode,'-p:Platform=x64','--no-restore')
  & .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c',$mode,'--no-build')
}
$env:GHOST_NATIVE_VIDEO_FIXTURE = (Resolve-Path artifacts/execution-08/controlled.mp4).Path
& .\.tools\exec05-inference\Scripts\python.exe -B -m unittest discover -s tests/tracking -p 'test_*.py' -v
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73022
```

## O — Crash and error summary

No unexpected application or hardware-worker crash was observed in the executed workflows. Controlled crash and forced-stop scenarios passed. The initial typing failure remains preserved. The second seed's closed-app cursor control observed movement in 85 of 120 samples before the controlled process launched and with the injector inactive. Idle and active cursor checks passed. The prelaunch observation remains inconclusive. The independent post-run audit found Default restored, no scoped app or worker processes, and the real camera session's worker gone. [Cleanup audit](../artifacts/execution-10/final-cleanup.json)

## P — Regression comparison

Execution 09 baseline was 51 core tests per build, 14 worker tests and 27 native passes. The new tests add a calibrated preview and stricter camera accounting. Hardware timing comparisons use the same laptop and reader, with differences in scene and session length.

## Q — Product status

Invey opens the existing Tracking lab and supports explicit live or recorded observation and a provisional working-region preview. Pocket provides the passive panel and simulated reference action. Capturing and storing persistent screen references is later work. Dual provides controlled independent replay and fixture-scoped UIA. Camera observations remain separate from input authority.

## R — Outstanding acceptance

Gate A physical input and Explorer are open. A2 is provisional. M02 ordinary-app control deferral remains proposed. F06 is verified on the ASUS FHD setup but physical loss, denial and formal review remain open. F07 and full B1 acceptance remain open.

## S — Next execution

Continue F07 with explicit display mapping and local numeric preferences, then test the canonical rectangle route and Move/Pinch/Release practice under separate authorization. Stop at B1 review before gesture authority or external actions.
