# Execution26 — Ghost stability

**Camera-free hybrid candidate; physical improvement UNVERIFIED. No camera opened or allowance amended.** Final exact published commit/CI, native outcome and cleanup are linked by the delivery receipt in `artifacts/execution-26/final-delivery.json` and the final response. The physical quality failures from Execution25 remain open.

## 1. Verified starting commit

Clean `codex/execution-20-interaction-engine` at `ab8d72bd4bb9c6ab69de1ebddaa5f88e79f28aa9`, existing open draft PR9. Remote refs fetched; exact-head CI42784037410 passed. No branch reset, parent merge, force push or forbidden PR-metadata workaround. Baseline pinned locked restore and Debug/Release184/184 core tests passed before implementation; zero build warnings/errors. Baseline39 workers passed using native MP4 and project-local TEMP. First restricted worker run failed six temp-file operations under the sandbox temp directory; preserved as baseline-worker.log, unchanged local-TEMP rerun passed.

## 2. Execution25 physical baseline

Full capture29.49/inference29.48/s,4256 host accepted observations,3052 logical positions. Creator confirmed ordinary mouse independence and smoother movement, but repeated acquisition/loss, persistent jitter and wrist-out freeze/disappearance.2295 good anchors,490 texture/warmup visits,638 partial visits,828 missing/expired-anchor visits,6 handedness resets and no successful reported image-only continuation. Intentional removals/setup are included; these are not false-negative probabilities. Metadata and original feedback are preserved. No physical images exist for retrospective pixel analysis.

## 3. Existing visual fallback analysis

Execution25 patch tracker retained unchanged in finger_visual.py, including its latest weak-patch anchor repair.17x17 central patch, variance gates, fixed12px search,4px uniqueness gap, absolute residual12, reverse check1.5px and600ms full anchor. Core v3 consumer also enforces600ms, freshness, selected hand, source/sequence and initial full-hand acquisition. Original model, capture thread, latest-frame handoff, protocol and shared desktop remain.

## 4. Supported causes

Generated low-texture center with usable surrounding finger/nail edges produces no legacy anchor, while the new local feature cloud can initialize. Fixed small search and central appearance can reject otherwise measurable motion. Existing physical metadata proves no producer visual output, so consumer-side filtering alone cannot explain that result. The known post-Execution25 anchor-erasure defect is already repaired in the baseline. The existing attributed Human Palm image visibly crops fingertips; it cannot independently validate anatomical index-tip location.

## 5. Uncertain causes

No saved webcam imagery exists to identify exact physical texture, blur, occlusion, search displacement, lighting, uniqueness or reverse-check failure.490 texture/warmup visits do not distinguish those subcauses. Handedness resets may contribute but cannot be called detector mistakes from these counts. New correspondence/initialization/arbitration/source counters separate future causes without saving pixels or landmark streams.

## 6. Engine A

Pinned MediaPipe CPU VIDEO, num_hands2, normalization and real full/partial geometry unchanged. Qualified model points pass through without averaging or synthetic joints. No threshold reduction, inference resizing, replacement model or alternative camera API. Model reliability is not proven merely by agreement with another tracker from the same image.

## 7. Engine B

hybrid_visual.py uses the existing local OpenCV5.0.0 package. Four full observations initialize up to12 locally supported corners within24px of the model index, with local color proximity and spatial-spread checks. These are support features, not anatomical fingertip detections. Sparse pyramidal Lucas–Kanade tracks actual pixels forward and backward; a similarity fit maps the original tip/support relationship. No predicted coordinate is emitted. At most640px on the largest image dimension; no unbounded queue/history. One source frame/sequence/timestamp feeds both engines.

## 8. Candidates

