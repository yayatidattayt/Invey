# GHOST — Execution 09 laptop webcam verification

Date: 2026-11-06, India Standard Time. The existing provisional branch `codex/a1-automation-a2-provisional` has an unborn HEAD and untracked working files. The checkout and previous evidence were retained.

## A. Executive verdict

**YES — real frames verified.** The ASUS FHD webcam was explicitly selected in the visible Tracking lab. The real Windows Media Foundation source delivered frames to the pinned MediaPipe worker. The lab showed both a detected hand and an empty result. Explicit Stop released the source, and the same device delivered fresh frames in a new worker after Restart.

## B. Device identity and compatibility

Windows reported two present USB devices, ASUS FHD webcam and ASUS IR camera. The Media Foundation camera picker exposed the ASUS FHD webcam only. The selected camera's opaque digest prefix was `cd2e0e71deb9985c`; each worker revalidated the full digest against the symbolic link in fresh enumeration. The active video stream advertised **1920 × 1080 YUY2 at nominal 30 Hz** through the reader. This is the active reader's offered mode list. Other driver configurations were outside the measurement. Windows negotiated RGB32 at the same dimensions with a 7,680-byte stride. Color capture was selected and the separate IR sensor was left idle. Device inventory (`artifacts/execution-09/device-metadata.json`, local only)

## C. Camera connection stages

Final-build session `ba557b8f…`, worker PID 16760. Times are milliseconds since the worker diagnostic began unless marked host. Every listed native stage succeeded, with no failing HRESULT.

| Stage | Actual result | Start → completion |
|---|---|---:|
| A. Identity | MFEnumDeviceSources matched ASUS FHD webcam | 1391.29 → 1394.07 |
| B. COM and Media Foundation | CoInitializeEx MTA and MFStartup succeeded | 1.22 → 2.95 / 1388.89 → 1390.42 |
| C. Activation | MFCreateDeviceSource opened the selected device | 1395.52 → 1567.28 |
| D. Reader | MFCreateSourceReaderFromMediaSource succeeded | 1568.63 → 1571.29 |
| E. Format | YUY2 to RGB32 at 1920 × 1080 | 1572.06 → 1635.69 |
| F. First usable frame | ReadSample and validated conversion completed | 1635.69 → 2124.86 from format ready |
| G. Continuous delivery | 227 converted samples, zero app skips | 2124.86 → 24821.70 |
| H. Inference and IPC | 226 submitted, completed, emitted and accepted | First worker result 2159.58. First host result 2357.75 after Enable |
| I. Stop | Diagnostic limit, native shutdown and observed process exit | Native shutdown 25166.64 → 25176.34. Host cleanup 494.92 ms |
| J. Reacquisition | Second session opened the same digest with new PID 29064 and accepted 63 results | First usable frame 1790.69 from that worker start. First host result 2004.55 after Restart |

First worker (`artifacts/execution-09/93a9bc57-1de9-4397-a663-2637da2688a8.worker.json`, local only) · Restart worker (`artifacts/execution-09/b987465a-79e2-4c6e-be66-6fe0827f4769.worker.json`, local only) · Final worker (`artifacts/execution-09/ba557b8f-ec27-44b7-ac41-c9f7007753d0.worker.json`, local only)

## D. Windows permission results

The read-only Windows consent entries reported Allow at device, user and desktop-app scopes. No camera override was found at the inspected AppPrivacy policy location. Successful source activation and delivery establish permission for this desktop app in this session. No privacy setting, permission, hardware shutter or other camera app was changed.

## E. Media Foundation diagnostics

