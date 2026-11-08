# Execution 16 — engineering evidence

Status: CHANGES REQUIRED for complete continuation acceptance. The implemented foundation is published as draft PR #5 for code review. No release acceptance or merge is authorized by this report.

## A. Recovery

Recovered the uncommitted Execution 16 branch from PR4 head ca400d966ec87e0c8ec638803fc8d5afa801bd5b. Recovery ref refs/checkpoints/execution-16-start is preserved. No reset, clean, parent merge or discarded work. The prior physical Escape stopped desktop control with the camera already off.

## B. Preserved improvements

Durable independent camera guardian; sparse v2 partial observations alongside complete v1; per-finger validity; adaptive index preview; safe partial/action separation; 200 deterministic negative opportunities; recorded-source WPF verification; original product features.

## C. Revised authorization

The continuation explicitly authorizes 12 total sessions, up to 180 seconds per new session and 1800000 ms total, including earlier charges. Version 1 retains six/120000/720000. Explicit version-2 migration records amendment execution-16-continuation-2026-11-08 without replacing prior entries. It refuses unconfirmed reservations. A local pre-amendment copy is retained. At amendment, three released entries totaled 217765 ms (the third was the first annotated-preview check, still run under the stricter original guard).

## D. Independent supervisor

A separate process reserves before start, checks pipe/worker identity, owns a kill-on-close job, and enforces a monotonic deadline independently of the WPF dispatcher. The amended upper deadline is 178 seconds, leaving margin under 180. Missing/corrupt configuration, unfinished reservations, parent/guardian loss and unclean shutdown fail closed. Independent synthetic workers verified six legacy permits and denied seventh, twelve amended permits and denied thirteenth, early release, hard timeout, guardian termination and startup cancellation. Evidence: 20261108T111052695-07d251ac387e4011affa096042a8c167 (local artifacts/automated-acceptance).

## E. Live preview

Optional native WPF RGB preview, off on startup. The user confirmed: “Image and dots are aligned.” The agent observed accessibility metadata only while camera pixels were visible; no webcam screenshots were taken. Stop clears the image.

## F. Transport and ownership

Separate random current-user named pipe, verified worker PID, maximum 640×360 RGB24 payload, strict monotonic sequence and dimensions, at most two transient host frames for exact matching and one replaceable pending writer frame. No frames enter text protocol, logs or disk. Frames older than 500 ms since receipt disappear. Frozen bitmaps are currently allocated per new frame; controls and joints are reused. This has bounded retained storage, but sustained allocation/GC qualification remains open.

## G. Finger overlay

Clean image, fingertips-only or full 21-landmark mode. Five anatomical fingertip indices and thin valid-point connections. Missing sparse points are hidden, not extrapolated. Index is also available through the separately gated experimental logical pointer. Matching frame/observation sequence is required before drawing landmarks.

## H. Mirroring

Historical user observations: physical right moved marker left with mirror on, and right with mirror off. The original setting affected both mapping and view. Continuation separates camera-image mirror from horizontal pointer mapping. Image and skeleton share one transform. Final post-separation physical direction acceptance remains open.

## I. Native formats

Actual ASUS enumeration exposed one mode: 1920×1080, YUY2, nominal 30 Hz. Selected output is FHD RGB32, stride 7680. No 60 Hz mode was exposed. No unsupported format or Windows scaling was forced.

## J. Capture-only baseline

Not yet measured without inference. The no-hand combined trial below is not a capture-only trial.

## K. Preview-only benchmark

Not yet measured without inference. The original lab used an 80 ms dispatcher timer, limiting normal preview updates to 12.5/s. Continuation fixes active-preview polling to 16 ms. Timer frequency is not reported as camera FPS.

## L. Inference-only benchmark

Existing synthetic MediaPipe crop measurements are in FINGER_TRACKING_RESEARCH.md. Separate physical inference-only comparison remains open.

## M. Combined pipeline

Initial annotated preview session f8988c6e-d9d8-461e-b904-725e156c58c8: 1311 accepted results, 507 hand /137 partial /3 ambiguous /664 empty; partial index in frame134. User confirmed aligned image/dots. Source-to-result median/p95 75.161/106.869 ms; inference20.355/40.209 ms; preview-copy1.058/1.715 ms. Worker CPU62.594 seconds over63.580 elapsed seconds.

Decoupled capture trial bdbf3b48-7dbe-4ec2-b9da-a2c4848cc91d: 697 unique captures and completed inference calls; approximately29.6/s over23.495 seconds between first and last frames. No pending-frame replacements recorded. Median/p95 source-to-result41.004/50.850 ms; inference12.337/16.627 ms. CPU27.953 seconds over25.855 elapsed seconds. All accepted observations were empty: no physical hand-accuracy claim follows. This trial preceded the display-timer repair.