Baseline: unchanged Execution25 patch. Candidate A: initial pyramidal flow plus fixed-anchor validation; improved generated low-texture availability but first public-photo combined p95 regressed to48.93ms (preserved real-fingers-first.json). Candidate B: deterministic source arbitration tests for full/partial/visual, conflicts and reacquisition. Candidate C: optimized combined path, reusing identical flow work when the previous/full anchor is literally the same image and refreshing model-confirmed features without re-detecting corners every frame. Reused computation is explicitly not independent corroboration. No parallel camera/inference engines were added.

## 9. Selected implementation

Combined hybrid is selected when existing OpenCV is available. `INVEY_VISUAL_BASELINE=1` selects the preserved patch comparison. If optical import is unavailable, the worker emits an explicit warning and falls back to the existing patch; Pocket/startup do not fail. No local install was needed. CI pins the same existing opencv-contrib-python5.0.0.93 Windows wheel, SHA256461622db95c964652d4d8fda171034961c3de270f78a6095aaad31050771774a, matched against official PyPI metadata. The53.8MB library is not a downloaded model. Cold acquisition remains model-dependent.

## 10. Low texture

Two deterministic seeds and same images for both trackers. Generated low-texture, controlled lighting/blur, moving-background, tiny-motion and reversal cases: baseline0 visual outputs versus hybrid16/16 in the tested continuation interval; tested false continuations0. Generated point is an assigned reference, not a human annotation. A wholly featureless region remains Lost. High-texture baseline and hybrid both16/16; hybrid subpixel estimates can have small error where integer baseline happens to land exactly, so not every error metric improves.

## 11. Wrist-out image simulations

Current-image translated/isolated-index-region sequences exercise actual flow rather than fabricated coordinates. MediaPipe absence is deliberately injected after warm initialization. Removing the real-photo surroundings is a controlled mask, not anatomical ground truth for a wrist/palm. Visible-tip continuation works on the tested generated and public-still sequences; physical wrist-out remains FAILED from Execution25 and UNVERIFIED for this candidate.

## 12. Partial hand

Sparse model index remains authoritative and cannot initialize/renew a full identity anchor. Visual candidates contain only v3 index/provenance/anchor age/residual, no thumb or21-point geometry. Missing index with valid recent visual correspondence can support preview; both missing return Lost. Partial geometry never authorizes a gesture or a personal-learning label.

## 13. Identity continuity

Side changes, ambiguous/two-hand model output, source/frame replacement, duplicate/out-of-order sequence, frame gaps and expired anchor cancel visual state. Cold visual acquisition is impossible in this implementation. Local appearance is not a biometric identity proof; similar-looking objects and unrecognized physical second hands remain limitations. No unknown hand receives action ownership; live actions have no issuer.

## 14. Source arbitration

Both valid and agreeing: preserve original model geometry. Model valid/flow invalid: model route, attempt safe visual initialization. Model absent/flow valid: v3 image-only preview. Both absent: Lost. Returning model must reconcile spatially with the independently measured visual point; no averaging. Core handoff regression confirms valid full→visual→full does not repeat preview acquisition, while ActionEligible remains false for every visual finger and Stop clears it. Four-observation core/gesture rules were not shortened.

## 15. Conflict

Both candidates valid but more than8px apart cause SourceConflict/reacquisition, even before a visual-only phase. After visual fallback, missing visual validation also prevents a returning model from silently jumping the pointer. A fresh model can reacquire after cancellation through the existing acquisition path. Conflict tests explicitly reject rather than average competing coordinates.

## 16. Drift rejection

At least4 consistent features,75% valid matches, reverse error<0.75px, local LK error<18, spread/aperture check, similarity residual<=1.5px, bounded step40px, per-step scale0.90–1.10 and anchor scale0.75–1.33. Fixed-anchor correspondence must agree within2px with incremental motion. Rectified current tip-patch residual<=12 and brightness difference<=30; nearby competing template matches are rejected. Scores are heuristics, not calibrated probabilities. Tip occlusion is tested while shaft pixels remain. Some rotation/scale and fast sequences conservatively lose tracking; those failures are retained, not tuned into false positives.

## 17. Anchor lifetime

