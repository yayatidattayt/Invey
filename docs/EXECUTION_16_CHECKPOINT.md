# Historical Execution 16 interruption checkpoint — 2026-11-08

The continuation has resumed. This file preserves the earlier interruption, not the current budget or final test state. See codex/HANDOFF.md and docs/EXECUTION_16_REPORT.md. The durable ledger has since been explicitly amended to version2, preserving all earlier charges.

Status: IN PROGRESS, user stopped desktop control with physical Escape. No camera session is active. Do not treat this checkpoint as the final execution acceptance report.

## User request and physical observations

The user wants to see a real local camera image with finger landmarks before attempting the separate cold-start trial. The user authorizes display, not recording or upload. Never capture a screenshot of the live image through the agent tools.

The user reports moving right moved the marker left with Mirror on; with Mirror off the marker moved right. Session one involved movement and pinch attempts; its position spread cannot be called stationary jitter. Specific selected-hand validation and isolated fingertip cold start remain unproven.

## Repository

Base PR #4 head ca400d966ec87e0c8ec638803fc8d5afa801bd5b. Branch codex/execution-16-precision-finger-tracking. Recovery ref refs/checkpoints/execution-16-start. Earlier PRs #1–#4 were open and unmerged at the last check. Preserve all uncommitted work. No merge, no OS pointer actuation, no B2.

## Durable hardware budget — preserve exactly

Existing local-only ledger: artifacts/execution-16/camera-budget.json.

Two released reservations:
- 900c6717-6185-48c9-ad22-94ff4e733994: 81609 ms charged.
- e3341955-746b-41a3-b712-3c5f9ab262d6: 72375 ms charged.

Limits: six starts, 120000 ms per reservation, 720000 ms cumulative; independent guardian upper bound 118 seconds. Do not delete or reset ledger. An unclean pending reservation fails closed and receives no refund. No third session yet.

Restart the visible Release Invey.exe only after final preview verification. Reuse INVEY_CAMERA_EXECUTION=execution-16, the same absolute INVEY_CAMERA_BUDGET path, and INVEY_CAMERA_DIAGNOSTICS pointing to artifacts/execution-16/live. Set DOTNET_ROOT and DOTNET_ROOT_X64 to project .tools/dotnet. Never silently start without the guardian configuration.

## Implemented

- CameraBudget durable strict ledger plus separate guardian process and kill-on-close worker job. Reservation persists before camera start; independent timer, parent death, guardian death, PID ownership and confirmed native release/exit checks.
- Six-permit/seventh-denied and corrupt/missing/deadline/interruption negative controls passed with synthetic non-camera workers.
- Sparse v2 partial observations preserve valid model points when wrist is outside frame; complete v1 remains supported.
- Per-finger state, model-point-only experimental index preview, four-fresh-observation rearming, adaptive One Euro smoothing and safe loss/ambiguity reset.
- Existing practice still requires full calibration readiness; sparse observations have no action authority.
- Aggregate diagnostics only; no raw camera images or landmark histories saved.
- User-requested live RGB preview now has a separate current-user named pipe and worker PID verification, <=640x360 bounded thumbnails, one replaceable pending frame, memory-only WPF bitmap, and skeleton only when observation and image sequence match. Stops clear the image. Optional checkbox defaults off, selected before Enable. Frames older than 500 ms disappear. Mirror applies equally to image and landmarks.
- Synthetic preview path exercises real Python pipe and WPF rendering. Added header validation controls (malformed magic/sequence/dimensions/length) plus cancellation before connection.

## Verification so far

Most recent pre-preview core suite: 98 passed (artifacts/execution-16/preview-core.trx).
Earlier independent guardian pass: artifacts/automated-acceptance/20261108T100503490-d4383585fd394064b851faa4b119addf/results.json.
Partial marker camera-free pass: 20261108T102021668-cfde49750ce44657b81d3d1a85feecfe.
Initial live RGB synthetic Windows test also passed (latest run around 10:41 UTC); it checked image assigned, partial pointer safety and Stop cleanup. Strengthened exact skeleton and malformed-header controls were added afterward and still require rerun.

