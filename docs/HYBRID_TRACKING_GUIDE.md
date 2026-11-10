# Ghost hybrid preview

**Execution27 update:** two authorized real-camera A/B sessions are complete. Stable-pointer qualification is CHANGES REQUIRED: creator-reported wrist-out improvement remains unstable and no optical-only handoff was logged. Both sources delivered~19/s in current conditions, not sustained30. Hybrid designated-still RMS11.8px; baseline moving/uncertain interval is not a jitter comparator. All16 starts are consumed, all released; no further camera test is authorized. See EXECUTION_27_REPORT.md.

Camera-free: launch `scripts\Start-Invey-Presentation.cmd`. Open Ghost Tracking, keep Recorded selected and choose **SIMULATED model loss · actual image-motion tracking**. Check Desktop Ghost preview, use Calibration to select display/hand, then Enable. The generated image and real optical tracker use the same worker/protocol/shared desktop pointer as ordinary tracking. The primary observations/loss are scripted; there is no camera or MediaPipe inference in this explicitly simulated sequence. Stop remains available. Normal recorded-model inference and simulated calibration practice are still separate options.

For the generated sequence choose **Right hand** in Calibration; its primary observations explicitly identify a right hand. The fixture does not acquire a real hand or infer anatomical correctness.

Normal recorded/live pipeline: MediaPipe provides full/partial geometry; the local optical tracker measures image correspondence near an index identity established by full-hand evidence. Qualified model points are preserved. Valid image-only movement is orange/visual preview, cannot pinch or Arm Pocket, and ends within600ms of the full anchor unless a new qualified full-hand anchor refreshes it. Genuine loss, ambiguity, hand/source/frame changes, stale data or conflicting sources cancel. The last successful position is not kept alive as a fabricated observation.

The visual backend uses the already-present OpenCV package. Missing optical support falls back explicitly to the existing patch tracker. For an engineering comparison set `INVEY_VISUAL_BASELINE=1` before launching; remove it to restore the hybrid. This switch never changes camera permission or gesture gates. No new smoothing/prediction is enabled.

Execution26 has camera-free evidence only. Natural wrist-out/palm-out, actual stationary jitter, two-hand identity and sustained physical throughput remain unqualified. At the end of Execution26 all14 starts were consumed; the later explicitly approved E27 comparison used and released the two added starts. All16 are now consumed. Do not Enable another live test or apply a further amendment without a new explicit decision. See EXECUTION_27_REPORT.md for the actual outcome.

## Reproduce comparisons

Use the existing project-local Python and model:

```powershell
.\.tools\exec05-inference\Scripts\python.exe -B tests\tracking\benchmark_hybrid.py --output artifacts\execution-26\hybrid-expanded.json
.\.tools\exec05-inference\Scripts\python.exe -B tests\tracking\benchmark_real_hybrid.py --model .tools\exec05-model\hand_landmarker.task --palm .tools\exec05-model\public-palm.jpg --output artifacts\execution-26\real-image.json
```

The old ESEMES still crops fingertips; its zero optical continuations remain a negative result. Initial model landmarks are never called manual annotations.

Additional evaluation used **Fingers.jpg by CT Nehls**, dedicated to the public domain by its author: [source/license declaration](https://commons.wikimedia.org/wiki/File:Fingers.jpg). Original file: `https://upload.wikimedia.org/wikipedia/commons/4/4f/Fingers.jpg`, SHA1 `3a287291eb2fc0f9b99cd3a4ac696a3cc7aabd83`, SHA256 `04f3c5318536a807c8c29c2b726d57585aad38b3607270634d92e6e6b5ec906d`. The photo stays in ignored local artifacts; no image is redistributed in the code. Analytic translations establish relative-motion ground truth, not anatomical landmark accuracy or physical wrist-out video.

After placing the verified file at `artifacts\execution-26\public-fingers.jpg`, run:

```powershell
.\.tools\exec05-inference\Scripts\python.exe -B tests\tracking\benchmark_real_hybrid.py --model .tools\exec05-model\hand_landmarker.task --palm artifacts\execution-26\public-fingers.jpg --photo-hash 04f3c5318536a807c8c29c2b726d57585aad38b3607270634d92e6e6b5ec906d --fit-width 600 --fit-height 440 --source-name "Fingers.jpg by CT Nehls, public domain" --source-url "https://commons.wikimedia.org/wiki/File:Fingers.jpg" --output artifacts\execution-26\real-fingers.json
```

All generated transformations occur in memory; only aggregate errors/timings/counters are written. No private webcam images or raw camera landmarks are used or saved.