Final mixed-hand trial c36804d9-b981-4a6a-81bb-2a8b6973fd47 on implementation455abf4:1812 unique captured frames,1750 completed inference calls,1749 accepted observations (458 full/443 partial/3 ambiguous/845 empty),424 partial index-in-frame samples. First/last native frame1504.082/62656.599 ms. Interval rates use count minus one:29.61 captured frames/s and28.60 completed calls/s. Sixty-two pending frames were superseded; maximum capture pending depth1. UI-reported unique preview-frame submission averages were sampled at25.3 and25.6/s after the16 ms refresh repair. This is not an independent scanout measurement or a continuously sampled whole-session preview distribution. Source-to-result median/p95 56.119/82.007 ms; inference21.778/28.037 ms; RGB/image8.967/11.661 ms; native copy2.249/3.099 ms. Timing distributions use the bounded prefix described below.

These workloads differ; lower inference time is not attributed solely to architecture. Source timestamps are driver-clock estimates, not independent optical motion-to-photon measurements.

## N. Native conversion

Safe RGB32/stride path retained. Decoupling eliminates mandatory camera-read waits on inference while retaining the original validated conversion. YUY2/NV12/MJPEG direct-conversion comparison remains unmeasured. Official Microsoft Source Reader guidance identifies software color conversion as a candidate bottleneck, not a reproduced defect by itself.

## O. VIDEO versus LIVE_STREAM

VIDEO remains the qualified default. LIVE_STREAM's busy-input dropping, callback correlation and disposal require separate validation; no unverified asynchronous backend switch was made.

## P. 30 FPS

About29.6 unique capture and inference calls/s measured on a no-hand combined workload. The final mixed-hand trial reached29.61 capture and28.60 inference calls/s, with UI-reported preview25.6/s. The30 FPS preview target remains unmet. No rounded-up30 FPS PASS.

## Q. 60 FPS

No 60 Hz mode exposed by this device enumeration. Synthetic30/60 Hz sources verify bounded replacement under a slow consumer; they do not establish camera capability.

## R. Partial hand

Earlier live sessions had70 and201 partial observations, with69 and185 index-in-frame results. Annotated preview had137 partial,134 with index. The user subsequently reported that the index dot disappeared when the wrist left the image. Treat that physical trial as failed continuation, despite the retained sparse samples. Model loss and cross-channel display timing are distinct potential contributors; this observation alone does not establish their individual causes. These model outputs establish retained partial geometry, not independent visibility ground truth or isolated-finger cold acquisition. Per-finger confidence is not invented.

## S. Pinch safety

Partial preview grants no pinch authority. Existing full-hand practice requires readiness and cancels on recognized loss, ambiguity, Stop or replacement. Physical two-hand detector recall and physical false-positive/negative rate remain unmeasured. Practice-complete text observed during the annotated session is not a ground-truth accuracy dataset.

## T. Jitter and latency

Synthetic One Euro results: normalized RMS .000748172 versus raw .008; 90% step80 ms versus fixed1.5 Hz260 ms. Prior live sessions involved motion and pinch, so their aggregate spread is not stationary jitter. Physical stationary and reversal trials remain open.

## U. Resources

Annotated-session worker working set at cleanup211251200 bytes; cleanup487.661 ms. Independent capture uses one source on its owning MTA thread and one latest pending frame; no native source crosses threads. Synthetic tests verify superseding, monotonic identities, source-open failure, owner-thread release and cancellation. In the final trial, a25.09-second independent process sample increased CPU time34.0625 seconds (about1.36 CPU cores), working set213835776 ->218271744 bytes, private bytes753860608 ->758059008, handles675 ->679 and threads46 ->47. Worker cleanup working set205885440 bytes and cleanup484.883 ms. This short change does not establish a leak. Sustained handles, thermal load and leak freedom require longer controlled measurement.

## V. Actual ledger

1. 900c6717-6185-48c9-ad22-94ff4e733994:81609 ms, released.
2. e3341955-746b-41a3-b712-3c5f9ab262d6:72375 ms, released.
3. f8988c6e-d9d8-461e-b904-725e156c58c8:63781 ms, released.
4. bdbf3b48-7dbe-4ec2-b9da-a2c4848cc91d:26047 ms, released.

5. c36804d9-b981-4a6a-81bb-2a8b6973fd47:63187 ms, released.

Total307999 ms charged; five of twelve starts. The ignored durable ledger remains authoritative for subsequent sessions. No quota reset occurred.

## W. Shutdown

All five reported sessions have nativeReleaseConfirmed=true, independent worker exit and released durable reservation. The third/fourth explicit Stops cleared UI images. Guardian refuses to refund unconfirmed termination. No device activation occurs on ordinary startup.

## X. Failures and repairs

