# Live Ghost qualification guide — Execution 22

Current status: software bridge implemented; final native regression BLOCKED by foreground ownership. Physical webcam, gesture accuracy and live Pocket actions are NOT QUALIFIED. No new webcam session is authorized.

## Launch and camera-free use

From the repository folder run `scripts\Start-Invey-Beta.cmd`. This uses the separate synthetic demo profile. Open **Try Ghost Beta**, Start preview, enable focused keyboard steering, then use WASD/arrows. Arm Pocket explicitly; Space grabs/releases. Escape stops Ghost. Pocket, Workspace, Settings and **Report a problem** remain available. Feedback copies only when requested and sends nothing.

For tracking preview, open **Ghost Tracking**, select **Recorded**, and Enable recorded tracking. Check **Desktop Ghost preview** and configure display, hand, mirror and working region in calibration. The ring is the mapped independent pointer. Recorded tracking has no live action capability.

## Gesture practice

With valid tracking and calibrated desktop preview already running, choose **Begin gesture practice**. Hold fingers apart until at least 12 eligible observations; Continue to pinch, collect at least 12 deliberate thumb/index pinch observations; Continue to release and collect at least 12 open observations. Check samples and practice. If samples overlap, lose identity, change context or have invalid timing, repeat the lesson. Samples are bounded and held in memory only.

Open hand establishes neutral. Brief approach is only a candidate; sustained pinch confirms after 180 ms. Continued pinch holds the proposal; sustained opening confirms release. The Tracking status shows the proposal/rejection reason. **Pocket interaction remains disabled.** Practice is not proof of physical accuracy. Changing camera/display/hand/calibration requires a new lesson. Stop then restart is required to resume a paused classifier.

## Separate physical permission decisions

The durable ledger has 10 released sessions, 809686 ms charged, no pending reservation and zero remaining Execution18 starts. Do not reset it or use another camera API. A future explicit authorization and audited amendment must preserve every historical charge. The older global maximum is not authorization.

Proposed request for a later decision: at most two creator-enabled sessions, each up to 180 seconds, aggregate 360 seconds, for pointer-only and proposal-only qualification. This document does not authorize or implement that amendment. The creator may decline either session. Start only after a separate explicit decision, with creator present and the bounded guardian enforced.

1. **Gate 1, pointer only:** select ASUS FHD device, display and hand; deliberately Enable. Check image/dots, mirror right/left and up/down, edges, center, diagonal motion, stationary jitter and tracking loss. Confirm the native mouse remains independent. Keep Pocket actions disabled. Stop by the agreed bound; verify worker exit and camera indicator off. If release is uncertain, no further session.
2. **Gate 2, gesture proposals only, after Gate 1 passes:** deliberately Enable under its own remaining authorized session. Perform the guided lesson and repeated open/pinch/hold/release cycles, pinched entry, brief approaches, partial thumb, wrong/second hand, loss, Stop and restart. Record false positives and rejected observations honestly. No Pocket actions. Stop and independently confirm release.
3. **Gate 3, live Pocket:** requires a further explicit live-actuation decision and a separately reviewed qualification issuer. This build has no issuer and cannot enable this gate. Prior gates do not automatically unlock it. A future authorized build should use a safe sample card, explicit Arm, deliberate grab/drag/release, native mouse independence, ambiguity and Stop-during-hold checks.

No video, raw landmarks, screenshots or private screen content are needed as evidence. Record bounded counts, timing summaries, qualitative direction/jitter feedback, false candidates, cancellations and independent release confirmation. Stop immediately for wrong target, unexpected cursor/focus movement, missing controls or uncertain release. Use **Stop**, then **Exit**; verify camera light off and owned worker exit. Report problems through the main window with steps, expected and actual behavior.
