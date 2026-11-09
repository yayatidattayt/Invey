# Execution21 — Hands-on Beta continuation

**Status: CONTROLLED BETA SOFTWARE READY FOR CREATOR HANDS-ON REVIEW; HUMAN ACCEPTANCE PENDING.**

The camera-free controlled pointer and product routes pass isolated Windows acceptance. The creator has not yet completed the ordinary-desktop/manual checklist. Physical camera preview is unverified; live hand-to-Pocket control is not connected and is unverified.

## Verified baseline

- Continued the existing `codex/execution-20-interaction-engine` stack from verified Execution20 head `aa2b97708786618762661a26d4ef6b4339a2b341` and existing PR #9. No parent PR was merged or rewritten.
- Locked restore, Debug/Release builds, and the existing 150-test core suite per configuration passed at baseline. The first sandboxed VSTest attempt timed out connecting to its test host; authorized local test execution passed. Baseline Windows CI run `42645901427` was green for `aa2b977`.
- Read the repository rules, handoff, Execution20 reports, interaction/precision guides and QA matrix before changes. The worktree was clean at recovery.

## Human-facing changes

- Main navigation calls the camera-free path **Try Ghost Beta** and exposes **Report a problem**. The new Beta accelerator is Alt+B; Ghost Tracking retains Alt+T. This fixed a collision found by the first extended rehearsal.
- The controlled Ghost panel now offers monitor selection, preview, arm/disarm/stop, target hover/status feedback, visible preview/armed/holding colors, a compact direction pad and focused keyboard steering. WASD/arrows move by 64 physical pixels per key step; Shift selects 16-pixel precision and Ctrl selects 128-pixel larger steps. Space press/release can grab and drag. Escape stops and hides Ghost immediately.
- Keyboard handling is scoped to the active Try Ghost panel and its explicit steering checkbox. Leaving the panel while holding disarms and releases. It does not claim to preserve focus in another app while consuming keys. The ordinary Windows mouse is never moved by Ghost.
- Controlled motion is clamped to the chosen display. The visible ring and hit testing use the same physical position. Orange indicates armed, green indicates held, and the pointer disappears on Stop/loss.
- Target failures now distinguish no reachable target, busy target and unavailable action. Hover status names the actual registered Pocket target. Covered or unreachable targets remain rejected by the stacking gate.
- Advanced target list/aim and Activate controls remain available under a clearly labeled expander. Existing Pocket, Workspace, tracking, calibration, learning, settings, diagnostics and developer test functionality remain present.
- The feedback form offers categories, description, steps, expected/actual results and an optional small local summary. It copies text only after an explicit button press; it sends and saves nothing. The summary excludes screen/camera images, landmarks, target names, desktop coordinates and personal identifiers.

## Controlled pointer and gesture boundary

- The isolated native product route drives the actual WPF control panel and sends actual key events only on the harness-owned private desktop. It verifies keyboard movement, unchanged native cursor after movement, Escape stop, actual Pocket shelf/card movement, release, pin/unpin, restart persistence, Workspace independence and recorded preview/action denial.
- The new `GhostGestureIntentAdapter` accepts timestamped full `HandObservation` values, checks camera-session identity, selected hand/confidence, freshness, sequence, ambiguity, landmark completeness and calibration samples. It applies the existing practice ratios (pinch `0.28`, release `0.42`) with a 180 ms dwell/hysteresis and emits typed candidate/confirm/hold/release/loss/ambiguous/pause/stop transitions.
- The adapter is a standalone prototype. `MayExecute` is always false and no production caller connects it to Pocket commands. Its output is never evidence of physical qualification. Recorded, partial and visual-only observations cannot execute production actions. Live gesture actuation remains **NOT IMPLEMENTED/NOT QUALIFIED**.

## Launch and profile safety

- Added `scripts/Start-Invey-Beta.cmd` and `scripts/Start-Invey-Beta.ps1`. The default is the existing isolated synthetic demo profile; the normal profile requires explicit `-UseNormalProfile`.
- The launcher checks the Release output, local runtime and optional tracking assets via the existing guarded launcher, refuses stale builds/duplicate instances/dirty source, and reports source commit, executable path and SHA-256. `-CheckOnly` never starts the app; `-Rebuild` uses the pinned locked restore/build. Logs are bounded under the local Invey Beta diagnostics folder.
- No installation, administrator access, startup registration, normal library migration or camera start is performed by the launcher. `scripts/Start-Invey-Demo.ps1` and `scripts/Start-Invey-DualCursor.ps1` remain intact.
- Launcher parser validation passed. Final `-CheckOnly` and actual executable hash are recorded in the delivery addendum after publication. The app is left closed.

## Camera and physical test status

- Camera stayed off throughout automated work. The last audited camera ledger SHA-256 is `01A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58`; the file hash is unchanged. No new session, reset, extension or guardian bypass was made.
- **REAL CAMERA PREVIEW UNVERIFIED. LIVE HAND POCKET CONTROL UNVERIFIED.** The earlier allowance is exhausted. A separate, bounded request and six-part physical procedure are in [INVEY_LIVE_PHYSICAL_TEST_PLAN.md](INVEY_LIVE_PHYSICAL_TEST_PLAN.md). It proposes at most two sessions of up to 180 seconds, conditional on fresh explicit authorization and an independent budget review. Do not start under the existing allowance.
- The manual ordinary-desktop route is [INVEY_BETA_TEST_CHECKLIST.md](INVEY_BETA_TEST_CHECKLIST.md). All human checks begin NOT TESTED; the isolated test suite is not substituted for them.

