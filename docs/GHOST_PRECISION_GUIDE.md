Execution25 current status: real ASUS capture/conversion/inference measured near29–30Hz; creator confirms smoother motion and improved-but-persistent jitter. Repeated model loss and wrist-out freeze/disappearance remain FAILED. A bounded-anchor retention repair has only camera-free evidence. Four-observation identity warmup and action safety remain unchanged. All14 approved starts are consumed; ledger version8/1262999ms/0pending. No new camera permission. Older session counts below are history; see EXECUTION_25_REPORT.md.

# Ghost precision preview

Open **Invey → Ghost Tracking**. Recorded tracking remains the camera-free demonstration. Open **Calibrate**, choose your display and hand, and enable **Experimental finger pointer preview** to see the logical pointer. Camera-image mirror and horizontal pointer mapping are separate settings.

The pointer now settles more quietly and responds faster to sustained movement in controlled tests. A sudden large jump waits for another observation so a single noisy frame does not throw the pointer across the preview.

After a full hand has been seen, Ghost can briefly follow the image around the index tip when the hand model loses it. **Tracking visible finger** means image correspondence, not a complete hand detection. Its orange camera-image dot is preview-only. The image must remain distinctive and in view. This continuation lasts at most **0.6 seconds** from the last full-hand anchor and can end sooner if the image becomes uncertain. Show your full selected hand again to re-establish tracking.

**Reacquiring** or **Finger tracking lost** means the pointer cannot currently be trusted. Ghost does not hold a missing finger on screen indefinitely. A returning hand that conflicts with the previous visual track is hidden and reacquired rather than silently joined.

Visual continuation cannot pinch, click, drag, acquire ownership or teach a personal model. Personal Learning still uses fresh, full selected-hand observations and its existing guided lesson. No Windows mouse movement is generated.

## Current hardware limit

The existing diagnostic allowance is exhausted: ten charged sessions, including all five Execution18 starts. No new physical-camera session was run in Execution19. The ledger has not been reset or extended. Do not remove the diagnostic configuration to bypass it.

Physical wrist-out reliability, real stationary jitter and finger-only cold start remain unverified. Starting with only a fingertip visible is not supported by this warm continuation mechanism.

## Camera-off launch for this execution

From the repository folder in PowerShell:

```powershell
$env:INVEY_CAMERA_EXECUTION = 'execution-18'
$env:INVEY_CAMERA_BUDGET = Join-Path $PWD 'artifacts/execution-16/camera-budget.json'
$env:INVEY_CAMERA_DIAGNOSTICS = Join-Path $PWD 'artifacts/execution-18/live'
./scripts/Start-Invey-Demo.ps1 -DemoProfile
```

This uses the existing isolated demo library and retains the exhausted guardian. It does not enable the webcam. Use Recorded tracking, then **Stop** to end the worker and **Exit** to close Invey.

For an engineering comparison only, set `INVEY_LEGACY_POINTER=1` before launch to retain the old One Euro filter and disable the new visual continuation. Remove that variable for the precision candidate. It never changes camera permission or gesture authority.

## Future supervised check

Only after a new explicit bounded authorization, prepare settings with the camera off, then let the participant press Enable. Spend roughly 20 seconds holding still, 20 moving along simple paths, 30 moving the palm out and back while keeping the tip visible, and 20 removing/reintroducing the finger. Stop well before the session limit and verify the indicator, worker exit and guardian release. Record aggregate metrics and participant observations separately; do not save camera imagery or raw landmark streams.

## Execution26 —2026-11-10
Execution26 uses one final existing precision filter after model/partial/validated optical source selection. No smoothing tuning/interpolation/predicted positions. Recorded selector has a clearly SIMULATED image-motion option using a right-index fixture; choose Right in Calibration. User-reported physical wrist-out failure remains unresolved until separately approved retest. All14 camera starts remain consumed.
