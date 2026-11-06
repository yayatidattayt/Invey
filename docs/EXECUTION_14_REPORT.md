# Invey Execution 14 — desktop identity and floating Pocket cards

## A — Executive result

Invey builds as `Invey.exe` from `Invey.sln`. This execution adds profile migration, independently owned floating reference cards and persistent library edits. The first visible hardware session reached live calibration Ready and the camera released cleanly after both sessions.

## B — Repository recovery

Both parent PRs were open. The clean checkout at PR #2 head `3309cd3dd88369534278dbde684c90ac6bf6a388` was preserved at `refs/checkpoints/execution-14-start`. Work continued on `codex/execution-14-invey-cards-migration`. The active checkout remains in place because this Codex session, the local SDK and model paths, and prior evidence references use it. At a handoff, verify that the branch is clean and published, close Invey and all owned workers, confirm one registered worktree, then move the existing checkout as a unit. Reopen it under the new workspace identity and repeat the locked build and fixture discovery. Preserve the checkpoint and ignored local evidence. The directory move is deferred.

## C — Invey identity migration

| Surface | Previous | Current and compatibility |
| --- | --- | --- |
| Solution | Pocket.Dual.Ghost.sln | Invey.sln. Project GUIDs preserved. |
| Desktop assembly and apphost | Pocket.App.dll / Pocket.App.exe | Invey.dll / Invey.exe. The worker's existing PID-bound fixture check accepts the two exact process names. |
| Project and XAML namespace | Pocket.App | Retained internally so generated WPF and resources keep their identifiers. |
| Desktop shell identity | Implicit | Explicit Invey.Desktop before window creation. |
| Profile | PocketDualGhost | Invey. Verified legacy copy, original retained, separate existing destination wins. |
| Instance coordination | Local Pocket.Ghost mutex and reopen event | Kept as the cross-name compatibility lock in the same Windows session. |
| Camera diagnostics | GHOST_CAMERA_DIAGNOSTICS | INVEY_CAMERA_DIAGNOSTICS, with the old name accepted as fallback. |
| Worker protocol and test IDs | Existing Ghost tracking and fixture names | Kept as subsystem/protocol identifiers. |
| Subsystems | Pocket, Dual, Ghost | Retained. |

## D — Solution and executable

The renamed solution restored from lock files with SDK 10.0.403 and produced the new executable in both configurations. The isolated Pocket workflow ran through the renamed apphost, and the normal Invey window opened the hardware Tracking lab. Existing project paths and subsystem assemblies remain in place.

## E — Windows identity compatibility

Both versions use the shared instance mutex and reopen event to coordinate ownership in the same session. The Windows app manifest and taskbar identity use Invey.Desktop. On the normal desktop, the previous PR #2 executable was rebuilt from a disposable source snapshot. While that executable still owned the session, `Invey.exe` exited as a secondary process with exit code 0. Both versions also passed their own native single-instance smoke checks. The check used an empty local-profile inventory, and neither process created a profile. The tray continues to be owned by the normal app controller. No new installer or startup shortcut was introduced. [Microsoft AppUserModelID API](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-setcurrentprocessexplicitappusermodelid)

## F — User-data migration

Startup resolves `%LOCALAPPDATA%\Invey` while holding the instance lock. With a verified legacy-only profile it copies the active manifest, active and tombstoned PNGs, and calibration settings into a bounded sibling staging directory. The copy is reloaded and every asset is verified against its original manifest hash before a directory rename publishes it. A separate canonical profile takes precedence and the legacy files remain preserved. Failed or interrupted migrations expose the legacy profile read-only. Linked profile directories and settings paths are rejected. Schema 1's display order is preserved while a first edit publishes schema 2. The normal local profile inventory contained neither library nor calibration files, so migration was exercised with isolated fixtures.

## G — Capture correctness

A black preview now carries a blank-or-unavailable warning and requires an explicit checkbox before Save. A Windows acquisition failure remains a separate error path. The region is still captured after excluding Invey windows, uses exclusive right and bottom edges, and writes only the approved crop. The fixture checks actual black and colored PNGs, and the original 230 × 144 crop at 125% DPI remains part of the test. This behavior makes no protected-content bypass claim.

## H — Floating cards

Each opened reference is an independent, ordinarily movable and resizable WPF window. Four cards and a 24-million-pixel aggregate budget bound decoding. Reopening the same reference focuses the existing card. Zoom, Fit and scroll use the already-decoded image. Hiding the library leaves cards visible.