## Performance observations

- The extended E20 isolated product route measured median 4.4842 ms and p95 54.8641 ms for command/IPC round trips including WPF state observation. This is not frame pacing or physical camera-to-screen latency.
- Startup duration, long-run CPU/memory stability and ordinary-desktop rendering were not measured in this session. No performance target is claimed for them.

## Regression and failures

- Final Debug and Release solution builds: passed, zero warnings/errors.
- Final Debug and Release core suites: 156/156 each, zero skips. The added target-lifetime regression verifies that closing or replacing a registered target releases and disarms an active hold. A restricted VSTest attempt stalled before connecting to testhost; the preserved local failure was followed by a local test run, which caught and corrected a dwell-timestamp assertion. The final Release run used the project-local temp directory after an initial isolated `File.Replace` test-fixture error; the failure is retained.
- Worker suite: 29/29, no skips, with the controlled generated video fixture and project-local temp directory. An initial run had one sandbox temp-directory cleanup error; its output is retained.
- Final rebuilt Release isolated run `20261109T165055502-07d13d3e96f0404c99751d9f87738cee` (seed 271828): 39 PASS, 0 FAIL, 1 INCONCLUSIVE, 1 BLOCKED. D17 took 10.64 s and passed the real approved screen capture, two synthetic samples, pinned card, two Workspace references, simulated Ghost movement, recorded model, calibration practice, settings, restart persistence, camera-off and worker exit. E20 passed all product checks; command/IPC/WPF round trip median 5.4569 ms, p95 67.8293 ms. The inconclusive closed-Ghost cursor observation saw 119 movement samples while injection was inactive; source of movement is not attributed. D17 complete demo and E20 dual-pointer product both pass. E20 explicitly verifies keyboard steering and Escape stop. The Explorer/Alt+Tab tray case is BLOCKED because the private desktop has no Explorer shell. `CURSOR_CONTROL_GHOST_CLOSED` remains inconclusive; it is not promoted to pass.
- The first extended run `20261109T162626252-f833bc11ac0c4196b6549652d9cf4666` had D17/E20 fail at Alt+T. Root cause was the new Beta button sharing Tracking's accelerator. The button is now Alt+B; the original failed run is preserved and the full rerun passes.
- The first new calibration unit-test attempt had a bad test expectation; it was corrected and the full core suite passed. The test was not skipped or weakened.
- Full results and individual logs remain in `artifacts/automated-acceptance/` and `artifacts/execution-21/`.

## Native integration and manual boundary

- Pocket capture, references, floating cards, pinning, shelf, Workspace, shortcuts, recorded tracking, calibration practice and cleanup passed their runnable isolated cases.
- Real WPF keyboard events in the isolated harness confirm the panel moves Ghost without moving the native cursor. They are not human usability evidence.
- Normal taskbar/tray/Alt+Tab, ordinary editor focus behavior, user-perceived card aim, hand ergonomics, physical camera behavior, live pinch and ordinary-desktop cleanup still require the creator's deliberate manual review. No ordinary-desktop synthetic input was sent.
- Physical mixed-DPI and Explorer behavior remain outside this environment. The selected-display route has deterministic/native isolated evidence; hardware variations are not qualified.

## Build linkage, cleanup and delivery

- Release apphost: `src/Pocket.App/bin/Release/net10.0-windows/Invey.exe`, SHA-256 `443B3E4C026468DA6244883CB23CCE1159CDCEC177AC6F16A78DA61B9734392B`.
- Acceptance-hosted WPF app assembly: `src/Pocket.App/bin/Release/net10.0-windows/Invey.dll`, SHA-256 `78BD7DEC16797E75C5AA6E3083136F5F9C6A2D11EE169FB0FA5783026316E86F` (matches the recorded hash in the isolated result JSON).
- Isolated desktop audit returned `Default` after each completed run. No Invey, worker, Python or dotnet process remained. No ordinary desktop input injection or camera activation occurred.
- Source commit `0578f63ff2765f66df3a5166b71d0eed95fe7463` was published to PR #9. GitHub Actions run `42660005352` passed for this exact commit. PR #9 remains open, draft and unmerged. Its metadata update was previously rejected with HTTP 403; title/body were left unchanged.

## Final state

- **CONTROLLED BETA SOFTWARE:** PASS in camera-free isolated native acceptance; creator hands-on checklist pending.
- **RECORDED PREVIEW:** PASS in the existing recorded-source integration.
- **REAL CAMERA PREVIEW:** UNVERIFIED; no camera session authorized or run.
- **LIVE HAND POCKET CONTROL:** UNVERIFIED and disconnected from production actions.
- **Explorer/Alt+Tab:** BLOCKED in isolated desktop; manual check remains.

## Published delivery record

- PR: https://github.com/yayatidattayt/Invey/pull/9 (open, draft, unmerged).
- Published commit:  578f63ff2765f66df3a5166b71d0eed95fe7463 on codex/execution-20-interaction-engine.
- Exact-head GitHub Actions run 42660005352: **SUCCESS**; all steps including SDK, locked restore, Debug/Release build and tests, and synthetic worker tests passed.
- Launcher -CheckOnly passed on the clean committed tree. It reports the isolated demo profile, camera off, tracking runtime available, zero running Invey instances and the Release apphost hash in this report.
- PR metadata update remains unavailable (HTTP 403); no merge performed.

- Report/handoff commit b7a1a931e7c5ccb2b37f06c2f43d58513e6fb4e CI run 42660335358 passed all jobs, including locked restore, Debug/Release build and tests, and worker suite.
