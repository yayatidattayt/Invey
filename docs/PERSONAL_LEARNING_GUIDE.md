# Personal Learning in Ghost

Experimental local mapping correction; gesture practice and the Windows mouse remain on their existing safety paths.

## Enable and teach

1. Open Invey → Ghost Tracking. Nothing starts automatically.
2. Choose Recorded, or deliberately select and Enable a live camera when appropriate. Recorded and simulated results are not physical tracking evidence.
3. Open Calibration. Select a connected display and **Left hand** or **Right hand**. “Any hand” cannot teach. Check pointer direction and mirroring first.
4. In **Personal Learning**, turn on **Learn from my tracking sessions**. This preference persists independently of camera Enable.
5. Choose **Improve my tracking**. The selected-display diagram shows an outlined intended target, a gray baseline marker and a blue personal preview. It is a diagram of the selected display, not an OS cursor.
6. Intentionally point at each target and hold one full hand steady. Confirm **Correct target — confirm** only when the outlined target represents your intended position. Do not chase the predicted dots and then call that ground truth.
7. There are nine baseline targets, nine teaching targets and nine different validation targets. Eight fresh stable observations are pooled into one example per confirmed target. Retry clears the current window; Skip adds no label; Cancel discards the in-memory lesson.
8. Choose **Evaluate candidate**. Training runs away from the rendering thread. An improvement must survive the separate validation set and conservative regional checks. A rejected update leaves your accepted model unchanged.

A short camera session may not be enough to complete all targets. Stop at any time. Missing, stale, partial, ambiguous and wrong-hand observations cannot teach. A session/display/mapping change cancels the lesson. The learning section is below the existing calibration controls; use the tracking window's scroll bar.

## What changes

Six small coefficients correct systematic position error in the central 20–80% of the mapped display. Outside that region, the original mapping is used. Device hash, source, image dimensions, selected hand, display geometry/DPI, mapping mirror, inset and sensitivity must match.

The correction affects **only the clearly labeled personal preview**. It does not change MediaPipe weights, recover an undetected finger, authorize a pinch, change dwell thresholds or control other applications. Existing practice and Dual safety remain authoritative.

**View learning results** shows measured normalized errors and physical pixel errors for confirmed targets. These are not a general accuracy guarantee. Physical improvement requires real confirmed independent observations. A synthetic test is labeled as such.

## Feedback

Select Correct, MissedPinch, FalsePinch, MissedRelease, TrackingLoss, WrongHand or WrongDirection and choose **Give feedback** during tracking. These reports are not target coordinates. Pinch/loss feedback contributes bounded local summaries only. The interface explicitly reports insufficient time-labeled gesture evidence; it does not pretend that a gesture classifier was trained.

## Saved data and reversal

The selected Invey profile contains `Learning/personal.json`, a checksum-protected file with accepted and previous coefficients, context and bounded aggregate metrics. The normal profile is under `%LOCALAPPDATA%/Invey`; the explicit demo profile is under `%LOCALAPPDATA%/Invey-Demo/Execution17`. Demo data stays separate.

No teaching examples, raw landmark streams, camera images or videos are retained by learning. No cloud training or export occurs. In-memory examples are discarded after evaluation, Cancel, Stop or session replacement.

- Turn off **Learn from my tracking sessions** to stop collection and training while ordinary tracking continues.
- Turn off **Use accepted personal correction (preview only)** to compare the original preview without deleting a model.
- **Restore previous model** swaps current and previous. Press it again to reapply. When no previous model exists, rollback returns to the default mapping.
- **Reset learning** removes learned parameters and the backup and disables learning. It never resets Pocket or calibration. A newer unsupported schema is preserved rather than overwritten.
- Unreadable or incompatible data falls back visibly to defaults. Original data remains preserved until explicit supported reset. Read-only or concurrent-write failures are shown; no silent success is claimed.

There may be no update because targets were skipped, coverage was poor, direction was wrong, no fresh full-hand examples existed, corrections were too large, a context changed, or the candidate did not beat both baseline and the current model safely. Unlabeled sessions save quality counts, not invented learning.

## Supervised Execution18 check

The owner authorized at most three additional user-enabled sessions, each bounded to180 seconds, carrying forward the five prior ledger charges. The independent guardian uses a178-second cutoff and retains the original12-session/1,800-second totals. This is a test-specific allowance; do not renew it by replacing the ledger.

Use deliberate Enable only. Complete baseline → teach → independent validation, then Stop and confirm release. Restart with the camera off, inspect persisted results, compare rollback/reapply, then deliberately Enable again only if another session is needed. Report “physically unvalidated” unless real confirmed observations support the comparison.
