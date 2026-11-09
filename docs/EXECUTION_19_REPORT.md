# Execution19 — Precision Ghost

Status: **PARTIAL — controlled precision preview implemented; physical qualification BLOCKED.** This is not B1 acceptance, a release, cold fingertip detection or B2 actuation.

## A. Repository baseline and preserved state

Recovered clean PR7 branch at769bf27cf9cb655b5f3077716c29c9024a682575; its Windows run42583541264 passed. Fetched origin and created codex/execution-19-precision-ghost from that complete stack. No resets, cleans, parent merges or history rewrites. The final camera ledger differed from the earlier handoff: ten released rows total809686ms; all five allowed Execution18 starts used. Original start count5, version4 and every charge remain unchanged. No camera was opened during this execution.

## B. Existing pointer behavior

The existing timestamp-aware One Euro filter used minimum1.5Hz, beta3, derivative1Hz. Four observations stabilize preview; sparse v2 points preserve available model geometry but cannot recover a point the detector no longer emits. A worker movement epoch reset could bypass an outlier gate; pointer continuity now retains movement while readiness/gesture continuity still breaks.

## C. Baseline jitter and latency

Same generated50Hz signals: stationary normalized RMS0.000748122 for original versus0.000549115 for candidate, a26.6% reduction. At an illustrative1920-pixel width that is1.436 versus1.054px; these are derived values, not measured screen accuracy. Raw input RMS0.008. Ninety-percent step response80→40ms, but initial response0→20ms because the large-step gate waits one frame. Step integrated RMS increases0.01440→0.02164; this tradeoff is explicit.

## D. Candidate comparisons

First candidate min0.7/beta10 improved jitter/rapid motion but worsened slow error0.00246→0.00400. It was not promoted in that form. A bounded motion estimator restores responsiveness. Baseline, first candidate and final comparison logs remain under artifacts/execution-19. No multiple low-pass cascade or prediction was added.

## E. Chosen mathematical behavior

One Euro retains the original exponential alpha=1/(1+1/(2*pi*cutoff*dt)); cutoff=min+10*filtered-speed, derivative cutoff1Hz. Minimum is0.7Hz near rest and2Hz during coherent motion. Ten recent timestamped points estimate least-squares velocity and residual noise. Motion is coherent when fitted displacement exceeds twice residual RMS plus0.001. Four-observation hysteresis avoids single-frame mode toggles. The same filter state continues through transitions.

## F. Motion transitions and spikes

Resting/Moving only; no unnecessary quality-mode menu or precision dead zone. A jump>0.08 normalized waits for a subsequent point within0.04 of the candidate. A continuing unresolved jump hides the pointer after80ms. Loss, stale data, hand/session/frame/settings changes reset preview eligibility. A tiny sustained0.003 adjustment remains reachable. The old filter and no visual fallback remain available through INVEY_LEGACY_POINTER=1.

## G. Visual fingertip implementation

NumPy bounded17x17 grayscale patch correspondence, anchored to a full-model index after four full observations. Processing images are at most640px wide. Both whole patch and inner core need standard deviation≥8. Search is±12px; RMS photometric error≤12, competing-match separation≥4, no search-border match, reverse correspondence≤1.5px. The fixed anchor template does not adapt onto a new object. No new production dependency, model or framework was installed.

## H. Arbitration

Single-hand full and sparse index observations have priority. Only a full observation renews the600ms visual anchor. Ambiguity, hand change, invalid geometry, frame gaps/order, missing texture and mismatching appearance invalidate it. A returning model index more than16 processing pixels from the visual track is hidden and requires reacquisition. Close returns pass through the persistent stabilizer. The host requires an already observed full-hand owner and matching frame geometry before displaying visual continuation.

## I. Continuation and drift limits

Two generated textured fingertip sequences/seeds each supported15 moving observations over495ms with floating-point-roundoff target error. Full-hand reacquisition took one observation in that controlled route. Appearance, disappearance, background-only motion, rotation, rapid displacement, duplicate correspondence, wrong hand and timing controls reject. This is not anatomical verification: a visually indistinguishable object could still match within the short lifetime. Real skin texture, occlusions, crossings, natural scale/rotation and physical two-hand detector recall remain unqualified.

## J. Observed versus predicted

