# Your first real Ghost pointer test

Reading this guide does not authorize the camera. The current allowance is exhausted. Wait for the separate one-session approval and audited amendment before Enable. This first test is **pointer preview only**; gesture proposals and live Pocket actions are separate later decisions.

## You can try this now with the camera off

From the repository folder run `scripts\Start-Invey-Beta.cmd`. Open **Try Ghost Beta** and Start preview. With keyboard steering selected, WASD/arrows move Ghost; explicit Arm and Space can demonstrate controlled Pocket grabbing. Escape stops. The ordinary mouse remains the Windows cursor. All samples in the demo profile are synthetic.

Open **Ghost Tracking** for recorded tracking or guided practice. Source labels distinguish recorded, simulated and camera input. Recorded software success does not establish physical tracking accuracy.

## When the pointer-only session is separately approved

1. Run `scripts\Start-Invey-RealGhost.ps1`. Invey opens with the camera off and the separate demo profile. Open **Ghost Tracking**.
2. Choose **Live camera**, Find cameras, and select **ASUS FHD**. Choose local image preview if desired so you can see what is in view. This alone does not start the camera.
3. Open Calibration. Choose the intended display and Left or Right hand. Check the working region and the **pointer mapping Mirror** setting. Camera-image mirroring changes the image presentation separately. Do not change Windows scaling.
4. Check **Desktop Ghost preview**. Keep Pocket actions disabled. Confirm the selected display/hand in the summary. Place a comfortable full hand in view, index visible. Only you press **Enable live camera** after approval.
5. For about 20 seconds move left/right, up/down and diagonally. Say whether Ghost moves in the intended direction. Move the real mouse separately: it should remain independent of Ghost.
6. Hold the index still for about 10 seconds and describe visible jitter. Then make tiny adjustments and ordinary faster motions. Describe sticking, delay or overshoot. These are creator observations, not measured optical latency.
7. Move the wrist/palm toward the frame edge while keeping the index visible. Say whether the dot and Ghost follow, freeze or disappear. Remove the hand and check for loss, then return it and describe reacquisition. Do not treat a frozen marker as tracking.
8. Press the fixed **Stop** button by 150 seconds, leaving time for cleanup. Automatic cooperative Stop is at170 seconds and the independent hard cutoff at178 seconds, within the180-second maximum. Confirm **Stop complete**, worker exit and camera light off. If release is uncertain, do not restart. Close with Exit when requested.

The session records only bounded diagnostic counts/timings and release evidence. It does not save camera frames or raw landmarks. There is no live Pocket action issuer. No physical gesture session or card-control test is authorized by the pointer permission.

## Feedback and normal desktop checks

Describe: correct/wrong horizontal and vertical direction; stationary jitter; tiny-motion response; lag/overshoot; partial-hand follow/freeze/disappearance; reacquisition jump; real mouse independence; and camera light off after Stop. Report unexpected actions immediately and stop.

On the normal desktop, manually check taskbar, Alt+Tab, tray reopening, editor typing/focus, floating cards, shortcuts, Ghost Stop and Exit. Mark anything you did not personally try **NOT TESTED**. The private desktop has no Explorer shell, so automated results cannot certify these items. Use **Report a problem** to copy your notes only when you choose; nothing is sent automatically.