Production stays600ms from full identity anchor, independently of recent valid visual frames. Offline1200ms ablation produced36 valid generated frames versus18 at600ms, rejected a later removal, but18 outputs exceed the current v3 protocol deadline and are intentionally NOT integrated. This short synthetic result does not qualify identity/drift over longer real finger-only motion. No weak or partial observation renews full identity. Existing600ms behavior remains available and no protocol limit was silently changed.

## 18. Reacquisition

Actual image tests cover returning coherent model and conflicting model, true removal/reentry, hand side/ambiguity, Stop and geometry changes. True loss clears the anchor; four new full model observations are required for optical reinitialization. Valid source handoff preserves preview continuity without granting new gesture authority. Rapid/rotation rejection can still create a blackout; not claimed solved.

## 19. Disappearance reasons

Bounded metadata separates feature initialization, correspondence rejection, source arbitration and selected source, alongside existing host loss/partial/ambiguity/gap counters and UI mapper entries/durations. Primary UI says Tracking hand, Tracking visible finger, Reacquiring or Tracking lost; no competing cursor. Advanced metadata retains reasons such as no-anchor, aperture, forward-backward, drift, appearance, nonunique, edge, source-conflict and expiry. Producer events are not confused with UI polling counts.

## 20. Jitter/error/latency comparison

Public-domain Fingers.jpg by CT Nehls (Wikimedia) has visible fingertips. Initial model estimate is not anatomy ground truth; only generated translation is exact. Same16 translated/isolated-region frames: both trackers16 valid, baseline max relative error0.9786px, hybrid0.0457px; removed-tip outputs0 for both. The original cropped-palm photo remains a negative result,0 outputs for both. Generated stationary-noise hybrid RMS0.00188px, tiny-motion0.00630px and reversal0.12855px in the recorded seed26 run; these are image-space synthetic metrics, not physical screen jitter. One existing final precision filter remains, parameters unchanged; no double smoothing or50ms interpolation. No60Hz claim from30Hz hardware.

## 21. CPU and memory

After the preserved expensive first candidate, optimized public-photo repeats: baseline combined model/visual median21.1069/20.9919ms, p9522.2852/21.4979ms; hybrid median19.6020/19.3614ms, p9520.9875/20.4313ms. Earlier optimized trial20.801/24.050ms remains too.40/40 full detections in each combined run. Python allocation peak baseline4917332B/hybrid1745771B; distinct retained NumPy arrays1229956B/308641B. OpenCV native allocations/library mappings are not measured by tracemalloc; do not call these whole-process RSS. Image-only flow timings are a few milliseconds on these inputs; cold first calls were slower and are not hidden.

## 22. Nominal30Hz budget

Paired warmed processing fits33.3ms on the tested still workloads, without accumulating frames. Inference, optical correspondence, feature refresh, arbitration, total visual work and capture-to-result age have separate bounded metrics. Actual ASUS throughput under hybrid remains UNVERIFIED; synthetic unpaced throughput cannot certify30Hz physical tracking or thermal performance. Source Reader/copy/latest-frame architecture remains unchanged.

## 23. Product integration

Generated images pass through an actual worker and same protocol pipe to the existing selected-hand/calibration/personalized mapper, one precision filter, one passive desktop Ghost. Private-desktop WPF test observes real local preview and image-only packet moving that shared pointer; it asserts Recorded source and denied Arm. Pocket/card/Workspace routes remain actual implementations. First integration run passed optical checks but later failed because the new fixture left Restart's mnemonic where the old route expected Enable. Fixture now restores its prior UI mode/image state; failed run retained. Corrected seeded product route passes.

## 24. No gesture authority

No change to LiveQualified/MayExecute, controlled replay issuer, full-hand pinch geometry, target ownership or cancellation. V3 contains no invented thumb. Camera and recorded image-only preview cannot Arm Pocket. No OS input injection or native cursor movement in tracking code. All automated native input remains in the existing guarded private-desktop test runner.

## 25. Core tests