The adapter accepts known color subtype families and bounds the delivered geometry. It records exact native errors by connection stage and terminates the reader on `MF_SOURCE_READERF_ERROR`. End-of-stream invalidates a live device. A null sample or stream tick yields no hand result. Native or current media-type change revalidates the output layout and can renegotiate before subsequent delivery. No further reader method is called after the terminal error flag. Both explicit hardware sessions reported one startup stream tick and no failed HRESULTs. [Source Reader flags](https://learn.microsoft.com/en-us/windows/win32/api/mfreadwrite/ne-mfreadwrite-mf_source_reader_flag)

## F. Live frame delivery

The source read successive real samples, checked monotonic timestamps and signed buffer layout, copied the buffer before releasing it and converted BGR to contiguous sRGB. The valid delivered format and independent fixture color/orientation assertions establish the conversion contract. The hardware tests retained no image to independently score scene colors. Reader and application drop counts are separate. Only application skips are measured. [Existing native conversion evidence](EXECUTION_08_REPORT.md#h-test-independence)

## G. Live MediaPipe inference

The original pinned MediaPipe Tasks 0.10.32 CPU VIDEO model processed the hardware samples. Across three sessions the source converted **379 frames** and the WPF host accepted **378 observations** over its PID-bound named pipe. Of the emitted observations, **95** contained a hand and **283** were empty. In the final run one converted frame was canceled before model submission. The experiment did not score gesture accuracy or hand classification against annotated ground truth.

## H. Tracking lab behavior

Observed in the running app through keyboard and pointer actions. The lab started on Recorded tracking with Camera off. Live selection and Find cameras left it off. Choosing ASUS FHD webcam enabled the real Enable button. While active, the lab showed Camera active and locked source controls. Live text showed **Hand detected · 21 landmarks** and **No hand detected** in separate observations. Stop showed **Camera releasing** and cleared the current hand. It then showed **Camera off** and made Restart available. A pointer click on the Restart button established the second session. One earlier keyboard attempt did not activate Restart; the observed button path worked. The existing keyboard path also ran in the controlled acceptance suite. The live no-recent-result state was verified with controlled delayed delivery, not produced by a physical interruption.

## I. Connection errors and repairs

- The initial adapter collapsed device activation, source-reader startup and delivery faults into generic failures. Added staged HRESULT capture, explicit error categories and bounded no-frame/stream-interruption handling in `windows_camera.py`, `TrackingSession.cs`, `TrackingProtocol.cs` and the lab.
- The generic controller header claimed Camera off even when the separate lab could run a camera. The header now points to Tracking lab, where the live state is maintained. The actual lab and tray continue using the source lifecycle.
- The first authorized hardware attempt delivered real frames. No hardware connection failure was reproduced, so the selected acquisition architecture and pinned model were retained. Diagnostics are opt-in with a visible 25-second cutoff. The worker and host emit image-free evidence independently.

## J. Stop and restart

The first two hardware sessions stopped through the actual lab control. Each cleared its observation, completed `IMFMediaSource::Shutdown`, and confirmed the worker exited. Restart selected the same digest, generated a fresh session and PID and accepted fresh frames. The old generation was gone before restart. The final session exercised the diagnostic time limit. Final process cleanup was checked independently.

## K. Failure tests

Controlled tests cover missing device, classified Windows errors, terminal reader error with concurrent media-type flags, EOF, delayed results, corrupted messages, startup cancellation, forced termination, and stale generation rejection. A no-frame timer and an interrupted-stream timer report different outcomes. Physical disconnection, a closed shutter and OS denial were not induced. An internal laptop device and current privacy configuration remained intact.

The injected terminal-error test failed when that check was removed in a disposable copy. The failure independently caught a second native method call after the terminal flag. The restored code passed and matched the original hash. Faulted test (`artifacts/execution-09/negative-reader-confirmed.log`, local only) · Restored test (`artifacts/execution-09/negative-reader-confirmed-restored.log`, local only)

## L. Performance

Windows 11 x64 build 26200, Intel Core i5-13450HX, Python 3.12.10, .NET 10.0.14, Release, MediaPipe CPU. All values below are from the laptop webcam. Worker time starts before inference import. Host time starts when the run begins after the WPF command.

| Metric | Enable | Restart | Final cutoff run |
|---|---:|---:|---:|
| Device activation API | 182.33 ms | 172.25 ms | 171.76 ms |
| First converted frame, worker clock | 2.65 s | 1.79 s | 2.12 s |
| First accepted result, host clock | 2.88 s | 2.00 s | 2.36 s |
| Frames converted / results accepted | 89 / 89 | 63 / 63 | 227 / 226 |
| Inference median / p95 | 10.58 / 19.92 ms | 10.46 / 19.10 ms | 13.00 / 22.87 ms |
| Estimated source-to-result age median / p95 | 37.43 / 47.31 ms | 37.63 / 47.85 ms | 45.18 / 55.15 ms |
| Worker elapsed / CPU time | 11.97 / 4.09 s | 8.48 / 2.95 s | 25.19 / 9.97 s |
| Working set at Stop | 190.61 MiB | 186.01 MiB | 189.93 MiB |
| Host cleanup and confirmed exit | 568.89 ms | 525.79 ms | 494.92 ms |

The final 22.70 s delivery interval averaged about **10.0 frames and 10.0 observations per second**. The driver advertised 30 Hz. Application queue depth and timestamp skips were zero. The timing is relative to delivery timestamps and excludes unknown initial sensor and driver latency. Stop was responsive to the UI actions. Actual display composition latency and long-run memory stability were outside this short test.

First host (`artifacts/execution-09/93a9bc57-1de9-4397-a663-2637da2688a8.host.json`, local only) · Restart host (`artifacts/execution-09/b987465a-79e2-4c6e-be66-6fe0827f4769.host.json`, local only) · Final host (`artifacts/execution-09/ba557b8f-ec27-44b7-ac41-c9f7007753d0.host.json`, local only)

## M. Test validity

Device verification used three real WPF sessions and independent Media Foundation and host metadata. The reader counted samples separately from host-accepted observations. The final build also counted model submissions and completions. A stage that returned success alone was insufficient for the verdict. Device shutdown, worker exit and reacquisition were separately checked. The existing file fixture verified orientation and the isolated suite verified failure recovery. It supplied no hardware evidence.

## N. Regression

Locked restore succeeded. Debug and Release built without warnings or errors and each passed **51** core tests. The final Python suite passed **14** tests, including native file reopening. The isolated Windows short profile with seed 73021 recorded **27 PASS, 0 FAIL and 1 BLOCKED**. A1/A2 controlled checks and recorded, native-file and failure tracking cases passed. Closed, idle and active cursor controls passed in their verified software states. Explorer Alt+Tab and tray shell acceptance remained blocked in the private desktop. The normal Explorer desktop was available for the authorized GHOST camera UI, with no Explorer or shell task exercised.

In the normal application, the second instance exited with code 0, retained the original instance and started no capture. The app was closed through its own Exit command. The independent cleanup audit found the Default desktop and no GHOST process or worker. One numeric worker PID had been reassigned to a later process, so the cleanup audit checked the process lifetime and session as well as the PID.

Reproduce the camera-free portions from the project root:

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Release','--no-build')
$env:GHOST_NATIVE_VIDEO_FIXTURE = (Resolve-Path artifacts/execution-08/controlled.mp4).Path
& .\.tools\exec05-inference\Scripts\python.exe -B -m unittest discover -s tests/tracking -p 'test_*.py' -v
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
```

For a separately authorized live diagnostic, launch the normal GHOST app visibly with `GHOST_CAMERA_DIAGNOSTICS` pointing to an existing local evidence directory. Choose **Live camera → Find cameras**, select the explicitly authorized device and use **Enable**. The lab displays the 25-second limit. Stop and Restart use the standard UI.

Debug TRX (`artifacts/execution-09/final-Debug.trx`, local only) · Release TRX (`artifacts/execution-09/final-Release.trx`, local only) · Worker tests (`artifacts/execution-09/worker-tests-final.log`, local only) · Native regression (`artifacts/automated-acceptance/20261105T190951613-7c4125fc0c0d4975af1bb96265e3d743/results.json`, local only) · Single-instance check (`artifacts/execution-09/single-instance.json`, local only) · Cleanup audit (`artifacts/execution-09/final-cleanup.json`, local only)

## O. Privacy

Activation used the app's deliberate Enable or Restart in a visible, time-limited diagnostic instance. The tool read status text and the stopped lab's interface. The lab has no video preview and the diagnostics contain technical metadata only. Frames were neither saved nor uploaded. No microphone was opened. Default startup stayed off. The diagnostic timer cancels the session, and the owning Windows job bounds worker cleanup. Gate A, M02 and release conditions were left intact.

## P. Current product completeness

POCKET has a passive panel and simulated reference action. Real screen-excerpt capture, persistence and retrieval remain later work. DUAL has controlled app-managed replay, independent cursor/focus evidence and fixture-scoped UIA. GHOST now has real laptop webcam hand observation through Tracking lab. The running integration is camera → MediaPipe → named pipe → WPF. Calibration and manipulation of retained POCKET references remain later milestones.

## Q. Gate and milestone state

| Milestone | Status |
|---|---|
| Gate A / A1 | Open for physical input and Explorer acceptance |
| A2 | Provisional controlled fixture and reliability evidence |
| M02 | Deferral of optional ordinary-app control remains proposed |
| F06 / B1 | Laptop color webcam, inference, Stop and reacquisition verified on this setup. Other devices, physical fault conditions and formal review remain open |
| Release | Pending canonical gates, hardware breadth, license/package audit and clean-host validation |

## R. Remaining issues

1. **P0 acceptance evidence:** physical Gate A, Explorer, M02 review and authorized device fault coverage.
2. **P1 performance:** establish driver-side drop/latency and evaluate smaller input modes if the camera exposes them through an eligible path. Nominal 30 Hz does not describe observed delivery.
3. **P1 packaging and coverage:** validate exact dependencies, model notices, additional cameras and clean Windows setup.

## S. Next execution

Review F06 on the verified ASUS FHD device. The next useful B1 slice is F07 calibration on that camera and the current display. Then validate intentional app-owned gesture and release behavior under B2, followed by actual POCKET capture, storage and retrieval under C. This execution stopped at F06. [Canonical phases](../codex/EXECUTION-PHASES.md)
