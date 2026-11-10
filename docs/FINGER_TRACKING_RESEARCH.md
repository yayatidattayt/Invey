# Finger tracking research — Execution 16

Retrieved 2026-11-08. Findings are provisional and distinguish controlled sources from physical observations.

## A. Research question

Can Invey preserve an observed index when a model-generated wrist leaves the image, without granting uncertain geometry authority to interact? A separate question is whether a palm-dependent detector can acquire an isolated finger from a fresh start.

## B. Existing backend

The qualified Python 3.12.10 / MediaPipe 0.10.32 CPU VIDEO backend remains the default. Existing model SHA256: fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1. No replacement weights were downloaded.

## C. Partial-hand failure

The old normalization rejected all 21 points when any point was outside the image. The new v2 sparse observation retains finite in-frame model points and represents missing geometry with null. Complete v1 remains supported. A finite model point is a prediction from the model, not independently verified physical visibility or a per-finger confidence measurement.

## D. Finger states

Thumb 1–4, index 5–8, middle 9–12, ring 13–16, little 17–20. State distinguishes observed, partial, unavailable and ambiguous. Predicted is reserved and never emitted by this implementation. No extrapolated geometry authorizes pinch.

## E. Cold acquisition experiment

The existing attributed still was cropped in memory. This is not a labeled isolated-fingertip dataset. At detection thresholds .5 and .3, cold IMAGE produced zero crop detections in 24 attempts each. VIDEO produced six full-image warmup detections and no detections on the subsequent 18 crops at either threshold. Lowering the threshold did not help. A physical isolated-fingertip cold start is still unresolved.

## F. Temporal continuation

Sparse valid index points support preview after full-hand acquisition. Four fresh observations rearm the preview. Missing index, stale data, session replacement, ambiguity and recognized loss reset it. This does not establish independent finger-only acquisition.

## G. Alternative landmarks

MMPose provides hand-keypoint models and training infrastructure; it requires separate model selection, packaging, dataset evaluation and weight-license review. No MMPose runtime or weights were installed or benchmarked here. It is a future research candidate, not a verified improvement. [Repository](https://github.com/open-mmlab/mmpose), [hand model zoo](https://mmpose.readthedocs.io/en/latest/model_zoo/hand_2d_keypoint.html), [code license](https://github.com/open-mmlab/mmpose/blob/main/LICENSE).

## H. Optical flow

Existing OpenCV 5.0.0 tracked four synthetic corners translated four pixels: mean horizontal displacement 4.000011 px; one cold call took 26.9 ms. This is only a controlled continuity experiment. Flow neither establishes anatomy nor acquires a cold fingertip. No production flow fallback was added. [Official optical-flow documentation](https://docs.opencv.org/4.x/d4/dee/tutorial_optical_flow.html).

## I. Smoothing

On deterministic normalized-coordinate samples, raw RMS .008, fixed 1.5 Hz RMS .000688948 and One Euro RMS .000748172. The 90% step response was 0, 260 and 80 ms respectively. One Euro processing 250 samples took about .02–.03 ms locally. This is synthetic filter response, not physical end-to-end latency or ground-truth accuracy. [Original One Euro work](https://gery.casiez.net/1euro/).

## J. Safety and ownership

Partial index preview never bypasses full-hand practice readiness. Existing pinch requires deliberate hold and cancels on recognized loss or ambiguity. Two hundred deterministic negative opportunities exercise cancellation and no-action behavior. These are not a measured physical false-positive rate or a guarantee that a second hand is always detected.

## K. CPU and latency

Controlled .5 IMAGE crop calls: median 10.37/p95 12.20 ms; VIDEO 11.12/20.96 ms. At .3: IMAGE 19.05/21.71 ms; VIDEO 19.98/28.20 ms. Workloads and detection outcomes differ; these figures do not justify switching thresholds. Local raw evidence: artifacts/execution-16/model-benchmark.json.

The first physical annotated-preview session accepted 1311 results over about 61 seconds of frame delivery; source-to-result median/p95 75.16/106.87 ms. A later no-hand decoupled-capture trial processed 697 frames at about 29.6 unique captured frames/s, median/p95 source-to-result 41.00/50.85 ms. No hand was detected in that later workload; it is not a comparable hand-tracking accuracy benchmark.

## L. Packaging and licensing

Reuse existing qualified assets and pinned dependencies. MMPose code licensing does not establish every candidate weight or training dataset's redistribution rights. The OpenCV experiment used an existing local package; it did not add a production dependency. Camera pixels remain in bounded local memory and are excluded from public evidence.

## M. Real webcam evaluation

User confirmed that real image and landmark dots align. Earlier sessions produced sparse index observations and a visible partial pointer. The user reported physical right -> marker left with mirror on, and right -> right with mirror off. Moving/pinch trials are not stationary-jitter measurements. The actual ASUS native mode enumeration in this environment exposed only FHD 1920×1080 YUY2 at 30 Hz; no 60 Hz mode was exposed.

## N. Recommendation

Keep the existing qualified MediaPipe backend, strict sparse protocol, adaptive preview filter and safe practice gating. Preserve independent acquisition and bounded frame replacement. Do not switch to LIVE_STREAM solely because it is asynchronous: its busy-frame dropping and late-callback ownership need separate measured verification. [Official MediaPipe guide](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python).

Microsoft warns that basic Source Reader software color conversion is not optimized for real-time rendering. Measure native conversion and validated alternatives before replacing the safe stride/orientation path. [Source Reader](https://learn.microsoft.com/en-us/windows/win32/medfound/source-reader).

## O. Open research

The user reported the index dot disappearing when the wrist left the image in the annotated trial. That trial did not establish reliable continuation. Physical isolated-finger cold acquisition, stable finger-motion accuracy, stationary jitter, selected-hand ownership, physical two-hand detection reliability, sustained thermal performance, native YUY2/NV12/MJPEG conversion alternatives, and VIDEO versus LIVE_STREAM comparison remain distinct evidence requirements. No 60 Hz capability or alternative-model improvement is claimed.

## Execution26 —2026-11-10
Execution26 adds warm optical support-feature tracking/arbitration with fixed-anchor drift/appearance/reverse checks. Public image motion errors improve on selected paired fixtures; rotation/large motion can still reject. Existing source same, cold detector unchanged,600ms lifetime kept. No new physical evidence and no gesture authority. No camera access or ledger amendment. See EXECUTION_26_REPORT and HYBRID_TRACKING_GUIDE.