Python preview-worker-tests-rerun.log: 16 passed, one pre-existing native-MP4 conditional test skipped because GHOST_NATIVE_VIDEO_FIXTURE was not set. Run it with the existing local generated fixture for final evidence. The first preview worker run encountered sandbox temp-directory denial; preserved in preview-worker-tests.log. Rerun used workspace TEMP/TMP. No test removed.

Build invocation errors were preserved: direct dotnet without local home hit denied global .dotnet; Pocket.sln no longer exists (use Invey.sln); passing colon switches through powershell -File lost parameter structure. Use direct project dotnet with local environment and quoted switches.

preview-release-build-rerun.log: Release succeeded, zero warnings/errors.
preview-release-final.log: newly added test had duplicate local variable protocol because bulk replacement affected both tracking-close sites. Duplicate block removed before checkpoint. This correction has NOT yet been rebuilt/tested. Do that first without camera activation.

Earlier WPF failures remain in artifacts: sparse snapshot indexing; private-desktop focus; NaN Canvas.Left JSON serialization; incorrect missing-index wait. Each was repaired and rerun; include them honestly in final report.

## Real camera evidence

Session one: 2018 accepted; 1491 full, 70 partial, 9 ambiguous, 448 empty; partial index in frame 69; 26 UI Ready entries. Source-to-result median/p95 88.174/111.042 ms.
Session two: 1713 accepted; 872 full, 201 partial, 12 ambiguous, 628 empty; partial index in frame 185; UI Ready 18; 140 UI partial-pointer frames. Source-to-result median/p95 85.34/119.42 ms.
Both explicit Stops, worker exited, forced stop false, native release true, guardian released the reservation. Evidence under artifacts/execution-16/live. Mixed-motion spread is not stationary jitter or ground-truth accuracy. Full and partial hand model observations do not prove isolated fingertip acquisition.

## Research results already measured

Pinned MediaPipe 0.10.32, existing model SHA256 fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1.
benchmark_fingers.py results in artifacts/execution-16/model-benchmark.json. Existing attributed still cropped in memory, not a separately labeled finger-only dataset.
At detector threshold .5 and .3, cold IMAGE crop detections 0/24 each; VIDEO full warmup 6 then 18 cropped inputs had six full detections and no cropped detections each. Lower threshold did not help; preserve current production backend.
Synthetic OpenCV flow four corners moved four pixels: mean 4.000011 px, one cold call 26.9 ms; continuity candidate only.
Synthetic One Euro RMS .000748172 normalized, raw .008; step 90% 80 ms versus fixed1.5Hz 260 ms. No physical latency claim.

Primary sources consulted: official MediaPipe Hand Landmarker docs and google-ai-edge/mediapipe issue5667; MMPose repository/model zoo/license; official OpenCV optical-flow docs; gery.casiez.net/1euro; Microsoft job and named-pipe docs. No new alternative model weights downloaded.

## Remaining authorized work

1. Build latest source, run strengthened camera-free RGB/skeleton/protocol controls and independent guardian regression. Camera remains off during implementation.
2. When desktop control resumes and the participant is ready, open opt-in live view using SAME remaining ledger. Let participant see only-fingertip framing before fresh Enable. No additional image persistence. Ask for physical observations when necessary. Use remaining sessions only when useful.
3. Run final Debug/Release core suites, Python suite with generated native MP4, full bounded isolated Windows acceptance, Pocket and reliability regression. Preserve every failure/rerun.
4. Write docs/FINGER_TRACKING_RESEARCH.md (A–O) and docs/EXECUTION_16_REPORT.md (31 requested sections), plus required traceability/research/handoff updates and validation.
5. Inspect diff for privacy/security; update CI base branch trigger for PR stacked on PR4; commit/push focused branch; create review PR; wait for actual Actions on exact final commit. Do not merge.
6. Audit final independent camera/worker/guardian cleanup. Gate A/M02/B1 physical gaps remain explicit. Do not describe B1 or finger-only research as accepted without evidence.
