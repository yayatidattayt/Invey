# Invey Execution 11 — F07 calibration and Windows verification

## A — Repository baseline

Fetched `origin` from the existing Invey checkout. The working tree was clean, local `main` and fetched `origin/main` both pointed at `04a147eedca189841bc807de8a093ac8853daa8c`, and there were zero commits ahead or behind. Created `codex/execution-11-calibration-ci`. The existing isolated harness, Windows x64 SDK 10.0.403 and local Python 3.12.10 / MediaPipe 0.10.32 environment were present.

## B — Build recovery

The original publication check queried the system host, which had no matching SDK. The project-local host had the exact pin. The cached Windows x64 archive matched the SHA-512 in Microsoft's release metadata. The installer now selects the exact `global.json` version from the official archive list, validates the HTTPS asset identity and verifies its SHA-512 before extraction. Roll-forward is disabled and the wrapper resolves the project root before invoking the pinned SDK. [Microsoft release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)

The installer also extracted the verified cached archive into a fresh project-local test directory and reported 10.0.403. Locked restore and Debug/Release builds passed. Both baseline builds had zero warnings/errors and 54 core passes. A test command that passed the solution-level platform property to the test project looked in `bin/x64` instead of the solution's actual `bin/Debug` output. The verified test command uses the default project output path.

## C — GitHub CI

The Windows x64 workflow uses a read-only token, commit-pinned checkout/Python/artifact actions and no checkout credential persistence. It checks the SDK archive, restores locked dependencies, builds and tests both configurations and runs deterministic worker tests with a checksum-pinned NumPy wheel. It uploads core TRX results. The native file-reader test reports a skip when the local MP4 fixture is absent. See [workflow](../.github/workflows/windows.yml) and [evidence policy](../VERIFICATION.md).

