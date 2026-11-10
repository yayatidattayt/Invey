# Invey Live Physical Test Plan — authorization required

## Current gate

**No live session is authorized by Execution21. Do not enable or acquire the webcam for this plan.** The last verified ledger has 10 released sessions totaling 809,686 ms, no pending release, and no remaining Execution18 starts. The applicable historical caps were 12 sessions and 1,800,000 ms. On that snapshot the arithmetic headroom is at most two sessions and 990,314 ms, but the exhausted Execution18 start allowance cannot be reused. Any new attempt needs a fresh, explicit, execution-specific authorization and an audited ledger amendment that retains all old charges. No reset or bypass is acceptable.

## Proposed request, if camera-free beta review is satisfactory

Authorize **at most two user-enabled sessions, each no longer than 180 seconds** (aggregate maximum 360 seconds). This is a proposed cap, not permission. It would retain the old 809,686 ms and 10 released rows; the theoretical cumulative totals would be 12 sessions and 1,169,686 ms, within the historical 12-session/1,800,000 ms ceiling. A separate policy review must confirm that these caps still apply before any amendment.

### What the user would do

For each separately authorized session, the creator selects the ASUS FHD camera and presses the existing **Enable** button. Invey shows its local live preview and visible index/landmark tracking. The user, not automation, performs the movements below. The assistant does not use the ordinary desktop to inject input.

1. Confirm camera selection, selected hand, display, mapping and calibration. Keep Pocket control disarmed for pointer-only observation.
2. Sweep slowly across the working area, make rapid and small movements, then hold still to estimate pointer jitter.
3. Confirm mirroring direction and point/ring alignment.
4. With a safe visible Pocket sample, verify neutral/open hand causes no action. Test calibrated pinch candidate, sustained confirmation, hold, release and hysteresis.
5. Only if all previous criteria pass, arm supported Pocket control and test one card grab, movement, release and pin/unpin. No third-party application actions and no unrestricted OS input.
6. Remove the hand, show partial hand, ambiguity and tracking loss; verify held state cancels. Press Stop while idle and while holding.
7. Confirm worker exit and camera indicator off before ending the session.

### Data and safety

- Preview frames and raw landmarks are not part of the requested evidence and must not be written to disk. Do not save screenshots or recordings.
- Record only session generation, frame/observation counts, readiness and intent transitions, timing summaries, known loss reasons, stop/release confirmation and the user's qualitative notes.
- End immediately on unexpected action, wrong target, uncommanded cursor movement, focus disruption, uncertain hand identity, missing status/Stop, camera indicator staying on, worker surviving Stop, or the 180-second deadline.
- Press Stop, then Exit. Confirm the worker is gone and the physical camera indicator is off. If release cannot be independently confirmed, do not start another session.
- Old charge rows remain untouched; each reservation and release is separately auditable.

## Qualification boundary

Recorded or synthetic sequences can verify state-machine transitions but cannot qualify camera-to-Pocket actuation. Live gestures remain disconnected from the production `GhostDesktop` command path until a separately reviewed physical safety gate passes and explicit user authorization is granted.

Execution22 clarification: this older plan does not authorize Gate3. Use LIVE_GHOST_TEST_GUIDE.md: pointer and proposal qualification are separate from live actuation. This build has no live qualification issuer; no camera authorization or ledger amendment was made.