Baseline184/configuration; candidate185/185 in both Debug and Release, no skips. Both build configurations pass with zero warnings/errors. New full-to-visual-to-full regression preserves warmup, denies visual action eligibility and clears on Stop. Existing selected-hand, calibration, pointer delivery, gesture authority and reducer invariants remain covered.

## 26. Worker tests

Baseline39 workers; final delivery run50/50, no skips (artifacts/execution-26/delivery-worker.log). Existing native MP4/color fixtures and original patch negatives remain. Added low-texture, optical flow, identity, sparse-partial advancement, source conflict, drift, occlusion, anchor expiry and explicit missing-backend fallback checks. Two deterministic seeds are paired against unchanged baseline pixels. No physical-camera acquisition.

## 27. Native tests

Typing-only26003 PASS; full26005=40PASS/0FAIL/1Explorer BLOCKED. Final selector/product26006 PASS after the small selector addition, including actual worker/image/protocol/shared pointer, denied actions and cleanup. Labels-off30/60Hz delivery, guided practice, Pocket and Dual remain in the actual isolated product route. Exact run IDs and chronology are preserved below. Environment audit reports Default; independent process inspection finds no Invey-owned workers. Both camera-off launcher preflights pass. Explorer and physical hardware acceptance are not manufactured.

Execution25 focus failure is not ignored: read-only pre-injection HWND/PID/owner/focus snapshot is now saved on failure, preserving the exact existing guard. A bounded typing-only fresh-desktop run26003 passed typing/cursor/cleanup. Earlier failure remains; no root-cause attribution to GameInput/service or harmlessness without evidence. No service killed, focus assertion weakened or ordinary desktop targeted.

## 28. Preserved failures

Initial restricted baseline worker run failed six temporary-file operations; unchanged local-TEMP rerun passed. Original cropped-photo negative, rotation/coherence losses and first expensive flow benchmark remain. Native26001 passed optical checks but later failed because the fixture left Restart instead of Enable; restored UI mode/mnemonic then product26002 passed. Full26004 failed the old21-landmarks copy assertion; exact Tracking hand assertion now matches the intended observed state, and26005 passed. All failed logs/results remain. No failed test was removed, skipped or reclassified as a pass.

## 29. Camera ledger

Version8,14 released rows,total1262999ms,zero pending. SHA256 F1A83C4A7CAEE339AE72F7E0863F6A87A271146CCD15E070939A1680269281D4, unchanged throughout this execution. All starts consumed; remaining arithmetic milliseconds confer no permission. No camera API opened, ledger reset/amendment, new device, live screenshot or private-image persistence. Invey will be closed/camera off at delivery.

## 30. Future physical qualification proposal — NOT authorization

At most two creator-supervised, creator-enabled preview-only sessions<=180s each, if separately explicitly approved:14→16 total starts,<=360000ms new charges, unchanged1800000ms total cap (maximum1622999ms with current history). First baseline vs second hybrid, identical source/display/hand/mirror/lighting where possible. Each: labeled stationary interval, tiny/slow/normal/fast movement, warm wrist-out/palm-out<=600ms initially, return, genuine loss, mouse independence, Stop by150s; cooperative170s/cutoff178s. Aggregate jitter requires explicitly labeled stationary samples; moving intervals cannot count as stationary noise. No images/video/raw landmarks or Pocket actions. Native shutdown/independent exit/guardian release and indicator-off required before second test. No version9 policy, allowance or physical startup is implemented/applied by this proposal.

## 31. Delivery