No predicted coordinates are emitted. V3 VisualIndex carries current-frame coordinate, full-anchor timestamp and photometric residual; inherited sequence/capture/result/frame/hand fields retain correlation. It is not a model landmark: Point(8) stays null, Tracked/HasModelPoints stay false, and action eligibility stays false. No frozen or extrapolated marker counts as observed continuation.

## K. Cold-start investigation

Re-ran existing checksum-qualified public-still crop benchmark. IMAGE cold acquisition at thresholds0.5 and0.3 found0/24 each. VIDEO found6 full warmup frames and0/18 subsequent crops at both thresholds. Crop lacks independent fingertip annotations, so this is not a physical finger-only dataset. Median inference32.1/58.0ms for the two cold settings in this run. Existing OpenCV LK followed four translated synthetic corners (~4.00001px); its60.5ms cold call is not an optimized throughput comparison. Dedicated cold detection remains an open research requirement; no new anatomical detector or synthetic-trained model is claimed. Texture/template acquisition alone is unsuitable because it cannot distinguish a finger from an object without a full-hand anchor.

## L. Frame correctness

Continuation consumes the exact RGB image passed to the synchronous VIDEO call, after that call returns, with the same source sequence/capture timestamp. Native sequence reordering now rejects before inference even when timestamps increase. Existing latest-frame capture and bounded two-frame preview match remain unchanged. V1/v2 readers/sources remain supported; v3 strictly forbids mixing visual evidence with full/sparse geometry. No image is included in JSON, diagnostics or disk output.

## M. Personal-learning compatibility

Learning retains its original separate pipeline: fresh raw full index → calibrated base mapping → supervised labels/accepted correction → personal comparison canvas. It does not train on temporally filtered points. The precision preview uses validated full/sparse/visual coordinate → image-space stabilizer → existing display mapping. Personal correction remains in its existing learning preview; a unified post-personal-correction precision channel is not claimed. Visual fallback never supplies a supervised label. Persistence, rollback/reset and guided controls are preserved.

## N. Gesture invariants

Visual source is a Partial host state solely to disarm existing readiness/practice; its distinct PointerState is FingerVisual. Full observation loss cancels held practice. All physical cursor, OS input and logical ownership boundaries remain unchanged. No production SendInput/SetCursorPos or external actuation was added.

## O. Deterministic signals

Compared raw/original/candidate on stationary, slow, rapid, small adjustment, step, curve, reversal, spike, irregular intervals and dropout. Final candidate RMS: slow0.001746 versus0.002463; rapid0.005395 versus0.010932; small adjustment0.000605 versus0.000653; curve0.011071 versus0.023043; reversal0.005304 versus0.008525. Initial-step penalty is disclosed above. JSON also records standard deviation, peak-to-peak, p95, step50/90, overshoot, CPU and trajectory/reversal metrics. Generated signals do not prove physiological jitter or optical end-to-end latency.

## P. Image/resource benchmarks

Evidence classes are SYNTHETIC_SIGNAL, SYNTHETIC_IMAGE_SEQUENCE and transformed public still. No new RECORDED_VIDEO of a physical finger was collected. A620-frame generated-image run (600 timed) yielded496 visual frames, median/p95 visual-stage2.04/2.59ms and420.6 loop frames/s without MediaPipe/capture/WPF. CPU1.422s for1.426s wall time. Retained image arrays308356 bytes; Python traced retained delta349366 bytes/peak3742990 includes a live tracker and measurements. This is not a webcam-FPS, OS-memory, thermal or leak-free claim. Real acquisition/inference/render/end-to-end stage qualification is deferred.

## Q. Real camera

BLOCKED by exhausted existing allowance; zero Execution19 camera starts. The ledger was read, never changed. Historical camera measurements and release reviews remain historical.

## R. Human validation

NOT RUN. Guide contains a prepared brief stationary → motion → palm-out → full-return → removal → Stop sequence for a future separately authorized user-enabled test. Cold acquisition and physical learning accuracy remain separate.

## S. Builds/core

Locked restore and final Debug/Release builds passed without compiler warnings/errors;132 core tests pass per configuration. Baseline Debug had124 passes/one known migration test failure (IOException0x80070005); baseline Release125 passed. Final results do not erase the baseline failure. Its Windows file-access root cause remains unconfirmed; migration safely preserved the original profile and its behavior was not weakened.

