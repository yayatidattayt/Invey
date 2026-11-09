# Execution24 — camera-free performance recovery

**Camera-free candidate delivered; physical30FPS and live motion recovery NOT ESTABLISHED. Presentation uses explicitly controlled input.**

## A–D. Baseline and Source Reader analysis

Starting head `031fad31c1867a50ad80e15242f72ee0f4e1655a`, clean existing `codex/execution-20-interaction-engine`, draft PR9; remote head checked through connector. Read current rules/handoff, Execution21–23 evidence, precision/research/QA guides and actual capture, frame, preview, mapping and filter implementations. Preserved inherited features and exhausted authorization.

Historical physical result: about9.8 unique processed results/s,1080p30 advertised, native read median94.65ms/p95111.09ms, inference18.60/24.90ms and source-to-result48.12/62.20ms. These stages overlap/use different clock mappings; they are not additive. Source delivery/conversion/exposure is a stronger hypothesis than inference alone, not a proven single cause. Both physical quality tests failed. The rejected interpolation remains removed.

Microsoft documents limited video processing and separate advanced processing, including resizing and frame-rate conversion. Advanced processing/GPU is not automatically faster. We retain the existing RGB32 Source Reader and low-latency configuration, not a new backend or converter-disabling change that would break RGB output. Explicitly setting the offered native size/rate on RGB output now avoids leaving these output attributes unspecified; if rejected, the original subtype-only negotiation remains available automatically.

## E–I. Format, conversion and bounded diagnostics

Pure tested format policy validates dimensions, finite advertised positive rate≤240, supported subtype, and bounded image area. It ranks offered≥29.9Hz modes first, then useful VGA/720p workloads near640×480. It never invents a mode or overrides the device's advertised list. Lower-rate supported formats remain fallback. RGB conversion remains through the qualified MF decoder for NV12/YUY2/MJPEG/etc.; no untested manual YUV decoder is enabled. Failed preferred negotiations fall back. Historical ASUS enumeration offered only one1080p30 type; camera-free inspection cannot establish whether the device or source configuration restricted discovery. That question remains OPEN.

Measured optimization: while a validated MF buffer is locked, create a temporary strided view and copy directly to owned contiguous RGB. The previous path copied native BGRA bytes then copied a BGR view to RGB. Native orientation/negative stride/span validation and ownership are retained. The returned image owns its data before unlock; no native view escapes. `INVEY_RGB_COPY_BASELINE=1` selects the original copy/conversion path for a future controlled comparison and grants no camera authority. No inference resizing is enabled by default, so calibration frame geometry/personal context does not silently change.

Added separate bounded summaries for native PTS intervals, read-completion intervals, blocking duration, last native timestamp/sequence, negotiated output rational rate, format policy/output negotiation, native copy and capture-to-submission age. Existing counters retain source-read, conversion/image, inference, fallback, IPC, unique frames and maximum pending frame. Capture-to-submission age includes clock alignment/copy/handoff and is not pure queue residence. PTS and delivery intervals are distinct; neither is optical latency. Diagnostic arrays retain their existing1500-sample bound and never store pixels/landmarks. Mapper additionally exposes bounded-vocabulary `UiRejectionPolls`, explicitly UI polls rather than worker frames; visible rejection statuses remain distinct. Actual host updates/rendered-unique preview counters remain separate; full WPF render FPS is not measured here.

Exposure remains a hypothesis: Microsoft's auto-exposure-priority contract permits frame-rate variation with light. No exposure, driver, privacy or system property was changed. A later separately authorized comparison should hold source mode constant and compare even lighting, PTS cadence, delivery cadence and read wait. All12 session slots are exhausted; this is a future procedure, not permission.

## J–L. Reproducible comparisons and rejected candidates

`tests/tracking/benchmark_capture.py`: seed24 synthetic buffers,40 conversions/candidate at640×480,1280×720,1920×1080; equal RGB pixels required. Results `artifacts/execution-24/conversion.json`. VGA median1.3366ms→0.7791ms;1080p7.89665ms→6.15615ms (p958.2584→6.6704ms). Native-owned copy traffic at this boundary7→3 bytes/pixel; MediaPipe image creation may still copy. These are unpaced memory benchmarks, not camera FPS or a fix for95ms native read wait.

`benchmark_inference.py`: repeated approved still imagery with preserved aspect ratio,25 timestamped VIDEO calls/input, first5 excluded from timing. Initial full-image variant detected0/25 at every size; preserved as `inference-negative.json`, not called quality success. Existing controlled640×480 canvas detected25/25, median17.7646ms/p9518.8982ms including image creation. Resized960×720 and1440×1080 variants detected0/25 despite faster timings; preserved in `inference-controlled.json` and rejected as evidence for a default resizing optimization. No detection threshold was weakened. Static detection is not motion/partial-hand accuracy, and a synthetic source is not a30FPS sensor.

