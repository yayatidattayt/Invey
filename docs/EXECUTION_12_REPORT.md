# Invey Execution 12 — B1 reliability review

## A — Repository and PR

Fetched the existing feature branch and PR ref. Both matched reviewed head `2a5f5f0a901b2a973569f6e090cd16d02572fd00`. PR #1 was open with no review submissions or inline review threads. Its target was `main` at `04a147eedca189841bc807de8a093ac8853daa8c`. The local tree was clean and the reviewed head had passing Windows CI. [Pull request](https://github.com/yayatidattayt/Invey/pull/1) · [Reviewed baseline run](https://github.com/yayatidattayt/Invey/actions/runs/42174655902)

## B — Confirmed findings

- P2 observability: a non-hand result overwritten before the next read was omitted from the host superseded count. The existing continuity generation still invalidated readiness. The new assertion reproduced a count of zero where one ambiguity result was overwritten.
- P2 settings integrity: duplicate mirror keys silently selected the last value. The negative control reproduced a valid load with mirror changed to 1.
- The worker diagnostic schema grouped detected ambiguity with empty results and both diagnostic layers stored a device fingerprint. The UI report used an independent random file name, so it could not be reliably joined with worker teardown evidence.
- Code inspection found a window for old stderr callbacks to write into the next worker's diagnostic fields. No observed UI incident is attributed to that window.

## C — Repairs

The host counts all accepted observation kinds and overwritten results. A separate fixed-size tally records continuity breaks before the UI can coalesce them. Duplicate settings properties now fail validation. Worker, host, and UI diagnostic summaries share the session generation and exclude device identifiers and landmarks. Controller shutdown joins its stderr reader and rejects a buffered message when cancellation is already signaled.

## D — Live Ready diagnosis

The original live check lacks the ordered samples needed to identify the exact Ready blocker. The six-frame readiness rule and thresholds are unchanged. For one explicitly enabled diagnostic session, set `GHOST_CAMERA_DIAGNOSTICS` to an existing local directory before launching Invey. The visible camera session has a 25-second cutoff and supports manual Stop or Cancel.

The worker summary separates converted and submitted frames, emitted hand/partial/ambiguous/empty results and native shutdown outcome. The host summary records decoded observations, accepted and rejected observations, superseded results, per-kind totals, continuity breaks and exit confirmation. The UI summary reports fresh observations reaching calibration, reset reasons (including wrong hand and region exit), Ready entries and status updates, maximum run, and whether the panel ended through Cancel, Skip or source end. Host break counts and UI resets measure different events. Process exit and successful native shutdown are reported separately.

## E — Hardware authorization and evidence

Live ASUS FHD testing was blocked by the lack of a fresh Enable or test-specific authorization. No webcam was activated for this execution. The recorded source supplies real local inference and Ready evidence. Previous physical-camera evidence remains in the historical reports.

## F — Display mapping and DPI

The deterministic B03 rectangle visits all four working-region corners, returns to its start, and independently checks signed physical coordinates and monitor-local DIPs. It covers 96 and 120 DPI, negative desktop origins, inverse round trips, mirror, sensitivity and invalid geometry. The isolated UI uses the available 125% environment. Simulated removal of the selected display goes through the normal display-list reconciliation path and clears the mapped point. Physical rectangle alignment, display unplug and mixed-DPI behavior remain open. Display scale was left as found.

## G — Two-hand behavior

The bounded synthetic landmark test enters a valid hold, completes release, holds again, and observes cancellation when the worker emits ambiguity. Existing core coverage checks session replacement, loss, stale input and coalesced ambiguity. Physical second-hand detection, label interpretation, and the complete physical rearming route remain unverified.

## H — Camera lifecycle and cleanup

Controlled sources cover startup off, explicit enable, injected permission denial/busy/unavailable, worker crash, malformed IPC, stale results and Stop/Restart. No camera source is opened in the injected error cases. The synthetic practice suite covers repeated Stop after Cancel. Exit exercises owned worker and window cleanup.

## I — WPF and settings

Display callbacks are detached on close and queued callbacks now avoid refreshing a closing lab. The selected display is invalidated when it disappears. The atomic settings regression locks the destination against replacement and checks that the previous value remains readable. Corrupt and duplicate-key documents are rejected. Windows device selection is still session-local.

## J — Debug and Release

Pinned SDK 10.0.403 and locked restore passed. Before any edits, Debug and Release each built without warnings and passed 60 core tests. The two new assertions failed on the baseline as intended. The final Debug and Release builds had zero warnings and errors, and each passed 60 core tests. Results are in `artifacts/execution-12/verified-Debug.trx` and `artifacts/execution-12/verified-Release.trx` (local only).

## K — Worker and isolated Windows

All 15 worker tests passed with the controlled native MP4, including the new diagnostic output check. The combined isolated Windows run with seed 73021 passed 31 cases, with zero failures and one Explorer case blocked. It confirmed recorded Ready and mirror, both injected camera errors, synthetic ambiguity cancellation, removal of the selected display, repeated Stop and worker exit. Results are in `artifacts/automated-acceptance/20261106T071922496-1993c92f871944dd8969aed2893c842e/results.json` (local only).

The final tracking-only run passed all 10 cases, including application exit with calibration open and an active worker. An independent check found the Default desktop restored and zero scoped test processes. Results are in `artifacts/automated-acceptance/20261106T072209255-5636e2caca8b4ae399cc7ba9179853ad/results.json` (local only).

## L — CI

The reviewed baseline run passed. GitHub Actions also reported success for implementation commit `e517de35c9c9ede4b6f34f9a8be57653cccce6b2`. This documentation follow-up records that result. [Hosted Windows verification](https://github.com/yayatidattayt/Invey/actions/runs/42180628066)

## M — Failed, blocked and inconclusive cases

- The baseline negative control failed both assertions (`artifacts/execution-12/negative-baseline.trx`, local only).
- The first modified build stopped at CS8602 when a constructor event referenced a field before nullable flow analysis could prove initialization. The handler now reads a fresh controller snapshot.
- The first 15-test worker run could not create the diagnostic output in restricted temporary storage. The same suite passed with normal temporary-file access. No test was skipped to obtain that result.
- Hardware Ready, physical two-hand reliability, physical B03 alignment, actual camera denial/busy/disconnect, and Explorer and physical input acceptance remain blocked or unverified.

## N — F06 and F07 traceability

F06 / B01, B02, B08, B09, B12 and S05 retain the distinction between historical device evidence, controlled fault injection and local native file-reader evidence. F07 / B03, B07 and A11 have controlled geometry and practice evidence. See the [traceability matrix](TRACEABILITY.md).

## O — Remaining gates

Gate A physical input and Explorer remain open. A2 remains fixture-only provisional, and M02 ordinary-application control deferral awaits disposition. F06 still needs the remaining real-device failure cases and F07 still needs physical alignment, hand-label and ambiguity trials. Full B1 remains open.

## P — Recommendation

**READY FOR REVIEW** for the calibration reliability fixes. The isolated suite and hosted Windows workflow passed. Live hardware acceptance remains **BLOCKED** until a new explicit Enable or authorization. Full B1 remains open.

## Q — Next canonical milestone

Run an explicitly authorized bounded F06/F07 hardware session and collect the correlated session summaries. Review the device lifecycle, physical rectangle route and ambiguity behavior against B1 alongside Gate A and M02 before opening the B2 milestone.