## I — Pinning and restart

Pin sets a card topmost by explicit choice. Manifest placement carries the monitor, normal physical bounds, visibility, pin and zoom. Explicit hide persists invisibility. App exit preserves the visibility of open pinned cards for the next launch. A fresh process only restores visible pinned cards, without deliberately activating them.

## J — Display geometry

Native bounds and work areas are physical pixels under PerMonitorV2. Restore clamps cards to their display work area. A missing display falls back to the primary work area. Negative-origin and missing-display placement have pure geometry coverage. The physical display in the isolated run was at 125% DPI. Mixed-DPI hardware acceptance remains open.

## K — Library management

Rename changes metadata only and validates a 1–120 character title. Move up/down changes the explicit manifest order. Delete removes the active entry and closes its open card after committing a tombstone. Undo restores the same ID, title, order, and placement for ten minutes. Expired tombstones can be purged through a separate explicit confirmation. The 200 active-reference limit remains.

## L — Storage recovery

Repair is a user-initiated action that decodes verified recovered references and preserves the damaged index as a separate checkpoint. The store refuses repair if any known asset cannot be validated or if an unsupported newer format is detected. The new index is committed only after validation. Known-good active data remains available after a locked write, and cleanup targets only the intended expired assets.

## M — Webcam authorization

The creator authorized visible local webcam verification for Execution 14 only, with up to three sessions of at most 25 seconds. Two sessions ran through the real Tracking lab. The diagnostic mode bounded the sessions and emitted aggregate counts and timing. Camera pixels and raw landmark sequences were never persisted or uploaded.

## N — Real webcam tests

| Observation | Session A | Session B |
| --- | --- | --- |
| Converted and accepted | 194 | 205 |
| Hand / partial / ambiguous / empty | 106 / 24 / 7 / 57 | 1 / 0 / 0 / 204 |
| First to last converted frame | 19.19 s | 17.79 s |
| Approximate converted throughput | 10.1 fps | 11.5 fps |
| Inference median / p95 | 18.26 / 19.99 ms | 10.75 / 17.14 ms |
| Ready entries / displayed | 4 / 4 | 0 / 0 |
| Maximum consecutive calibration observations | 26 | 0 |

Session A showed hand detection, stabilization and loss. Host counts distinguished loss, partial, ambiguity, side change and movement, with no frame gap or stale break recorded. Session B reacquired the same selected source. The Mirror control was exercised but there was insufficient sustained hand evidence to verify its physical mapping or handedness. This does not establish physical two-hand accuracy or explain the exact conditions in Execution 10.

## O — Camera cleanup

Session A canceled calibration and Session B stopped explicitly. The worker recorded successful native shutdown both times, and the host confirmed exit without a forced stop. Cleanup took approximately 547 ms and 418 ms respectively. The lab displayed Camera off after both and the diagnostic application exited. Local summaries are under `artifacts/execution-14/live/` (local only).

## P — Debug and Release

Baseline locked restore, both builds and 70 core tests per configuration passed before changes. The rename was built independently before larger changes. Both final builds completed with zero warnings and errors. The pinned `scripts/dotnet.ps1` wrapper built `Invey.sln` in Debug and Release x64 from the locked restore. A restricted compiler invocation was canceled with MSB5021. The rerun completed cleanly in both configurations. A restricted legacy fixture restore also exited before completion, and the rerun restored and built the historical executable.

## Q — Core tests

Debug and Release each passed 80 tests, with zero failures or skips. New tests cover rename and ordering, preserved hashes, durable Undo and expiry, explicit repair, old/new profile conflicts, interrupted and locked migration, migration of tombstones with Undo, corrupt and newer legacy manifests, missing assets, corrupt legacy calibration, and card placement on signed desktop origins.

## R — Worker tests

The baseline and final rerun each passed all 15 worker tests, including native file reading on controlled MP4. An intervening invocation failed the diagnostics test because the process could neither publish the file nor remove its default temporary folder. Setting TMP and TEMP to the writable local evidence directory restored both operations. No worker test or diagnostic was suppressed. Hosted CI uses a generated synthetic media fixture.

## S — Native Windows tests