## T. Worker tests

Baseline20 passed; expanded29 passed, no skips, including native Media Foundation reading of the existing generated MP4. New image controls exercise both seeds, expiry, disappearance, ambiguity, hand switch, conflict, frame order/geometry, small illumination change and Stop. No camera activation is involved.

## U. WPF integration

Initial route ba760e5f failed because calibration was opened before Start reset it. The fixture now opens the real panel after Observing; production requirements were not weakened. Focused run05daac2c passes image-based movement, no invented model points/action authority, return, loss/ambiguity, restart and Stop during fallback, alongside the old sparse-preview route and cleanup. Ordinary UI shows simple source/loss feedback; orange image dot distinguishes visual evidence. Worker rejection counts and bounded timing metrics remain diagnostics.

## V. Pocket and Dual regression

Full current Windows regression and seeded learning checks are recorded in the delivery continuation below. No Pocket, Dual, shortcut, profile or library implementation was removed or rewritten.

## W. Cleanup and guardian

Focused isolated tests verified worker exit and normal desktop restoration. All camera charges and limits remain intact; no new permission or refund. The verified default app remains camera-off on launch. Final process/desktop audit is recorded at delivery.

## X. Failures retained

Preserved baseline migration failure; first filter's slow-motion regression; visual-first.log accepting other-texture residual17.63 (appearance gate tightened18→12 and mild illumination acceptance tested); initial compile CS0136 name collision (renamed local); initial WPF setup-order failure. No failed test was skipped or hidden. MediaPipe's existing feedback-manager warnings remain in cold-and-flow.log.

## Y. GitHub and binary linkage

Focused draft PR targets codex/execution-18-personalized-ghost-learning. The workflow now includes that parent branch. Exact final head, CI run and tested DLL hash are recorded at delivery; hosted CI cannot establish physical tracking quality. Parent PRs are not merged.

## Z. Recommendation and next milestone

Review the bounded experimental preview as PARTIAL, with controlled gains and explicit physical gaps. Warm continuation is short-lived, not sustained wrist-independent tracking. Next milestone is a newly authorized structured hardware qualification plus annotated natural-image false-correspondence evaluation. Do not claim physical finger-only PASS, cold-acquisition PASS, full B1 acceptance or OS gesture control.

Sources consulted2026-11-09: [original One Euro algorithm/tuning](https://gery.casiez.net/1euro/), [OpenCV optical flow assumptions](https://docs.opencv.org/4.13.0/d4/dee/tutorial_optical_flow.html), [template correspondence](https://docs.opencv.org/4.13.0/d4/dc6/tutorial_py_template_matching.html). The conservative thresholds are implementation choices, not calibrated anatomical probabilities.

## Delivery verification continuation

Full native standard run20261109T101753949-5972d4d5d1bf410aa3e0d3a866bdc0da:39 PASS, zero FAIL, one Explorer-only BLOCKED. This covers Pocket capture/save/restart, pinned references, shelf, shortcuts, Workspace/Dual ownership, cursor controls, recorded inference, calibration/practice and visual continuation. Two actual-WPF learning/restart/rollback runs pass with seeds73021 and99174: baed930a879840ab93a32bf44af18a63 and29b1dabbefd5429187d328a7f684585f. Supplemental demo-control seed99174 run826ddd5737e048f2b493f3cc98428e9b also passes. These supplemental control routes are not reclassified as native input tests.

Final review found repeated UI reads could retain an otherwise fresh visual sample beyond its600ms anchor deadline. Expiry is now checked before the same-sequence cache return; a dedicated test covers that boundary. This does not extend or weaken any lifetime. Both configurations now pass132 core tests. The full native run above precedes this narrow final expiry guard; final committed-binary checks are recorded separately.

The signal benchmark additionally estimates trajectory lag by minimum squared alignment on the same generated trajectories: slow100→60ms, rapid60→20ms, curve60→20ms; reversal80→40ms. These are discrete20ms-grid signal estimates, not camera-to-screen latency.

The research-sync script was refused by the shell signing policy; the reviewed script's sole copy operation was performed directly to keep the canonical document and root mirror identical. No system execution policy was changed.
