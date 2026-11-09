# Personal Learning

Ghost can learn a small local pointer correction from targets you confirm. This remains an experimental preview, separate from gesture actions.

## The short route

1. Open **Ghost Tracking → Calibrate** with the camera off. Choose your display and **Left hand** or **Right hand**, then **Save settings**.
2. Check **Learn from my tracking sessions**. Select Recorded or deliberately select and Enable your live camera. Nothing activates the camera automatically.
3. Press **Start lesson**. Saved calibration is opened internally. If your hand is temporarily out of view, the app waits for a fresh full-hand observation instead of making you hit Start at exactly the right moment.
4. Keep the selected hand and wrist fully visible, inside the working region. Use your other hand for the mouse/keyboard. Follow the outlined target and press **Confirm target** only when it represents where you intended to point.
5. Complete nine baseline, nine teaching and nine separate validation targets. The same main button guides the whole lesson. At the end it becomes **Evaluate lesson**.
6. Read the accepted/rejected result, then press **Stop** above to release tracking.

The outline is the intended target; gray is the original mapping and blue is the personal preview. Do not chase a dot and label that as independent ground truth. Eight fresh steady observations make one example. Missing, stale, partial, ambiguous and wrong-hand observations cannot supply labels.

Temporary tracking loss pauses confirmation without erasing already confirmed targets from the same session/setup. Stop, Cancel lesson, a replaced session or incompatible geometry discards the in-memory lesson. No label is fabricated.

## Fewer controls

The normal section shows one main action, a progress message and **Cancel lesson** when needed. **Calibration settings & practice** and **Learning details** keep the optional controls available without filling the lesson screen.

Expand **Learning details** for:

- **Retry target** or **Skip target**. Skip supplies no label and may leave too little evidence to accept a model.
- **View learning results**, comparison with the original preview, rollback/reapply and Reset.
- Separate **practice feedback**. Its Correct/MissedPinch/etc. dropdown does not confirm a lesson target. Feedback contributes counts only; there is not enough time-aligned gesture evidence to train a gesture classifier.

Turn learning off to stop collection/training while ordinary tracking continues. **Cancel lesson** cancels learning; the separate **Stop** button stops tracking and releases its worker.

## What is learned

Six bounded coefficients correct mapping in the central20–80% of the selected display. A new model must improve independent target errors against both the original mapping and a compatible existing model, including regional/tail checks. Worse or insufficiently supported updates are rejected.

The correction does not change MediaPipe weights, recover an undetected finger, change pinch/dwell safety or control other applications. Source, device hash, image dimensions, hand, display geometry/DPI, mapping mirror, inset and sensitivity must match; otherwise the original preview is used.

Measured errors describe the confirmed targets. Synthetic tests are not physical accuracy evidence.

## Data and reversal

The selected profile stores Learning/personal.json with current/previous coefficients, context and bounded aggregate metrics. Teaching samples stay in memory only. No camera images, video or raw landmark streams are retained or uploaded by learning.

Normal data is under %LOCALAPPDATA%/Invey; the explicitly selected demo profile is separate under %LOCALAPPDATA%/Invey-Demo/Execution17.

**Restore previous model** swaps the current and previous correction; press again to reapply. If no previous model exists, rollback returns to the original mapping. **Reset learning** removes the learned parameters/backup and disables learning without touching Pocket or calibration. Corrupt data falls back visibly to defaults; newer unsupported schemas are preserved.

## Timed engineering sessions

The current supervised test build shows remaining authorized sessions before Enable. Exhausted or unverifiable allowances disable Start with a specific reason. Normal recorded demonstration remains camera-free.

The original three Execution18 starts were explicitly extended by two; prior charges and the overall12-session/1,800-second limits remain intact. There is no automatic reset or hidden quota renewal. Cooperative stop now starts at170 seconds, leaving eight seconds before the unchanged178-second hard guardian cutoff, within the authorized180-second ceiling.

If a release acknowledgement is missing, the camera stays blocked pending independent review. An explicit review may use matching process-exit evidence and the participant's camera-indicator-off confirmation, with a durable receipt and the full reserved time charged. This is not a claim that the missing native API acknowledgement was received, and it grants no new start.
