# Execution20 recovery — resumed checkpoint (2026-11-09)

The user resumed this checkpoint. The previously requested pause is complete; continue this same branch and PR. Final local verification has passed with one documented Explorer-shell block. Publication and exact-head CI status are tracked in the current report addendum. Preserve the original pause and all failure artifacts below as history.

Historical pause note: the user explicitly requested a smooth stop because only10% usage remained. At that pause, app/tracking/test processes were stopped, input desktop was Default, and project-local idle MSBuild servers were gracefully shut down. See artifacts/execution-20/user-stop-final-audit.json.

## Repository
Root: C:/Users/YAYATI DATTA KANTETI/Documents/ghost/GHOST_CODEX_PROJECT_READY
Branch: codex/execution-20-interaction-engine
HEAD / published PR9 head remains84b8102275292a2b3ecf79ccf4728768cbccd76f.
PR: https://github.com/yayatidattayt/Invey/pull/9
All recovery changes are UNCOMMITTED, including new files. Preserve them. Do not reset/clean or start another branch. The previous successful CI42626739078 covers only the old checkpoint, not these changes.

The full user directive is in:
C:/Users/YAYATI DATTA KANTETI/.codex/attachments/94a08126-cfc1-4f09-83e5-e78060734324/Pasted text.txt

## Implemented recovery work
- One application-owned Core GhostDesktop plus WPF GhostDesktopHost, typed sources/provenance, supported-command registry, native stacking/reachability, source generations and expiry.
- One passive desktop pointer and explicit Pocket reveal affordance; native pass-through/nonactivation styles reused.
- User-operable Controlled Ghost Demo window: Start, target selection/aim, Arm, movement buttons, Press/Release, Activate/Pin/Unpin/Hide, Disarm/Stop/Escape.
- Actual shelf rows/dismiss, actual floating cards and actual Workspace references registered. Existing reducer preserves Mouse/Ghost ownership; per-Workspace coordinator removed from product flow. Legacy Core GhostInteraction retained only for compatibility tests.
- Recorded and separately enabled camera snapshots can feed desktop preview through GhostTrackingMapper: validated raw point → calibration → compatible accepted personal correction → one precision filter → physical display pixels. Recorded/camera sources cannot arm actions.
- Source replacement cannot let an old simulation timer stop its replacement. Closing Workspace cannot cancel an unrelated card. Actual card close releases its hold. Async opening checks the current session/mode and reloads current metadata.
- Unique Enlarge Alt+E fixes collision with Move left Alt+L. Native demo moves the pinned card through its title bar, retaining pinning and the real resize postcondition.
- New guarded launcher scripts/Start-Invey-DualCursor.ps1 defaults to the isolated demo profile and preserves the exhausted guardian.
- Prior report copied to docs/EXECUTION_20_CHECKPOINT_REPORT.md. Current report and operating guide updated, but final delivery addendum is still pending.

## Evidence (preserve every failed attempt)
- recovery-Debug.trx and recovery-Release.trx:150 core tests each; recovery-worker.log:29 workers/no skips. These suites must be rerun on final current source after the last supported-command/lifecycle additions.
- Complete integrated source-connected product route:
  20261109T141514145-29fbda091ae44b98aa2c6142a03a359d PASS.
- Actual controller-button route:
  20261109T142528163-31a22749efd94b41aaba60d6edc4c111 PASS.
- Latest route including old-simulation replacement, unrelated Workspace closure, recorded preview/action denial, native editor typing/cursor checks, actual pin/unpin, owned-card Hide and restart:
  20261109T142735996-75caf2d439874c8f829c22c7ffd89ba4 PASS.
  Log: artifacts/execution-20/dual-source-lifecycle.log.
- Full extended regression:
  20261109T141746432-d8af3c64705a40cbb1a2426d97eaa483:39 PASS,1 FAIL(E20 restart assertion),1 Explorer BLOCKED. D17 resize/full demo PASSED.
  Its new route reused D17’s profile with extra pinned cards, so LastWindow on restart could select another card. The route now uses its own clean dual-demo-profile (dual-demo-start) and targeted tests pass. The FULL suite has NOT yet been rerun after that correction.
- Earlier failures include a diagnostic missing System.IO compile import, nullable callback compile issue, one card-close IPC timeout (now cached hook-source cleanup), uninitialized calibration fixture, full-monitor vs usable-work-area parking expectation, initial foreground refusal, and profile contamination. Original artifacts remain.

## Camera / input boundary
Ledger artifacts/execution-16/camera-budget.json UNCHANGED:
10 released rows,809686ms,0 pending,0 authorized Execution18 starts.
SHA25601A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58.
No real camera started. Do not reset/extend/bypass it. No ordinary-desktop input injection. Only the existing isolated harness may inject test input. Live hand actuation remains gated/physically BLOCKED.

## Next work, in order
1. Read this checkpoint, current reports/guide and source diff; verify clean-of-external-edits status and no running app. Preserve all uncommitted files.
2. Review the final integration for correctness; avoid speculative expansion.
3. Pinned SDK locked restore; Debug/Release builds/core suites;29 worker tests using GHOST_NATIVE_VIDEO_FIXTURE=artifacts/execution-08/controlled.mp4.
4. Run the full isolated Windows regression on current source. Use existing extended profile (bounded360s); new E20 route is included. Two seeds for the main route; preserve any foreground/environment failures. Rerun learning/reload/rollback controls.
5. Useful isolated environment flags: INVEY_ACCEPTANCE_DUAL_PRODUCT_ONLY=1, INVEY_ACCEPTANCE_LEARNING_CONTROLS_ONLY=1. Clear flags between scopes. Do not run two isolated-desktop harnesses concurrently.
6. Confirm camera-off launcher -CheckOnly, native process/window cleanup and Default desktop restoration.
7. Finish report readiness/evidence addendum and handoff/research/test/compatibility decisions with actual results.
8. Commit and push to the SAME PR9 branch; verify exact published-head Windows CI. Rebuild/link the committed binary and verify a real route. Do not confuse the assembly version’s old HEAD with uncommitted source evidence.
9. No parent merges. Final status can be controlled product COMPLETE only if required A–G checks actually pass; physical camera/human acceptance remains separate.

## Stack audit
Current PR1–9 heads all verified ancestors of this checkout; all open/unmerged/mergeable and passing exact-head CI at audit. No submitted reviews returned. Review/merge only in dependency order after separate review, never automatically.
