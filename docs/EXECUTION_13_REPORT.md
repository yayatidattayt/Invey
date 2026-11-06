# Invey Execution 13 — Pocket capture and B1 review

## A — Recovery summary

Recovered the staged implementation in the original checkout on `codex/execution-13-pocket-capture`. Its local HEAD was `48f5449a0af1b40681eee40fa056b4491cc418a4`, with 19 staged files, no unstaged or untracked changes, and one worktree. Preserved the exact index in local checkpoint `4239805459b7f329718248bf584f9a0c293ba61b` before editing. The checkpoint contains source and documentation only. GitHub confirmed PR #1 was open at the branch base, and no remote Pocket branch existed.

## B — Work completed before interruption

PR #1's CI-only change pinned official Checkout v5.0.0, Setup Python v6.0.0 and Upload Artifact v6.0.0 with Node 24. A checksum-pinned encoder generates six synthetic H.264 frames with orientation markers and deletes the MP4 after the worker suite. The hosted native file-reader case exercises conversion and reopening. Pocket's existing staged work supplied the selector, storage and reference viewer. [PR #1 CI](https://github.com/yayatidattayt/Invey/actions/runs/42189993965)

## C — Interrupted regression

Recovered the original combined result from `artifacts/automated-acceptance/20261106T090350148-529637d7c8bd49a7ab99e9ef7a765bab/results.json` (local only). It finished with 34 passes, zero failures and one blocked Explorer case. It preceded the mandatory window exclusion guard, so the resume ran fresh verification on the final source. The resumed checkout had no lingering Pocket app or runner processes.

## D — Capture implementation

Pocket opens from the main Invey window or tray. The user chooses a physical display. Capture stops owned replay and tracking, hides Invey surfaces, checks the active input desktop, synchronizes with the compositor, and acquires one bounded GDI snapshot before showing the WPF selector. It aborts if the monitor changes, Windows cannot exclude an owned surface, or the input desktop no longer matches. Preview and save recheck display identity and DPI. [Microsoft BitBlt documentation](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-bitblt)

## E — 125% DPI crop repair

The frozen raster-to-viewport transform maps WPF DIPs to display-local physical pixels. The left and top edges round down, and the right and bottom edges round up as exclusive boundaries. Values within floating-point error of an exact pixel snap to the integer. Clamping prevents crossing the selected display, and the resumed code rejects a rectangle that collapses after snapping. Pure tests cover exact and fractional boundaries, clipping, negative desktop origins, a single pixel at the far edge, and 100%, 125% and 150% scales. The real harness compares the selected bounds against independently read cursor endpoints.

## F — Window cloaking and restoration

Only the WPF windows owned by Invey are cloaked and hidden. The isolated test leaves its known synthetic source window visible. Two compositor synchronization points and native visibility checks precede acquisition. Restoration runs in a finally block after success, Escape or failure, restores each surviving window independently, and returns focus to Pocket. It tries uncloaking again if the first restoration attempt fails. A persistent failure disables capture and raises a critical visible warning in normal use. Exit requested before the selector opens now abandons the operation after the current acquisition step unwinds. Exit during selection cancels the selector, and exit during a confirmed save waits for it.

## G — Preview and cancellation

The selector uses a frozen display and a separate cropped preview. The user can drag, choose a keyboard region, adjust it with arrows, resize with Shift and an arrow, or press Escape. The native fixture tests invalid selection, blank rejection, preview, confirmation, cancellation, and focus return. It also locks the library manifest during a real Save and verifies the previous library remains intact.

## H — Storage implementation

Pocket stores a versioned manifest, a backup index, and GUID-named PNG assets under `%LOCALAPPDATA%\PocketDualGhost`. Asset dimensions and sizes are bounded. The writer flushes and verifies the approved asset before publishing the index and atomically replaces an existing index with a backup. An interrupted write can leave an approved unindexed asset, which is recoverable read-only. Retrying the same ID is idempotent. Invalid and future schemas are preserved. Missing or damaged PNGs are isolated, and linked storage paths are rejected. Tests use independent directories and cover ten writes, restart, blocked asset paths, interrupted manifest replacement, incomplete temporary files and corruption.

## I — Pocket library and viewer

The workspace lists actual snapshots, newest first. Open loads the PNG off the UI thread and displays it in an ordinary movable WPF window. Hide returns to the library. A fresh application process reopened the same snapshot. A missing or corrupt item has an explicit failure message. Pinning, rename, reorder, delete and undo are outside this slice.

## J — Privacy

