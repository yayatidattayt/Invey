# Invey Execution 15 — Pocket shelf, shortcuts and Dual Workspace

## A — Executive assessment

Invey starts with the webcam off and presents Capture, Pocket, Workspace, Ghost and Settings as normal product actions. The existing Pocket shelf tucks into an actually hidden window. Windows shortcuts are configurable, and the conventional Workspace loads two real local references through the existing library and interaction reducer. The application-owned slice is ready for code review. Release acceptance remains blocked by the diagnostic scope failure and open physical gates.

## B — Repository and parent reviews

The starting tree was clean at PR #3 head `afbabaeb8df71e7bdc140971694687e53d544a97`. Fetch confirmed the current origin refs. The starting head is preserved at `refs/checkpoints/execution-15-start`. The feature branch is `codex/execution-15-workspace-shortcuts-reliability`. The repository had one registered worktree and no normal Invey or legacy application data at the starting inventory. The active checkout stays at its existing path because the current session, local SDK and evidence use it.

| Parent | Head | State at recovery | Windows verification |
| --- | --- | --- | --- |
| [PR #1](https://github.com/yayatidattayt/Invey/pull/1) | `48f5449a0af1b40681eee40fa056b4491cc418a4` | Open, mergeable | [Pass](https://github.com/yayatidattayt/Invey/actions/runs/42189993965) |
| [PR #2](https://github.com/yayatidattayt/Invey/pull/2) | `3309cd3dd88369534278dbde684c90ac6bf6a388` | Open, mergeable | [Pass](https://github.com/yayatidattayt/Invey/actions/runs/42221648236) |
| [PR #3](https://github.com/yayatidattayt/Invey/pull/3) | `afbabaeb8df71e7bdc140971694687e53d544a97` | Open, mergeable | [Pass](https://github.com/yayatidattayt/Invey/actions/runs/42229159360) |

## C — Baseline

The pinned SDK is .NET 10.0.403. The existing Release test assembly passed all 80 tests before editing. Execution 14's retained evidence establishes 34 native isolated passes and one Explorer block, with separate real webcam Ready and lifecycle evidence. Historical failures remain in the local run directories.

## D — Capture discrepancy

The original failure showed a colored preview of the intended black region even though sparse stripe anchors matched. The new fixture independently counts nonblack pixels in the original band and a bottom control band, keeping only the counts and bounding rectangle. In run `20261106T184333574`, the original band contained 8,018 nonblack pixels at x806–1016 and y324–361 immediately after closing a floating card. All 211 pixels across each of 38 rows were nonblack. The same band in the other three captures and the bottom band in all four were black. The location and timing match the closed floating card's title bar and support stale native chrome as the cause of this reproduction. The earlier historical run does not contain comparable full-region probes.

## E — Capture repairs

Disable DWM transitions on Pocket's capture, shelf and floating-card windows and other frequently opened Invey surfaces, including Tracking, Workspace, Settings and Rename. Serialize the freeze with ongoing card opening so an asynchronously restored card cannot appear after capture has enumerated the windows to exclude. Keep the existing cloak, native visibility check and restoration, full freeze in memory, crop-only persistence and explicit black confirmation. The final isolated run found zero nonblack pixels in all four samples of each of the historical, bottom and large bands. The fixture also exercises a small black crop, the black/color boundary, and a known 230 × 144 color crop. DWM synchronization is scoped to the calling application. [Window attribute](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute) · [DwmFlush](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmflush)

## F — Pocket shelf

The shelf reuses the existing Pocket window and card manager. It has two steady visibility states. Tuck, Escape, or the window close affordance hides it after any current modal or capture interaction ends. The live window preserves its selection, while separately owned pinned windows remain visible. There is no hidden edge input target.

## G — Reveal and no-camera use

The main window reveals the shelf. The shared tray and registered shortcut deliberately toggle it. Reveal restores a minimized shelf. Capture can open a selector while the shelf is tucked and returns it to its prior visibility. All these paths use the existing local store and leave the camera off. The shelf uses immediate native show and hide, so no reveal animation delays input or conflicts with reduced motion.

## H — Shortcuts

Defaults are Ctrl+Alt+G for capture, Ctrl+Alt+P for the shelf, Ctrl+Alt+Space for Pause Ghost, and Ctrl+Alt+W for Workspace. Settings accepts a bounded set of Ctrl+Alt, Ctrl+Shift and Ctrl+Alt+Shift bindings with a letter, digit or Space. A blank entry disables that action. RegisterHotKey reports unavailable chords without reclaiming a fixture's binding. Apply reserves all new chords before committing the file, preserving the previous map if a reservation or atomic write fails. MOD_NOREPEAT suppresses repeat presses. Dispatch defers during a physical mouse button hold. Registrations are removed on exit. [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)

## I — Workspace

Workspace is a conventional WPF window using the same verified local store. Select a saved reference and load it into A or B. Each view has a selected outline, a move grip, reachable resize and move controls, Fit and 100% zoom with scrolling. The Workspace board layout lives for that window session. Floating card layout continues to persist separately. Capture, Workspace and the shelf have no camera prerequisite.

## J — Dual ownership

Native mouse input and the private fixture's logical Ghost stream use separate pointer owners in the existing reducer. A target stays claimed until that owner releases it or the target disappears. Removing a target cancels only claims on that target. Closing the window cancels both owners and releases mouse capture. The simulator is confined to the isolated acceptance host. Workspace does not route live Ghost hand tracking to production actions.

## K — Floating cards and persistence

The placement writer serializes work and coalesces pending changes by card ID. Failed writes produce visible feedback and leave the drain able to commit later requests. Delete takes the library edit lock before awaiting current placement, and the committed deletion removes the floating card and any Workspace view for that reference. Exit waits for the active capture and library edit and drains final placements. The existing four-card and 24-million-pixel limits remain.

## L — DPI and display recovery

The isolated fixture uses the available 125% display. Core selection tests cover 100%, 125% and 150% geometry, including negative desktop origins and exact edges. Existing placement tests cover clamping and a missing display. Actual mixed-DPI, physical display removal and resolution-change trials remain blocked by the available display configuration. No Windows display setting was changed.

## M — Camera scope and session audit

The creator authorized up to three visible sessions of 20 seconds each and volunteered to present hands. The visible lab ran with a 16-second automatic cancellation timer and no camera at startup. The participant controlled Enable during the physical handoff. The initial diagnostic build enforced the per-start timer but omitted a cumulative start limit. Two additional manual starts occurred before the lab was closed. The run exceeded both the three-start scope and the 60-second aggregate scope. This is an execution safety failure. All five sessions are retained in the audit. The build now rejects a fourth diagnostic camera start across all lab windows in one app process and keeps the time limit when the configured diagnostic folder is absent. There were no further camera runs after the overrun.

| Start | Native activation-to-release | Accepted | Hand / partial / ambiguous / empty | Ready displayed | End |
| --- | ---: | ---: | --- | --- | --- |
| 1 | 11.669 s | 107 | 56 / 0 / 0 / 51 | Calibration panel absent | Timer |
| 2 | 15.081 s | 140 | 42 / 25 / 0 / 73 | 2 | Timer |
| 3 | 15.076 s | 140 | 55 / 11 / 1 / 73 | 3 | Timer |
| 4, beyond scope | 15.173 s | — | — | — | Timer |
| 5, beyond scope | 14.530 s | — | — | — | Explicit stop |

The first three summed to 41.826 seconds in the measured native activation-to-release window. The five-start total was 71.529 seconds. All five confirmed native Media Foundation shutdown and worker exit without force. Cleanup took 485–544 ms. The lab showed Camera off and was then closed, followed by Invey. Only aggregate local timing and count diagnostics were retained. No camera frames, video, biometrics or raw landmark histories were stored or sent.

## N — Physical mirror and hand findings

Within the authorized starts, the second session reached Ready with Mirror off, and the third reached Ready with Mirror on and had one ambiguity. Calibration recorded Any hand. An ambiguity count alone does not verify physical two-hand cancellation of a held practice target. Mirror direction, selected physical hand and independent selected-display route remain inconclusive without a corresponding witnessed movement trace. The out-of-scope starts are disclosed for safety accounting and are excluded from acceptance evidence.

## O — B1 acceptance

Live ASUS FHD acquisition, bounded per-start cleanup and visible Ready are observed. Physical B03 direction and route, selected-hand behavior, and physical ambiguity during an active practice hold remain open. The camera-free acceptance host exercised the same process-wide reservation function. The first three requests succeeded and the fourth was rejected.

## P — Resource profile

Run `20261106T184645350` exercised four WPF cards for 109 cycles over 45.58 seconds. It refused a fifth, moved and resized the four cards, alternated pin and zoom and toggled the shelf. After four observed write failures under a manifest lock, the same process committed the latest zoom to all four card records and captured a clean synthetic black band with the cards open. Card startup including synthetic fixture creation took 219 ms. Median and p95 command round trips were 47.7 and 64.0 ms, including IPC, dispatch, window updates and test state serialization. Working set started at 180 MiB, sampled a high of 208 MiB and ended at 195 MiB. Managed heap samples ranged from 5.1 to 13.4 MiB. GDI objects were 74–76 and USER objects 76–78. The seven to eight visible WPF windows included the test fixtures and varied with shelf visibility. These are bounded workload observations. A five-minute soak, dedicated save/reopen timings and production memory stability remain unverified. [GetGuiResources](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguiresources)

## Q — WPF usability and accessibility

The main window exposes normal actions directly and collapses the prior replay, fixtures and export under Diagnostics. Native controls retain keyboard traversal, access keys, named image/viewer controls, system colors and live status regions. The private test exercises real WPF windows and registered shortcuts. Narrator, high-contrast and final physical usability studies remain open.

## R — Debug and Release

Locked restore succeeded with the pinned x64 SDK. Debug and Release each built without warnings or errors.

## S — Core tests

Debug and Release each passed 82 core tests, with zero failures and skips. New deterministic evidence covers target removal, resize denial while the opposite owner holds a claim, and a 150% viewport boundary.

## T — Worker and Media Foundation

All 15 worker tests passed with the local controlled video, including native file orientation and reopen. Frame cancellation, malformed input, terminal read failures and diagnostics without image data also passed. The fixture used a writable local temporary path and no camera activation.

## U — Native Windows acceptance

The native runner drives only owned windows on the private desktop. The product integration starts the real Invey controller with an isolated profile. It presses actual Ctrl+Alt chords, verifies the fixture owns the conflict, checks hidden HWND visibility and an independent click, cancels a native capture, and restarts with the remapped chord. The Workspace case drives the native mouse route while its separate Ghost stream stays logical. Run `20261106T184936533` passed all six isolated Pocket and product cases, including 30 two-target trials, separate release in both orders, native resize and closing during two claims. Pure reducer coverage verifies target deletion isolation. The final short suite, seed 73021, used code commit `b33c85683ad047a42211ea402fe9315137a603a1`. Run `20261106T185454130-355642fb0d8347aea5b12702b197fd3a` recorded 36 PASS, zero FAIL and one BLOCKED for Explorer Alt-Tab and tray. It repeated all 30 Workspace trials and covered the recorded F06/F07, native media, A2 containment, capture and shortcut tests.

## V — Failure injection and retained attempts

- `20261106T184108225` timed out before the third capture selector appeared. The fixture switched to clicking the enabled native Capture button and waiting for capture state.
- `20261106T184223068` failed a small black crop assertion. The fixture added an observable return-to-selection checkpoint between previews.
- `20261106T184333574` passed capture, restart and shortcuts. It reproduced the historical contaminated band and timed out at the Workspace resize accelerator. The next fixture targets the real resize button by observed position.
- `20261106T184645350` verified four clean historical bands after the transition fix, the 45-second card profile and shortcut rollback. Workspace accepted both claims and moved the Ghost target, but the first native drag move was not observed by the board. The next build listens for captured preview move events before control-level event handling.
- The first full run, `20261106T185110092`, had a colored 462 × 192 preview of the intended large black region after tracking tests. The monitored historical and bottom bands stayed clean. The exact origin of this second artifact was inconclusive. The follow-up disabled transitions on more Invey windows, added an independent large-band and selected-crop probe, and retained the same crop coordinates. The final full run passed.
- The documentation validator initially failed because PowerShell would not run the unsigned research-sync script. The direct mirror copy then exposed a link whose relative target worked in only one location. Removing the mirror-sensitive link and copying again passed both validators without changing execution policy.
- The native suite retains storage locks, calibration faults, late worker outcomes and contained UIA reliability cases. The new shortcut fixture locks its settings file while testing rollback.

## W — Desktop and process cleanup

An independent audit after the final run found the Default Windows desktop active, zero project test or worker processes and zero temporary Pocket fixture folders. The normal and legacy profiles still did not exist. Camera-off and all five camera worker exits were verified separately.

## X — Open issues

The diagnostic start-budget overrun is a release-blocking test execution failure and is documented above. Physical mixed DPI, Explorer tray/Alt-Tab, screen-reader and high-contrast checks remain open. The shorter synthetic resource workload does not establish sustained production performance or leak freedom. Full Workspace layout persistence is outside this controlled slice.

## Y — Acceptance gates

Gate A physical input and Explorer acceptance remain open. B1 has new live Ready evidence with the limitations in sections M–O. Gate C advances through camera-off shelf, keyboard and Workspace integration. Full Gate C and Gate D remain open. No production Ghost gesture actuation or external app control was introduced.

## Z — GitHub verification

[PR #4](https://github.com/yayatidattayt/Invey/pull/4) targets PR #3. The tested code is `b33c85683ad047a42211ea402fe9315137a603a1`. Its hosted run and the review's exact current-head verification are linked here. [Code verification](https://github.com/yayatidattayt/Invey/actions/runs/42275600053) · [Current PR checks](https://github.com/yayatidattayt/Invey/pull/4/checks)

## AA — Stacked review sequence

Review PR #1 for calibration and its physical boundaries, then PR #2 for the capture and storage foundation, then PR #3 for identity, migration and floating cards. Review this branch after the three dependencies. GitHub reported the parents mergeable at recovery and returned no submitted reviews or inline review threads for the three parents. The open acceptance gates remain release conditions regardless of clean CI. No parent was merged or closed.

## AB — Next milestone

Review the diagnostic budget containment and close the remaining controlled and physical F06/F07 acceptance questions with independent observation. Then complete physical Gate A and broader C/D usability and stability evidence in dependency order.