Implementation commit `3320ed4163ad4fce46aaaa9f784029a8a5b89c52` is published to [PR9](https://github.com/yayatidattayt/Invey/pull/9). [Exact implementation-head Windows CI42792835816](https://github.com/yayatidattayt/Invey/actions/runs/42792835816) completed SUCCESS, including both core configurations and all worker tests. A documentation-only follow-up separates required test sections and records this result; its own final exact-head check is recorded in artifacts/execution-26/final-delivery.json and the delivery response. No source/test changes after the verified implementation commit.

Committed-source Release rebuild passed0 warnings/errors. Invey.exe SHA25642CC159DADB47FF8B93C2F667815EDD3630E4BA637EFC9C15EBE3A173FCDBAF5; Invey.dll SHA256E06C39BDCA66816E7A15CA0A98F8087B5DDDF7DDCE988D65D4993CCCCDC4849B. Presentation/Beta and Performance CheckOnly preflights passed with optional tracking available, camera off and zero running instances. Independent process/desktop audit found no Invey-owned process and input desktop Default. Ledger hash unchanged. Working launcher: scripts/Start-Invey-Presentation.cmd; source instructions: docs/HYBRID_TRACKING_GUIDE.md. Status: camera-free implementation READY FOR REVIEW; physical improvement UNVERIFIED, B1 not accepted.

Continue same PR9 branch; do not merge. Existing Presentation launcher remains camera off, normal profile preserved. `INVEY_VISUAL_BASELINE=1` is the reversible comparison, not camera authorization. CI installs the hash-pinned matching OpenCV for deterministic tests; runtime gracefully falls back if unavailable. Exact final commit, executable hash and successful CI are verified in final delivery receipt/response. All PR metadata restrictions respected.

## 32. Remaining blockers

Physical hybrid wrist-out/partial-hand accuracy, actual jitter, sustained device30Hz, longer-than600ms identity/drift, difficult rotation/large motion, cold isolated-finger acquisition, true second-hand detection and canonical B1/Gate A/M02 acceptance remain open. More visual availability on fixtures is not physical acceptance. The camera-free candidate has positive paired evidence; it is not release-ready or live-Pocket qualification.

Sources: [OpenCV optical flow](https://docs.opencv.org/5.0/main_modules/video_track.html), [official forward/backward sample](https://github.com/opencv/opencv/blob/5.x/samples/python/lk_track.py), [public-domain photo and author declaration](https://commons.wikimedia.org/wiki/File:Fingers.jpg), [existing ESEMES photograph](https://commons.wikimedia.org/wiki/File:Human_Palm.jpg), [package metadata](https://pypi.org/pypi/opencv-contrib-python/5.0.0.93/json).

## Final native evidence and usability addendum

Fresh typing-only seed26003 run20261110T091227500-c62bd91d1e5c4c5392940c3f717a094a passed cursor control, A03 typing soak and cleanup. Full26004 run20261110T091441391-d338bdcb36364400b4dc8e5b2df7b0b8 passed39, failed1(F06 expected the old21-landmarks status), Explorer1BLOCKED. Updated that test to require exact Tracking hand copy as requested; did not weaken/remove the observation-state assertion. Full26005 run20261110T092241571-ce0fbf91716f47fba084dddb5165f1d6 then passed40,0FAIL,1Explorer BLOCKED. Historical E25 focus failure is not reproduced in these fresh runs, not declared root-caused or erased.

Final camera-free selector entry SIMULATED model loss / actual image-motion tracking was subsequently added to the existing Recorded combo, with distinct source context and explicit no-camera/no-model-inference status. It loops generated images until deliberate Stop. Actual WPF Enable control selects the real optical-worker route; fixture restores prior selected sequence/mnemonic/image state. Final focused product26006 run20261110T093309796-a3d3a1561248472cb44a78d2d531c3eb PASS, including actual image/pipe/shared pointer5 updates, denied actions, library/cards/pinning/Workspace and cleanup. This focused run verifies the final small selector change after the broad run. No ordinary-desktop input or real camera.

Producer partial-model frames now advance Engine B from its current independent pixel correspondence without renewing the full anchor; a unit assertion checks actual reference advancement and unchanged anchor time. Baseline comparison remains unchanged. All previous diagnostics preserved. Both build configurations pass with zero warnings/errors;185 core/config and50 workers remain the expected final counts, with exact published-head CI and clean launcher preflight checked at delivery.