The unconfirmed screen buffer is volatile. The preview crop has its own pixel buffer, and only the approved crop enters storage. No camera, screen-capture network path, clipboard or telemetry is involved. Capture is bounded to 16,777,216 pixels and 8,192 pixels per axis. Tests compare known fixture colors before saving and remove synthetic images afterward. Account permissions protect local PNGs.

## K — Debug and Release

Locked solution restore succeeded on pinned SDK 10.0.403. Debug and Release x64 both built with zero warnings and zero errors. The commands were `scripts/dotnet.ps1` with `restore Pocket.Dual.Ghost.sln --locked-mode -p:Platform=x64`, then `build Pocket.Dual.Ghost.sln -c <configuration> -p:Platform=x64 --no-restore`.

## L — Core and worker tests

Initial builds passed 66 core tests per configuration after correcting WPF and GDI type errors. The resumed builds passed 70 core tests per configuration with zero failures or skips, including the exact-boundary and obstructed-asset assertions. Results are in `artifacts/execution-13/resume-Debug.trx` and `resume-Release.trx` (local only). All 15 worker tests passed with zero skips using a generated synthetic video. It exercised real Media Foundation conversion and reopening without camera activation.

## M — Native Pocket tests

The first passing targeted run saved a 230 × 144 crop at 125% DPI, reopened it across an application restart, rejected black, and confirmed Escape and exit cancellation. Evidence is in `artifacts/automated-acceptance/20261106T090235551-428bac7313c24fc0a9e56ab4b8874344/results.json` (local only).

The resumed Pocket-only run passed four cases at the same DPI and crop size. The locked manifest produced a real WPF save error, left the existing manifest byte-identical with one PNG, and restored focus and the overlay after Escape. The fresh-process check reopened the same pixels and closed during a new unconfirmed capture without creating a second PNG. Results are in `artifacts/automated-acceptance/20261106T125110790-7f7b7ebe39274dc1bb229e823cc075c2/results.json` (local only).

## N — Combined Windows regression

The earlier recorded run passed 34 cases and blocked Explorer. Early native failures are preserved under `artifacts/automated-acceptance/` with IDs beginning `20261106T085620652`, `20261106T085802945`, `20261106T085855195`, `20261106T090019084` and `20261106T090148901`. They showed that Hide and compositor synchronization could still leave stale pixels, an initial DPI notification could close the selector, and integer edges could expand by one pixel. Explicit compositor cloaking, stable DPI checks and the edge snap address those observations.

The resumed combined run with seed 73021 passed 34 cases with zero failures and one blocked Explorer and tray case. It included cursor independence, Dual release, UIA containment, recorded tracking and calibration, Pocket capture, restart, locked-library failure, and teardown. Results are in `artifacts/automated-acceptance/20261106T125115050-db17a20b80ce41a3bc82c608e91412ae/results.json` (local only).

## O — Resource and desktop cleanup

The runner switches to an owned private desktop, checks process identity before input, confirms owned window and process exit, and removes generated crop files. The independent post-run audit verified the Default input desktop, zero scoped test processes, and removal of the generated Pocket store. The full run also verified exit with the recorded calibration worker active.

## P — GitHub CI

PR #1's verified CI-only head is `48f5449a0af1b40681eee40fa056b4491cc418a4`. The Pocket workflow status is recorded after publication. [PR #1 CI](https://github.com/yayatidattayt/Invey/actions/runs/42189993965)

## Q — Pull request

Pocket uses `codex/execution-13-pocket-capture` stacked on `codex/execution-11-calibration-ci`. Its review diff is limited to Pocket and its supporting tests and documentation.

## R — Remaining defects and evidence gaps

- P2 evidence gap. Real mixed-DPI display placement, protected content and secure-desktop behavior require separate verification. Black fixture rejection is a conservative heuristic.
- P2 scope gap. Recovery preserves uncertain data read-only. An explicit library repair and deletion workflow remains to be designed.
- Physical camera Ready, display routing, two-hand behavior and remaining device lifecycle checks require fresh authorized hardware evidence.

## S — Product completeness

Pocket supports capture, confirmation, persistence and retrieval with the webcam off. Dual retains independent logical ownership and fixture-only automation. Ghost retains opt-in local tracking and provisional calibration.

## T — Acceptance status

Controlled F07 is suitable for merge review in PR #1. Live Ready, physical display routing and two-hand verification remain blocked. Prior physical camera acquisition, Stop and Restart evidence remains separate from injected denial, busy and missing-device evidence. Full B1, Gate A physical input and Explorer, and M02 remain open. Pocket has vertical-slice evidence for C01–C06 and C11. Full Gate C and production gesture actuation remain later work.

## U — Next engineering milestone

Add bounded library management and persistent viewer placement and pinning. Review physical B1 independently before granting Ghost authority to manipulate Pocket references.
