# Execution23 — Real Ghost qualification

## Starting state

Continued `codex/execution-20-interaction-engine` / draft PR9 from `c424d44ca52e986985bcccd52be594ae0bf46d40`; connector confirmed the remote head before edits. Worktree was clean. Inherited the single shared Ghost desktop pointer, calibrated/personalized mapper, bounded visual continuation, guarded gesture coordinator, guided practice, Pocket/Workspace and controlled beta. No parent merge, force-push or unrelated feature addition.

Read active rules/handoff/specification, Execution20–22 reports, QA matrix and existing live-test guides. Execution22's final native acceptance and guided lesson verification were still open; physical camera/gesture/live-actuation claims were not accepted.

## Native blocker and verification

Read-only foreground diagnostics now record input desktop and the desktop/thread owning foreground before test input and on failed postconditions. New preflight evidence shows both foreground fixture and input on the designated `GhostAcceptance_...` desktop. Historical GameInputServiceWindow interference did not reproduce throughout the final completed run. This establishes a successful new environment instance, not a root cause for the earlier interference. Windows services, scaling and guards were not changed.

Initial unmodified short run `20261109T184934004-65dc5ccee28e4a9a917b088a81acf881` reached later stages but exceeded150s; its files remain. Its old parent error falsely labeled input as uninjected despite a child having run. Parent timeout reporting now explicitly says injection status unknown and refers to child evidence.

Final extended seed23023 run `20261109T185553944-0dcf8c9eb0eb417f9813077d1b6336d0`: **39 PASS,0 FAIL,1 INCONCLUSIVE,1 BLOCKED**. D17 actual capture/preview/save, two real Workspace references, pinning, recorded model, practice, settings, restart and worker cleanup passed. E20 real card acquisition/movement/release/persistence, independent owners, native editor typing/focus, controlled keyboard steering, Escape, recorded action denial and shared pointer passed. All native input remained on the guarded private desktop.

`CURSOR_CONTROL_GHOST_CLOSED` recorded119 changed samples with injection inactive and remains INCONCLUSIVE. Physical mouse, remote input, sampling effects or other input origin were not independently identified. No attribution to Ghost or harmlessness claim. `A10_ALT_TAB_TRAY` remains BLOCKED because the private desktop lacks Explorer. The creator's ordinary taskbar/tray/editor/manual route remains NOT TESTED unless separately reported.

## Software routing, mapping and repairs

The existing production path remains observation validation → selected hand → calibration → context-compatible personal correction → one precision filter → physical display coordinates → shared host → actual passive pointer. No new tracker, model or cursor was introduced. Existing deterministic tests cover mirror direction, negative origins,96/120 DPI transforms, corners/edges/center, frame/context changes, compatible correction, loss and Stop. Physical direction, jitter, sensitivity/overshoot and mixed-DPI behavior require human evidence.

Fixed `GhostDesktopHost.TrackingPosition` to begin a new desktop session when source kind changes even if a caller retains the source GUID. Source-context replacement immediately cancels practice; pointer reacquires through the existing four-observation eligibility period. Full/partial/FingerVisual provenance remains separate; incomplete evidence cannot press or continue a held action. The600ms visual deadline is unchanged.

The actual Tracking window now participates in a camera-free test using explicitly synthetic camera-protocol observations: the real guided open/pinch/release controls complete, proposals reach the production coordinator, mapped preview uses Camera provenance, production Arm is denied, release completes and Camera→Recorded invalidates context. This does not open a camera and is not physical evidence. Successful focused seed23005 run `20261109T185528194-698c1bfdfae446f1a87c38a418a690db` and the full final run include these checks.

Stop now sits above scrollable preview/calibration content, always reachable in the lab. A concise summary shows selected device/display/hand/mirror, full/partial/visual evidence, unavailable tracking, completed Stop and disabled Pocket actions. No live qualification issuer was added; `MayExecute` and `LiveQualified` remain false in production.

Preserved failures: seed23002 constructor ownership error while moving the existing ScrollViewer into a DockPanel, repaired by detaching Content first; seed23003 foreground PID0 before test input, repaired by using the existing explicit fixture-focus precondition; seed23004 source assertion before mapper reacquisition, corrected to check immediate cancellation and then valid stabilized source. Guards/assertions were retained; no failed test was skipped. Runs are under `artifacts/automated-acceptance/` with suffixes `7b0a5cbb27a04d14b74fb2c6182443b3`, `9381fb5b4533430ba4ea6493ccd57fc1`, `889977e074c04dc09259811873c7634d`.

## Builds and performance

Pinned locked restore passed; Debug/Release builds passed with zero warnings/errors. Core **168/168 per configuration**, no skips; worker **29/29**, no skips, controlled video only. Baseline167/167 passed. Core results are in `artifacts/execution-23/`. The added budget test proves ten historical rows remain unchanged and that the one-use amendment cannot replenish after consumption or restart.

Focused E20 command/IPC/WPF observation median3.5379ms,p9560.2072ms. Final D17 startup-through-fixture observation886.7339ms; these are controlled measurements, not optical camera/display latency. Physical stationary jitter, input lag and overshoot cannot be inferred from them. Existing bounded host/worker diagnostics capture observation counts/age, timing, continuity, provenance and cleanup without saved frames or raw landmarks. Session-wide calibration variance is not stationary jitter when the creator moves.

Release apphost SHA256 `57D3D9C5E536BD6FDF37E6B57D011D474CE9AB22DEF8D4FE32935EB5DA94593E`; Invey.dll SHA256 `2E06FBDB2F0816236145557742FE079C578CB7BC6BA36641B2C25BB205EEAD41`.

## Camera authorization and physical gates

At preflight the immutable historical snapshot contained10 released rows totaling809686ms, zero pending, exhausted Execution18 starts, SHA256 `01A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58`. Existing global caps remain12 starts and1800000ms. Independent guardian and Stop paths were inspected; cooperative timeout170s, independent cutoff178s, maximum reservation180s.

After all camera-free checks the creator explicitly authorized **one creator-enabled ASUS FHD pointer-only session, maximum180s**. The audited version5 amendment is one-use and leaves Execution18StartCount=5, all ten history rows and all global caps intact. Applying it again does not replenish a start. Pre-amendment backup and scope receipt are under `artifacts/execution-23/`; code startup/preview/Enable never applies it automatically. Amendment CLI was invoked only after explicit approval. Ledger comparison verified all historical rows byte-equivalent as JSON values. Camera remained off at app launch; user selects device and presses Enable.

Physical pointer gate: **PENDING supervised session outcome**. Physical gesture gate: NOT AUTHORIZED/NOT TESTED. Live Pocket control: DISABLED/NOT AUTHORIZED/NOT TESTED. The pointer allowance grants neither later gate. No video, raw landmark files or private screenshots are authorized. See [REAL_GHOST_FIRST_TEST.md](REAL_GHOST_FIRST_TEST.md) for setup, direction/stillness/small motion/fast motion/partial/loss checks, manual mouse independence, Stop and feedback.

## Delivery and next action

Final published commit/CI and physical outcome will be entered after their actual evidence. PR9 stays open/draft/unmerged; previously forbidden metadata updates are not retried. Camera-free native regression is complete with the explicit inconclusive/shell cases above. Complete the supervised pointer gate, independently verify release and record creator feedback; gesture proposals require a further separately approved session, and live Pocket actions require evidence review plus another explicit decision. No dual-engine fusion or cold-start model work is justified by this execution.