Candidate selection: adopt tested direct RGB copy and offered-mode/explicit-output policy with legacy fallback. Reject blind advanced processing, GPU enablement, new capture backends, manual YUV decoding and inference resizing without reliability evidence. Keep latest-frame-only capture thread/COM ownership, bounded queue, Stop and guardian unchanged. No pipeline-wide sustained30FPS claim follows from sub33.3ms processing stages alone.

## M–R. Pointer disappearance, precision and rendering

Existing distinct mapper statuses expose wrong hand, missing context, working-region exit, large inconsistent movement, warmup and loss. Four-observation acquisition spans three sample intervals (~300ms at10Hz/~100ms at30Hz). Reviewed resets for session/hand/frame/continuity, actual loss and source age; did not shorten identity protection without measured reliability. Preview retains250ms source-age bound, strict gesture/readiness150ms and600ms visual continuation. No missing point is invented. Existing deterministic stationary/slow/rapid/small/step/reversal/spike/irregular/dropout filter tests run in Core. No new jitter tuning or claimed physical precision gain; parameters are preserved.

No rejected interpolation, synthetic intermediate landmarks, extrapolation or camera action issuer. Shared passive pointer, direct controlled movement, deduplicated camera preview and visible source/device/calibration/Stop remain. The previous UI recovery is inherited, not claimed physically requalified.

## S–AA. Demonstration and regression

`scripts/Start-Invey-Presentation.cmd` delegates to existing Beta launcher, isolated demo profile by default, camera off, real Pocket samples/cards and explicit controlled keyboard inputs. Guide: `INVEY_PRESENTATION_GUIDE.md`. No external input, second app, private capture or normal-profile modification. This is a thin presentation entry to existing behavior, not a fake webcam demonstration.

Pinned locked restore passed; Debug/Release builds zero warnings/errors; Core171/171 each, zero skips. Worker32/32, zero skips with generated native MP4 fixture: baseline29 preserved plus offered-mode and direct-copy color/stride/ownership/range tests. Native file reader reopens, matches color/orientation and stops using the same MF path. LatestCapture30/60Hz controlled tests preserve bounded handoff and cleanup; they are not sensor benchmarks. All logs/results retained under `artifacts/execution-24`.

Focused native seed24001 run `20261109T195800024-75a36a89577047c2ab018e7398370a7d` passed actual Tracking layout/preview/lesson, controlled card drag/release/pin/restart, independent Workspace ownership, native cursor/editor focus, Stop and cleanup. Broader Pocket run seed24002 `20261109T200021339-688a46d0a53b4599a35974b37270a64d`:1 PASS/3 FAIL, all failed workflows blocked by GameInputServiceWindow foreground on the private desktop. Failures remain failures; broader native acceptance BLOCKED, not inferred from Core. No unsafe retries, service changes or weakened assertions. Shell/manual acceptance remains open. Presentation preflight and final published-commit CI are recorded in delivery evidence; application is left closed.

## T–U,AB–AE. Hardware boundary, delivery and next physical evidence

No camera opened. Durable ledger remains12 released,total1054077ms,0 pending, SHA256 `E578B2FE328B260AE02277E318BAFA66DE8525A7B1B3CAC34459B0CC15A59486`. No amendment, alternate-device access, changed exposure or raw-frame persistence. Existing Pocket/Dual/learning/calibration/gesture/diagnostic tests retained; no live actuation.

Physical30FPS is **UNVERIFIED**, and historical physical quality remains FAILED. Capture delivery, low-light/exposure, actual offered modes and real fingertip precision require a fresh explicit authorization policy decision after cap exhaustion. Next test should compare original and direct-copy paths with the same offered mode and lighting, inspect PTS vs delivery, verify source/processed/accepted/rendered-unique rates separately, then assess stationary and moving precision plus independent mouse/Stop. Do not alter caps or infer permission from remaining milliseconds.

Continue PR9 open/draft/unmerged on the same branch. Forbidden metadata updates were not retried. Exact source/CI, launcher preflight, desktop restoration and final binary hashes are in final delivery receipt and report addendum. This execution delivers a measured copy-cost reduction and a reversible capture candidate; full physical performance recovery remains open.

Sources: [Source Reader processing](https://learn.microsoft.com/en-us/windows/win32/medfound/processing-media-data-with-the-source-reader), [reader attributes](https://learn.microsoft.com/en-us/windows/win32/medfound/source-reader-attributes), [advanced processing](https://learn.microsoft.com/en-us/windows/win32/medfound/mf-source-reader-enable-advanced-video-processing), [exposure priority](https://learn.microsoft.com/en-us/windows-hardware/drivers/stream/ksproperty-cameracontrol-auto-exposure-priority).
