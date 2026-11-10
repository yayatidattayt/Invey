# Execution 22 — Live Ghost software integration

**PARTIAL — software bridge implemented and controlled WPF gesture replay passed; final full native regression BLOCKED. Physical qualification remains BLOCKED.**

## Baseline and architecture audit

Started from `f30d01b06ccfc29022a70e3886c35095c8fad6eb` on `codex/execution-20-interaction-engine`, existing PR #9. GitHub connector verified the open PR head before modification. No parent was merged. Read the active rules, master specification, handoff, Execution20/21 evidence, physical plan, beta guide and QA matrix.

The existing `GhostTrackingMapper` already validates observation context, applies calibration, compatible personal correction, one precision filter and physical display mapping; `MainWindow.DesktopTracking` forwards it through dispatcher-owned `GhostDesktopHost` to the passive WPF ring. That pipeline was reused. Full/partial/visual provenance is retained. Recorded and camera pointer sources cannot arm production commands. Invalid mapping clears the pointer; display changes, source replacement, Stop and loss invalidate ownership. Personal learning and warm visual continuation were preserved.

Baseline core binary: initial 155/156 with `PocketStoreTests.TenConfirmedItemsReloadAndDuplicateConfirmationIsIdempotent` File.Replace failure. Project-local-temp rerun 156/156 (`execution22-baseline.trx`). This recurring local filesystem error is not classified as a reproduced new product defect.

## Gesture repairs and bridge

The previous classifier allowed pinched arrival to confirm, resumed observations after Pause, accumulated dwell over a long observation gap, and accepted distributions based only on their median. These were confirmed by inspection and focused regression tests. It now requires an open neutral observation before pinch, blocks Pause until Begin, rejects gaps over 250 ms, and requires all bounded calibration samples to meet the existing .28 pinch/.42 release separation. Existing 180 ms dwell and hysteresis remain. One sustained pinch emits one Press; release emits once; stale, wrong-hand, ambiguous, partial or visual observations release/disarm and cannot fabricate thumb geometry.

`GhostGestureCoordinator` connects typed proposals to the existing `GhostDesktop` target/reducer route. It binds source session, desktop generation, valid mapping context, selected hand, frame size, target registration/lifetime, reachability, unchanged acquisition geometry, arm state and sequence. Candidate target is captured at onset: entering a target after confirmation cannot retroactively acquire it. Target replacement/closure, context change, loss or stopped pointer cancels. Press/Move/Release use existing card offsets and ownership; the physical cursor is never moved.

Normal coordinator construction grants **no action authority**. `LiveQualified` is always false; `MayExecute` remains false. A separate internal controlled-replay construction exercises the existing Controlled-only desktop command gate. Its application caller is solely the authenticated acceptance host, which validates the named private desktop. No environment flag grants camera production authority. No qualification issuer exists in this build; future activation needs reviewed qualification evidence and explicit Arm. This is a software integration boundary, not physical authorization.

The Tracking lab has a bounded in-memory open → pinch → release lesson with 12 samples per phase, selected-hand/full-landmark/freshness checks, minimum spacing, interruption rejection and no image/history persistence. Its result feeds the normal coordinator for practice proposals after mapped preview. The human-facing lesson itself has not been physically or manually usability-qualified.

## Deterministic and native evidence