Preserved earlier failed WPF runs: sparse snapshot indexing; private-desktop foreground focus; nonfinite Canvas.Left JSON; missing-index predicate mistaken for stabilization. Preserved setup failures: global .NET home permissions, obsolete solution filename and shell parameter forwarding. New preview-control compile failed due duplicated local protocol variable; repaired and rerun passed. Worker TEMP access failed in the sandbox; rerun used workspace TEMP/TMP, retaining the failure log. No failed test was removed or converted to a skip.

The80 ms UI timer was a confirmed display bottleneck. It was repaired to16 ms only during active preview; no physical speed claim is made before retesting.

## Y. Debug and Release

Current Debug and Release builds passed with zero warnings/errors. Each configuration passed100/100 core tests. Evidence: final-debug.trx and final-release.trx under artifacts/execution-16.

## Z. Python

20/20 tests pass, including native Media Foundation decoding/reopen using the existing generated color MP4. Earlier runs without its environment variable retained the existing conditional skip; the final native run did not skip it. Evidence: artifacts/execution-16/continuation-worker-native.log.

## AA. WPF and isolated Windows

Frame-matched synthetic RGB/skeleton, seven malformed-header controls, pending-connect cancellation, moving sparse index marker, missing-index suppression, ambiguity/rearm and Stop cleanup passed. Evidence: 20261108T110406931-b645dab5eaf64ca8af07c458c98ffa7d and later focused reruns. Final full standard run20261108T113109452-a3d7193f437347a5b9f6c18c7f92f55e passed37 cases with zero failures and one blocked Explorer case. It includes native cursor controls, Pocket capture/restart, shelf/shortcuts, Workspace, provider containment, calibration and worker exit.

## AB. Pocket/Dual and cursor

Prior focused preview runs confirmed no generated native cursor movement. Full Pocket, shortcuts, Workspace, containment and cleanup checks passed in the final standard regression, including independent worker exit while calibration was open. Physical simultaneous-input and Explorer gaps remain separate.

## AC. GitHub

Recovered branch codex/execution-16-precision-finger-tracking; no parent merged. [Draft PR #5](https://github.com/yayatidattayt/Invey/pull/5) is stacked on PR4. Implementation commit455abf491e8e931b6f77fe47f12c0d502656f741 passed [Windows verification42476414070](https://github.com/yayatidattayt/Invey/actions/runs/42476414070). The final documentation-only review head is independently checked on [PR checks](https://github.com/yayatidattayt/Invey/pull/5/checks); final delivery reports its exact SHA. Local checks do not substitute for hosted CI, and hosted CI cannot prove physical camera performance.

## AD. Remaining limitations and recommendation

CHANGES REQUIRED for the complete continuation acceptance: 30 FPS annotated preview, separate capture/preview/inference comparisons, real hand accuracy/jitter/selected-hand/two-hand trials, cold isolated-finger acquisition, VIDEO/LIVE_STREAM and native-conversion comparisons, meaningful selectable performance profiles, complete live stage metrics, broader physical acceptance. Current core/worker/full standard regression and implementation CI are clean. Gate A, M02, full B1 and Gates C/D remain open. No B2 actuation or external-app gesture control was added.

## AE. Next milestone

Complete the remaining measured performance comparisons under the same amended ledger, preserve useful live preview, then move draft PR5 toward completion after review. Physical acceptance remains evidence-driven.

### Additional regression attempts

Full run 20261108T111609122-63514e08ab604f31907e137745295f84 retained the earlier fixture's mirrored mapping. The moving-marker predicate expected increasing X and timed out; its open lab caused downstream Pocket/exit failures. The synthetic fixture now chooses an explicit mapping and a finally block always stops/closes it. Run 20261108T112109517-e43d3c448c3c4f999f5f56ef36f0dfbc failed at initial foreground-focus setup before exercising the scenario. That environmental failure is preserved; no product defect is inferred from it. A further full rerun is active.

Timing distributions are bounded to the first1500 samples; long-session p95 values therefore describe the sampled prefix, not an unlimited whole-session trace. Camera interval rates use count minus one between first and last capture timestamps.


Run 20261108T112234768-6d4f6458e8d3495d8ee638b58f363c44 failed an unattributed cursor-change assertion and Pocket crop endpoint check; neither is silently reclassified as PASS. Run 20261108T112635467-db5d3c4232304cbb8922d12704fd1f14 passed pointer assertions but disconnected during repeated test cleanup: tracking-stop dereferenced a lab already closed by tracking-close. Null-safe inactive Stop repairs that fixture path. The final focused frame/marker/cleanup run 20261108T113030615-3d375997f7c944f6a95259fb3f8e425d passed all three cases. A fresh complete standard rerun follows the repair.

The final transport retains at most two host image frames and selects an exact frame match when available; latest-completed-image mode may show newer imagery with its unmatched skeleton hidden. No separate low-latency pre-inference video branch is claimed. The retained native source remains one per session.