The first hosted run passed SDK setup, locked restore, both builds and core tests, deterministic worker tests and TRX upload. GitHub published the `windows-core-test-results` artifact. The local MP4 case remains a separately reported fixture-dependent test. [Windows verification run](https://github.com/yayatidattayt/Invey/actions/runs/42174465421)

## D — Engineering defects

| Severity | Observed issue | Cause and disposition |
| --- | --- | --- |
| P1 | The UI could miss a loss between two render polls | The latest-only snapshot overwrote the event. A controlled test now coalesces ambiguous and valid frames and confirms readiness restarts. |
| P1 | First timed practice regression released immediately after opening | The release timer inherited pinch dwell. Reset on transition to Held. The first failing TRX is retained. |
| P2 | SDK bootstrap selected a moving release and public build instructions invoked the wrapper incorrectly | Exact archive lookup, strict resolution and verified PowerShell argument form. |
| P2 | Private evidence links were broken on GitHub | Converted links to labeled local-only paths and kept original reported results. |
| P2 | Initial build hit ambiguous `Path` and a worker assertion still expected partial for two hands | Qualified the type and updated the assertion to require the distinct ambiguity outcome. |
| P2 | The first restart integration test failed before practice started | The assertion tried to decode an absent worker PID as a number. It now requires JSON null for a source that has never started. |

## E — Repairs

Core calibration owns transforms, continuity, practice and numeric schema validation. WPF adds display selection, a mapped target, feedback, settings, Skip/Cancel and Repeat in the existing panel. The worker accepts up to two detections and emits an ambiguity marker with an empty landmark payload when it detects more than one. The isolated harness drives actual controls with bounded recorded or synthetic sources.

## F — Live Ready investigation

Execution 10 accepted 246 results, including 149 tracked, 7 partial and 90 empty. The panel was observed in Waiting and OutsideRegion. These observations establish tracking loss/partial results and out-of-region feedback during the run, but the aggregate counts cannot establish where they fell in the six-observation window. There is no captured per-frame calibration continuity record for that run. The precise cause of missing live Ready remains unresolved.

Execution 11 preserved the six-observation rule and the 220 ms / .14 continuity limits. TrackingSession carries a monotonic continuity generation through the latest-only boundary, with reasons for loss, partial/ambiguous results, a capture gap, side/frame changes, jumps and expiry. Calibration distinguishes observed resets, fresh samples, current and maximum runs, and Ready entries versus actual status updates. Optional bounded diagnostics write counts only. Counts at the UI boundary describe observed events, so several intervening breaks can appear as the last reason. Recorded WPF reached displayed Ready.

## G — Display mapping

The lab enumerates physical screen bounds and obtains effective DPI using a hidden non-activating PerMonitorV2 window on each screen. From the mirrored or unmirrored working region, normalize each axis, apply a reversible bounded sensitivity curve, and map endpoints to the first and last pixels of the selected display. Convert the display-local position to DIPs with `96 / DPI`; add the signed desktop origin only when producing physical pixels. Camera and display diagrams letterbox independently. Display and DPI notifications refresh the selected geometry and invalidate the previous calibration when it no longer matches. [GetDpiForWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow)

Deterministic tests exercised a rectangle with negative X/Y, 125% DPI, mirrored and unmirrored cases, inverse round trips and invalid inputs. The WPF test verified a mapped physical point using the available local display. Physical mixed-DPI, disconnect and fingertip rectangle-route checks remain open.

## H — Mirror and hand selection

Preview mirroring affects X and display mapping. Selection uses the observation label, and unknown labels and detected ambiguity disarm. The pinned Tasks runtime classified an original recorded composite Right and its horizontal flip Left. Duplicate-photo composites did not yield two detections in the trial. These were camera-free model probes. The actual camera's physical left/right interpretation remains open, so the application makes no source-specific label correction. The two-hand policy passed deterministic worker and reducer tests. Identity continuity for an undetected second hand is limited by detector output.

## I — Calibration preferences

Version 1 stores display number, bounds and DPI, mirror as 0/1, hand as 0–2, region inset and sensitivity. It uses a bounded JSON read and a same-directory temporary file flushed before atomic replacement. Invalid schema/geometry defaults safely and is reported. A missing saved display requires a new explicit selection. Camera choice remains session-local. No source activation, images or landmark history is stored.

## J — Practice experience

The mapped marker moves inside a diagram of the selected display. Open-hand dwell enables practice. Over the local target, a thumb/index distance divided by palm width must remain at or below .28 for 100 ms. Opening to .42 or greater for 80 ms releases. Distances account for frame aspect and reject near-zero palm widths. Freshness, sequence continuity and detection loss gate practice. Ambiguity or tracking failure cancels a hold and requires a new open-hand dwell. Practice runs entirely in the calibration panel.

## K — UI quality

Landmark-only camera geometry and a display diagram use restrained system brushes, labeled controls, access keys and live-region status. The panel opens with mirror focus and offers a visible Cancel/Skip path that stops the source. Marker updates are direct, with no animated transition. The test desktop exercises access keys and visible status. Screen-reader and physical mixed-DPI usability remain open.

## L — Automated tests

- Locked restore, zero-warning Debug/Release builds and 60 core passes per configuration after the practice fix. The initial Debug test failed one assertion (`artifacts/execution-11/Debug.trx`, local only). Final results are `artifacts/execution-11/final-*.trx` (local only).
- 14 worker tests passed with `GHOST_NATIVE_VIDEO_FIXTURE` pointing at the controlled local MP4. The first run expected the old partial marker and skipped the optional MP4; the updated test distinguishes ambiguity.
- Isolated tracking run passed seven cases, including real recorded inference, displayed Ready, mirrored mapped point, settings reload in a new lab window, Cancel/Skip, native file-reader, controlled denial and cleanup (local only: `artifacts/automated-acceptance/20261106T061505415-60c1634732834dae9cee82087c345a49/results.json`).
- The first combined run had 28 passes, one blocked Explorer case and the restart-test assertion failure (local only: `artifacts/automated-acceptance/20261106T061742449-333f119baea34708be8cf46b4a1e80b9/results.json`).
- After correcting the assertion, all eight targeted cases passed. The fresh app process started with no worker, reloaded the saved mirror choice, moved keyboard focus to hand selection with Tab, and demonstrated Held, Released, ambiguity cancellation and Repeat. The practice input was a bounded synthetic landmark stream. Both owned hosts and their workers exited (local only: `artifacts/automated-acceptance/20261106T062105063-fa9c704ee8774121a68dff84e6343559/results.json`).
- The final combined short run, seed 73021, passed 29 cases with zero failures and one blocked Explorer case. It included all three cursor independence controls, click/drag/typing, Dual release, A2 uncertainty recovery, recorded/native-file tracking, practice, and cleanup. The available local display was 1920 × 1200 at 120 DPI (125%). Results are in `artifacts/automated-acceptance/20261106T062226084-138248d4029842cdbf2b5f42a84b27c7/results.json` (local only).

## M — Hardware evidence

Execution 10's webcam authorization was specific to its session. Execution 11 used recorded, synthetic and controlled native-file sources. Live Ready, real disconnect/denial, physical orientation, physical two-hand behavior and hardware route/practice remain blocked pending explicit device activation and an available test participant.

## N — Reliability

Loss, expiry, incompatible display geometry, Stop and worker exit clear logical practice state. Stop/Restart use the established worker ownership and teardown. The independent post-run check found the Default input desktop and zero remaining project test processes. The private desktop has no eligible Explorer shell. A shell-enabled test desktop or VM with Explorer and separately coordinated physical input is needed for those cases.

## O — Regression comparison

The Execution 10 baseline was 54 core tests per configuration, 14 worker tests and up to 28 native passes in a combined run. Execution 11 reached 60, 14 and 29 respectively. It extends recorded integration to inspect real Ready text, mapped coordinates and saved settings.

## P — Product completeness

Pocket still provides a passive panel and simulated reference action. Dual retains independent logical interaction and fixture-scoped automation. Ghost provides explicit local tracking and calibration practice in the lab. Persistent Pocket excerpts and production gesture actuation are later milestones.

## Q — Canonical gates

| Gate | Status |
| --- | --- |
| A1 | Physical input and Explorer acceptance blocked. |
| A2 | Fixture-only provisional. |
| M02 | Ordinary-application control deferral proposed, pending formal disposition. |
| F06 | Prior live acquisition/Stop/Restart established; device denial/disconnect and formal acceptance open. |
| F07 | Controlled mapping, preferences and practice implementation. Physical route and hand verification open. |
| B1 | Review open. |

## R — GitHub delivery

Feature branch `codex/execution-11-calibration-ci` targets `main`. Implementation commit `4092c008f6a8141697e246961390e1d670c871dd` passed the hosted Windows workflow. This report records that implementation run in a documentation follow-up. The pull request is open for review. [Pull request #1](https://github.com/yayatidattayt/Invey/pull/1) · [Passing Windows run](https://github.com/yayatidattayt/Invey/actions/runs/42174465421)

## S — Remaining risks

- P1: Live Ready, real device-loss recovery and physical hand identity need bounded hardware verification.
- P1: Physical input and Explorer need a shell-enabled authorized test environment.
- P2: Monitor numbering can change with topology. Exact stored geometry must match or the user must reselect. Physical mixed-DPI behavior needs validation.
- P2: The two-detection model can miss hands. Simulated ambiguity handling does not measure physical detector reliability or the new configuration's live performance.

## T — Next milestone

Review F06 and F07 with separate authorized hardware trials, including live reset counts, a rectangle route at 100% and 125%, two-hand entry/exit, and camera-off fallback. Review B1 alongside A1 and M02 before any B2 actuation work.
