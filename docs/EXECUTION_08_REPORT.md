# GHOST — Execution 08 engineering report

## A. Initial repository state

The existing checkout is on `codex/a1-automation-a2-provisional` with an unborn HEAD and untracked working files. Execution 07 left a working recorded Tracking lab, 47 .NET passes per configuration, eight Python passes and a 24-pass native run. No commit or release merge was made. [Execution 07](EXECUTION_07_REPORT.md)

Final SHA-256s are `687c799ae5b8308c7ff216cb7e77c81da6cbfecafc1a423a10247b03389f6c5b` for `windows_camera.py`, `052aec11d1c431cf53c641d2f18606d4f46c0408b2aa49f7dcdff7da70a88120` for the worker, and `0f9daf3a5f80cd415505098871de73e7e05ae3fe93263cf1230749f0aec5b099` for the short MP4 fixture.

Execution 08 §4 explicitly permits provisional live F06 implementation and controlled testing. This changes the earlier development sequencing described under Gate A, ADR-05 and M03. It preserves their acceptance requirements and the pending M02 decision. Device consent remains independent. [QA release sequence](QA_MATRIX.md#release-sequence) · [Canonical milestones](../codex/MASTER-IMPLEMENTATION-PROMPT.md#23-roadmap-and-stop-boundaries)

## B. Defects discovered

| Reproduction | Finding |
|---|---|
| Open the controlled H.264 MP4 through Media Foundation's advanced conversion | Requesting RGB32 failed with `0xC00D36B4`. The standard conversion accepted the same format |
| Open the same file inside the pinned inference worker after model import | Media Foundation COM initialization failed with `0x80010106`. The inference import had already initialized the thread in a different apartment |
| Select recorded tracking, Stop, then invoke Alt+R in the first integrated build | The source radio and Restart shared the same access key |
| Inspect the original private-desktop cursor probes | The prelaunch sample was a useful control but lacked an explicit closed-state check and settle window. Idle and active samples started without waiting for confirmed fixture state |
| Read Stop and worker-exit state transitions | Core could accept a worker's Stopped record as cleanup proof. The WPF supervisor already waited for actual exit |
| Review normalization at frame edges | Execution 05 observed 10 out-of-frame points with a real model. The worker folded that result into no-hand, losing the distinction between an observed partial hand and an empty inference |
| Run full native regression after the tracking fixes | A2's ready-marker poll could see a newly created file while the adverse fixture still held it open for writing. The runner failed with a sharing violation before testing that provider |

Format reproduction (`artifacts/execution-08/format-probe.log`, local only) · COM reproduction (`artifacts/execution-08/exact-runtime-reader.log`, local only) · First WPF run (`artifacts/automated-acceptance/20261105T172330234-bb793ed4fe2948bab84c5d4f71081ccd/results.json`, local only) · Second WPF run (`artifacts/automated-acceptance/20261105T172556661-bcd585823ac4443ea8d812eb93c73d82/results.json`, local only) · First full regression (`artifacts/automated-acceptance/20261105T173100374-5e3f59913fdd4b6a84f78e509c1d4b21/results.json`, local only) · [Edge evidence](EXECUTION_05_REPORT.md)

## C. Repairs

- `tests/tracking/windows_camera.py` now uses the standard Source Reader video converter. The worker establishes COM MTA before importing inference dependencies and balances COM/Media Foundation lifetimes.
- `src/Pocket.App/Tracking/` owns camera inventory and source selection, uses distinct access keys, and locks source controls while starting or active. The app tray follows camera Starting, Active, Releasing and Off.
- `Pocket.Core/Tracking` requires independent process-exit confirmation before releasing the generation. A version-1 observation can carry `partial: true` only with an empty landmark payload and no tracked hand. Finite out-of-frame geometry yields this status. Nonfinite or wrong-count geometry faults. In-frame validation remains strict and no coordinates are clamped.
- The runner verifies process state and replay activity around each cursor interval. No product cursor-control code was changed.
- The adverse and standard UIA fixtures stage the ready marker, close it and rename it into place. Readers see only the finished marker. The app's UIA and mutation policy were unaffected.

## D. Camera implementation

Windows Media Foundation is the sole live backend. **Find cameras** uses `MFEnumDeviceSources` for video metadata and returns bounded friendly names and digests of symbolic links. It does not activate a source. Enable re-resolves the chosen digest and uses `MFCreateDeviceSource` for that exact link. The supervised worker selects the first video stream only, prefers a mode near 640 × 480 and 30 Hz, and requests RGB32 with a 1920 × 1080 pixel ceiling. Selection can fall back to another supported format on the same device.

The synchronous Source Reader runs in the supervised Python worker. One read, conversion and inference is in flight and there is no application backlog. Capture requests two source buffers and low-latency operation. Actual driver buffering requires a device test. RGB32 samples are copied from the locked surface after checked dimensions, range and stride. Both positive and negative stride are handled. The borrowed buffer, sample and COM references are released on the worker thread before publishing. Normal Stop shuts down the source and model. The parent invalidates observations immediately and waits for exit. Its job terminates a worker blocked in native code. An unconfirmed exit holds Restart. [Device enumeration](https://learn.microsoft.com/en-us/windows/win32/medfound/enumerating-video-capture-devices) · [Source Reader](https://learn.microsoft.com/en-us/windows/win32/medfound/processing-media-data-with-the-source-reader) · [Buffer layout](https://learn.microsoft.com/en-us/windows/win32/api/mfobjects/nn-mfobjects-imf2dbuffer)

## E. MediaPipe integration

The existing Python 3.12.10 and MediaPipe Tasks 0.10.32 CPU VIDEO worker and model remain pinned. The native reader and recorded source share the same serial frame validator, BGR-to-sRGB conversion, monotonic timestamp check, inference, normalization and PID-authenticated named-pipe transport. A live reader rejects duplicate or backward presentation times and discards samples with at least 250 ms of estimated backlog from relative source timing. This estimate excludes the unknown initial sensor/driver delay. Missing samples do not emit a no-hand observation. The host expires stale results independently, and cancellation prevents publication after Stop. [MediaPipe API](https://developers.google.com/edge/api/mediapipe/python/mp/tasks/vision/HandLandmarker)

## F. User-facing behavior

Open **Tracking lab**. Recorded tracking is the default and the camera is off. To use a camera, choose **Live camera → Find cameras**, select a device, then choose **Enable live camera**. Discovery only lists devices. The lab and tray show camera status. The lab displays a detected hand, an empty result, a partial hand or a stale result separately. **Stop** clears observations and waits for worker exit. **Restart** is available after cleanup and requires another deliberate action. The live device path is integrated but device-unverified. [Development launch](EXECUTION_06_REPORT.md#d-actual-f06-interaction)

## G. Test results

Final core and worker checks are reproduced from the project root. The pinned Execution 05 environment and an installed FFmpeg with libx264 are needed to regenerate the controlled test video. FFmpeg is a test tool. The live app does not use it.

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Release','--no-build')
New-Item -ItemType Directory -Force artifacts/execution-08
& .\.tools\exec05-inference\Scripts\python.exe -B tests/tracking/prepare_native_fixture.py --palm .tools/exec05-model/public-palm.jpg --out artifacts/execution-08/controlled.mp4
$env:GHOST_NATIVE_VIDEO_FIXTURE = (Resolve-Path artifacts/execution-08/controlled.mp4).Path
& .\.tools\exec05-inference\Scripts\python.exe -B -m unittest discover -s tests/tracking -p 'test_*.py' -v
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
```

| Coverage | Evidence category | Verified outcome |
|---|---|---|
| Core protocol and lifecycle | Deterministic and real local IPC | 49 passes per configuration. Rejects partial packets carrying usable points, old generations and worker Stop claims before exit |
| Worker source tests | Deterministic and native file reader | 13 passes. Signed stride, ownership, channel order, camera error mapping, no-device selection, EOF/loss, native reopen and cancel |
| Recorded WPF lifecycle, stale result, worker faults | Native software and real recorded inference | Actual controls, loss and return, delayed observation clearing, forced process exit and new session |
| Native file reader to WPF | Native Media Foundation, recorded real inference, WPF | Actual RGB32 reader and model, loss/return, Stop, new worker/generation and close |
| Camera permission feedback | Controlled failure and native UI | Explicit selection remains off. Injected denial displays Windows privacy guidance and clears the worker |
| Camera metadata | Native Windows API | One device listed, zero activated |
| Authorized physical or virtual acquisition | Hardware | BLOCKED, zero frames |

Debug TRX (`artifacts/execution-08/final-verified-Debug.trx`, local only) · Release TRX (`artifacts/execution-08/final-verified-Release.trx`, local only) · Worker tests (`artifacts/execution-08/worker-tests-final.log`, local only)

## H. Test independence

The real file-reader case uses an MP4 generated from the attributed public palm photo and controlled blanks. Known red and blue corner markers independently verify channel order and top/bottom orientation. Native WPF uses the actual Enable, Stop, Restart and Close controls. The runner checks worker PIDs, session IDs and window handles independently. Device errors from HRESULT mapping, missing selection, simulated end of stream and injected permission denial are controlled cases. The real metadata probe found one device and opened zero. The file input is explicitly labeled and uses no camera acquisition API. Redacted discovery evidence (`artifacts/execution-08/native-discovery.json`, local only) · [Public fixture attribution](EXECUTION_05_REPORT.md)

Reversing the output rows in a disposable copy failed the native corner-marker test and the independent signed-stride test. Restoring the source restored the tests and matched the original hash. Fault-injected test (`artifacts/execution-08/negative-orientation-failure.log`, local only) · Restored test (`artifacts/execution-08/negative-orientation-restored.log`, local only)

## I. Performance

Windows 11 x64 build 26200, Intel Core i5-13450HX, CPU inference. These are controlled Media Foundation file runs with a local sink. Their pacing, driver characteristics and latency do not establish camera performance.

| Metric | Short | Extended |
|---|---:|---:|
| Duration | 10.94 s | 66.13 s |
| Raw samples acquired / submitted / processed / emitted | 123 / 120 / 120 / 120 | 723 / 720 / 720 / 720 |
| Raw sample rate / observation output rate | 11.24 / 10.96 per s | 10.93 / 10.89 per s |
| Camera frames | 0 | 0 |
| Nonincreasing source timestamps skipped | 3 | 3 |
| Pending application frames | 0 | 0 |
| Source setup / model setup | 80.07 / 57.87 ms | 74.70 / 57.44 ms |
| RGB preparation and MediaPipe image construction, median / p95 | 1.69 / 2.26 ms | 1.66 / 2.26 ms |
| Inference, median / p95 | 17.87 / 25.21 ms | 17.53 / 25.20 ms |
| Reader delivery to observation, median / p95 | 20.46 / 27.51 ms | 19.89 / 27.85 ms |
| Process CPU time | 3.34 s | 18.14 s |
| Working set at start → end | 162.73 → 179.02 MiB | 163.04 → 181.19 MiB |

The extended run sampled memory every 120 results, plus before and after the loop. It grew during warm-up and finished below the last periodic sample. The run is too short to establish long-run memory stability. Counts are synchronous results, with no asynchronous MediaPipe callback. Source Reader copies occur before the separate RGB preparation timer and are included in reader-delivery age. Short measurements (`artifacts/execution-08/native-short.json`, local only) · Extended measurements (`artifacts/execution-08/native-extended.json`, local only)

The final private-desktop native file test reached a first hand in **1.72 s**, stopped with confirmed process exit in **135.72 ms**, and restarted to the first hand in **1.76 s**. These are keyboard-to-observed-state measurements, including WPF and runner polling. The separate recorded lab run measured **92.22 ms** maximum sampled age since host receipt. Compositor timing, sensor age and live driver queue depth remain unmeasured. Native timing and observations (`artifacts/automated-acceptance/20261105T173443873-72db4162abd54d28bd1f6125873e0f20/results.json`, local only)

## J. Failure handling

The worker distinguishes absent or removed source, denied access, known busy/preempted HRESULTs, unsupported format and generic runtime errors. A successful metadata scan never establishes capture permission. Unknown failures retain a generic category rather than inventing a diagnosis. Diagnostics retain only fixed phases and a validated numeric native code. Windows privacy settings are left untouched. The UI tells the user where to look after denial. Startup timeout and health timeout clear observations and contain the process. Stop during startup/inference clears the current observation before teardown. An unexpected worker exit invalidates the session. Repeated Enable, premature Restart, old-generation records and misordered frames cannot produce a fresh valid hand. [Windows privacy guidance](https://learn.microsoft.com/en-us/windows/apps/develop/camera/camera-privacy-setting)

## K. Explorer and cursor checks

The three cursor intervals explicitly record mode, process identity, UTC boundaries and replay progression. The closed control settled for 600 ms after switching desktops and verified that Pocket.App had not launched. It observed movement in 38 of 120 samples with the injector inactive and remains **INCONCLUSIVE**. Idle and active checked the same running host PID before and after their intervals. Idle replay events stayed at zero, active advanced from 2 to 127, and neither interval saw cursor movement. No cursor reset or product input change was made. These are native software observations.

The private desktop has no Explorer shell, so the runner records A10 as **BLOCKED** and performs no shell action. The read-only post-run audit found `explorer.exe` on the normal Default desktop. No separately authorized shell-enabled test session was identified, and the normal desktop received no injected input. Physical and shell acceptance remain open. Cursor and shell results (`artifacts/automated-acceptance/20261105T173443873-72db4162abd54d28bd1f6125873e0f20/results.json`, local only) · Read-only cleanup audit (`artifacts/execution-08/final-cleanup.json`, local only)

## L. Regression

Locked restore passed. Final Debug and Release builds had zero warnings/errors and **49 passing core tests each**. The final short native run with seed 73021 recorded **26 PASS, 0 FAIL, 1 BLOCKED, 1 INCONCLUSIVE**. Click-through, typing, held drag, idle/active cursor, controlled UIA targeting and postconditions, IPC, mutation uncertainty, recovery, recorded tracking and the new native file tests passed. The four late-mutation scenarios passed after fixing the marker race. The final app SHA-256 is `a96c43bc692b747ef8d74bccbfce3b8d126b3f26799ea8f4c23c1eaf60b720d6`. The separate audit observed the Default desktop and zero scoped workers. Final regression (`artifacts/automated-acceptance/20261105T173443873-72db4162abd54d28bd1f6125873e0f20/results.json`, local only) · Cleanup audit (`artifacts/execution-08/final-cleanup.json`, local only)

## M. Product progress

| Capability | Current state |
|---|---|
| POCKET | Passive panel and simulated reference action. Capturing, storing and recalling a useful screen excerpt remain later work |
| DUAL | App-managed simulated pointer with software click-through, focus, held-drag and cursor evidence. Optional ordinary-app control remains hidden and controlled A2 fixtures remain intact |
| GHOST | Explicit camera selection, source adapter, local inference and visible lifecycle implemented. Recorded and native file reader paths are verified. An authorized device test is outstanding |
| Integration | Source → shared worker → authenticated pipe → WPF, with tested independent teardown. Gestures and POCKET manipulation are later work |
| Design | Compact native controls, ordinary keyboard navigation and text/live-region status. Visual and assistive-technology coverage remains limited |

## N. Acceptance status

| Level | Status |
|---|---|
| Implemented | Provisional live Media Foundation adapter and in-app controls |
| Deterministically verified | Frame geometry, channel/stride handling, ownership, generation, stale state and fault classification |
| Native integrated | Metadata enumeration and actual Source Reader file path through WPF and MediaPipe |
| Camera-device verified | BLOCKED. Zero virtual or physical devices activated |
| Gate A / A2 / M02 | A1 physical and shell open. A2 fixture scope provisional. M02 deferral remains a recommendation for formal review |
| Release ready | Pending mandatory acceptance, authorized device lifecycle, exact dependency/notice review and clean-host packaging |

## O. Remaining issues

1. **P0:** Verify the complete live path on an explicitly authorized camera or virtual source, including denial, disconnect, contention and actual device release/reacquisition.
2. **P0:** Close Gate A evidence and record the M02 disposition before release acceptance.
3. **P1:** Measure real device latency, drop behavior and sustained CPU/memory. Verify clean-host runtime and notices. Controlled performance does not establish a live camera target.

## P. Next engineering execution

The next canonical work is to complete the M03/B1 F06 device evidence and review. Then implement F07 calibration on one verified camera and display. After B1, B2 adds intentional gesture ownership and release, and C adds the first real POCKET capture/store/recall path. Those dependencies lead to the first supported gesture manipulation of a retained reference alongside normal Windows input. [Canonical phases](../codex/EXECUTION-PHASES.md)