The first renamed Pocket-only run passed using the Execution 13 suite. The revised suite verifies actual black and colored snapshots, two independent cards, pinning, move/resize, native row selection, rename, order, Delete/Undo, and restart retrieval. Both final targeted runs passed all four cases at 125% DPI, including capture and edits, exit, migration and restart with restored bounds and zoom, and fixture cleanup. The run IDs begin `20261106T134538152` and `20261106T134541968` for seeds 73021 and 73022. The first verified normal-size colored region retained its 230 × 144 dimensions. The tests drive real WPF windows over owned synthetic content on a separate Windows desktop.

## T — Crash and resource review

Cards release decoded images on close, detach display notifications, and save placement via bounded debouncing and an asynchronous serialized writer. Capture retains the existing cloak-and-restore cleanup. Profile staging retains a prior interrupted copy for inspection. No app crash occurred in the final targeted, full regression or startup smoke runs. The normal-desktop audit found the Default input desktop restored, no scoped processes, no synthetic Pocket fixture folders and no new normal profiles. Sustained CPU and memory targets remain unmeasured.

## U — Full regression

The first card build caught a nullable constructor callback and was corrected. The first two private runs reached the real pin path but failed the fixture's list-selection assertion. The second showed focus on the ListBox with its initial row still selected after a single Down key. Using End after confirmed list focus selected the intended second reference. The first corrected targeted run passed. Two later targeted runs beginning `20261106T133655190` and `20261106T134100061` saw a nonblack pixel in the second intended black selection despite sampled stripe anchors matching. The exact origin of that pixel was inconclusive. Run `20261106T134105330` had 32 passes, one blocked Explorer case and one failed Pocket case. The pinned card lost its zoom when the list-focus shortcut was sent and the list never received focus. The revised fixture places the library outside the pinned card, clicks the realized row and verifies selection and foreground, and samples the black stripe in a clear bottom band. Failed and passing attempts are retained locally. Both revised targeted runs passed. The full short run on seed 73021 (`20261106T134545570-89cb3210ea1949bd9ea31f126320b787`) finished with 34 PASS, zero FAIL and one BLOCKED for Explorer on the isolated desktop. This included the recorded F06/F07 controls, A1/A2 fixtures, and Pocket restart and cleanup. Both specification validators passed. Separate normal-desktop smoke runs (`20261106T134859801` and `20261106T134928987`) each passed with 900 observations, retained fixture focus and cursor position, and destroyed the overlay handle. These are software-only native checks.

## V — GitHub CI

The published implementation at `d1a2223bf9706a26ab3045ee02734cf4c504e53a` passed the pinned SDK installation, locked restore, Debug and Release build and tests, and native-file worker tests in hosted Windows. The follow-up adds four deterministic migration fault cases and this report. The PR Checks tab records the verification for its current head. [Implementation CI run](https://github.com/yayatidattayt/Invey/actions/runs/42228795362) · [PR checks](https://github.com/yayatidattayt/Invey/pull/3/checks)

## W — Pull request

[PR #3](https://github.com/yayatidattayt/Invey/pull/3) targets PR #2's feature branch. Parent PRs #1 and #2 are open at the same heads observed at recovery. The Execution 14 branch is ready for review of the verified slice. Acceptance of the open physical and broader product gates remains separate.

## X — Defect ledger

- P2 product defect repaired. All-black pixels were previously rejected as an acquisition error.
- P2 build error repaired. A constructor callback referenced the card window before definite assignment.
- P2 test defect repaired. The keyboard fixture initially focused a list without selecting the intended row.
- Physical handedness and mirror movement were inconclusive. Mixed DPI and isolated Explorer shell acceptance were blocked. Protected content, real display disconnect, normal-desktop use with pinned cards, and long-session card resource profiling were not run. Missing, corrupt or locked storage and display changes have deterministic or isolated coverage as applicable. Dedicated migration tests for a real denied ACL and every transaction boundary remain open.

## Y — Acceptance gates

Gate A physical input and Explorer remain open. A2 remains fixture-scoped and M02 awaits disposition. B1 now has verified live Ready and restart in this session, while physical hand selection, display routing and outstanding hardware faults still need acceptance. Pocket has new controlled Gate C evidence. Full Gate C and Gate D remain open.

## Z — Next execution

Review and complete outstanding physical B1 and broader Gate C and D acceptance, including mixed-DPI placement and a longer multi-card resource profile. Production Ghost gesture actuation stays a separate dependency-gated milestone.