- Pinned locked restore passed. Final Debug/Release builds: zero warnings/errors. Final core suites: **167/167 each, zero skips**, `artifacts/execution-22/final3-Debug.trx` and `final3-Release.trx`.
- An intermediate final Debug suite had one migration fixture access-denied result (166/167); `final2-Debug.trx` is preserved. A fresh project-local temporary directory passed the unchanged suite. Do not erase either failure.
- Worker suite: **29/29, no skips**, using controlled video and the pinned local Python runtime. No worker architecture or visual deadline changed.
- Mapper tests cover corners/edges/center, diagonal endpoints, mirroring, negative origins, deterministic 96/120 DPI transforms, frame-size/context invalidation and Stop. Existing compatible-model tests and calibration suites remain. Physical mixed-DPI/display-disconnect behavior remains unqualified.
- Core bridge tests exercise actual registered-target callbacks and reject moved/covered/late-entered targets, wrong hand, context replacement, pinched entry, long gaps, paused input and overlapping calibration. Existing tests retain stale/partial/visual/target-lifetime and simultaneous Dual reducer coverage. Hand rotation/occlusion quality needs physical evidence; software checks cannot infer anatomical accuracy.
- Controlled WPF replay run `20261109T183127593-9520f62a92de4d64873848e1e9e857e5`, seed22003: passed E20 complete route. Synthetic full-hand observations went through the mapper/filter, visible pointer, dwelled classifier, explicit controlled bridge, actual Pocket card acquisition/movement/release, ambiguity cancellation and persisted placement; concurrent Mouse/Ghost Workspace ownership, editor focus/typing, pin/unpin, Stop/Escape, recorded preview/action denial and cleanup also passed. This is explicitly a synthetic controlled replay, not a recorded physical camera session.
- Replay failure `20261109T182942199-d138f96b235c4b4c958d12dfa71b1128`: fixture used inset0, rejected by mapping; fixed fixture to supported inset12. Failure `20261109T183050787-fb8fb82defad45c4aca9212fcf42ebcf`: gesture moved the real card, but old persistence assertion expected its pre-replay position; assertion now checks the resulting placement. Both failures retained.
- Full extended runs `20261109T183335322-156f3a520ca740798f8cdb142c6e22f9` and `20261109T183359756-fc2c622c84f24a5cbe51785f4648fcf6` failed at initial SESSION foreground guard: GameInputServiceWindow owned foreground. Final focused attempt `20261109T183437053-3cd5c137962f41079019ebbe228cfaf9` was also blocked by setup. No guard was weakened and no Windows service modified. These recorded FAIL results remain FAIL; final full native acceptance is BLOCKED, not a green regression claim.
- The latest changes after the successful E20 run add frame/context guards and timing instrumentation; final core/builds pass, but their final WPF rerun was prevented by the foreground condition. Guided-lesson usability and final complete native replay remain required before software acceptance.

## Performance, cursor investigation and cleanup

The controlled replay now collects bounded aggregate mapping/filter, pointer/WPF movement and classifier/dispatch processing times. No final timing values are claimed because the final instrumented WPF route could not start. Individual filter vs mapping and hit-test vs reducer times are not separately measured. Camera exposure/inference/display latency and FPS remain unmeasured. Existing IPC timing is not substituted for them.

Read-only inspection and the new isolated controls found zero cursor changes in the initial successful closed-app controls. This does not explain Execution21's earlier movement: raw device origin/remote influence was not established. The historical check remains INCONCLUSIVE; no attribution to Ghost or claim of harmlessness. No product SendInput/SetCursorPos path was added. Explorer Alt+Tab/tray remains unavailable on the private desktop and requires manual evidence.

After the final attempts the independent runner audit returned `InputDesktop: Default`; no Invey, Python or dotnet process remained. Camera ledger SHA256 remained `01A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58`: ten released rows,809686ms, no pending reservation, zero authorized starts. No webcam was started, no ledger field was reset or extended, no ordinary-desktop automated input was sent, no user library was migrated by this work. Release `Invey.dll` SHA256: `333C59D779775710C202C136F3E52A78A53E695DEBC7B9F93732C1F81E859F0E`.

## Qualification and delivery

See [LIVE_GHOST_TEST_GUIDE.md](LIVE_GHOST_TEST_GUIDE.md) for launch, camera-free beta, guided practice, three separate physical gates, Stop and feedback. No hardware permission is requested or implied by this delivery. A future bounded pointer/proposal-only request must be separately approved; live Pocket actuation requires another explicit decision and reviewed issuer.

Continue the same PR #9 branch; leave it open/draft/unmerged. Its metadata previously rejected updates with HTTP403 and was not retried. Publication and exact published-commit CI are recorded in the final delivery receipt and conversation. CI verifies builds/core/worker suites, not the blocked interactive native cases.

Next work is the remaining Execution22 verification: resolve the test environment foreground condition without weakening guards, rerun full private-desktop and guided lesson checks, then seek separate bounded physical pointer/proposal qualification. Planned dual-model fusion and unrestricted Windows actuation are outside this work. Live experience is NOT PASS and is not yet one physical test away from acceptance because final software/native verification remains open.

## Published software verification

Implementation commit `2868afa8bfdd5d03a267513984165dce01b7d3ed` was published to the existing PR #9 branch. GitHub Actions run [42675757404](https://github.com/yayatidattayt/Invey/actions/runs/42675757404) passed on that exact commit. Final committed-source Debug/Release builds passed with zero warnings/errors; core167/167 per configuration (`published-Debug.trx`, `published-Release.trx`). The committed source additionally clears gesture state explicitly on host Stop. Camera-off beta launcher `-CheckOnly` passed with zero instances and optional tracking runtime available; apphost SHA256 `C7F57AF068B1DFCE8E353FAEECFF40384FFA0EE2DFAB093AC9990EC83CC2AF83`. The final report-only commit is also checked separately before delivery; its exact SHA/run is in the local delivery receipt and final response. CI success does not remove the blocked native/human gates above.
