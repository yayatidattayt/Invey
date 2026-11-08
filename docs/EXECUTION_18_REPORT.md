# Execution18 — Personal Learning evidence

Engineering status: **IMPLEMENTED, PHYSICALLY UNVALIDATED**. Preview-only supervised personalization is available for review. This is not B1 release acceptance or B2 actuation. Evidence below separates deterministic, actual-WPF, native injected and physical observations.

## A. Repository state

Recovered clean branch codex/execution-17-demo-stabilization at99a2646421d220a0d71fab05f1d19b237a660e18. Fetched origin; PR1–6 remain open, unmerged and mergeable. New isolated branch: codex/execution-18-personalized-ghost-learning. Recovery ref refs/checkpoints/execution-18-start. No reset, clean, force push, parent rewrite or merge.

## B. Existing architecture

The qualified local MediaPipe worker still produces bounded hand observations. TrackingSession owns freshness, continuity and worker lifecycle. CalibrationMapping handles monitor-local DIPs and physical pixels; CalibrationPractice and FingerPointer retain their independent eligibility rules. The new pure-Core model sits around mapped positions. It has no OS input dependencies. The webcam backend and MediaPipe weights are unchanged.

## C. Algorithm choice

A regularized affine residual model has six parameters, predictable evaluation cost and inspectable bounds. Larger detector retraining, adaptive gesture authority and predicted missing landmarks are deferred. [Ridge objective](https://scikit-learn.org/stable/modules/generated/sklearn.linear_model.Ridge.html) and [atomic replacement semantics](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-10.0) informed implementation; no Python training dependency was added.

## D. Mathematical update

For normalized baseline position p, phi(p)=[1,x−0.5,y−0.5]. Fit residual target−p separately for x and y:
W=(PhiᵀPhi+0.01I)⁻¹PhiᵀR, solved using a pivoted3×3 elimination. All coefficients must be finite and ≤0.12 in magnitude, correction per axis ≤0.08 over the supported domain, and affine determinant between0.7 and1.4. The training convex hull must surround [0.2,0.8]². Outside that domain, or on a context mismatch, return the original baseline exactly.

## E. Labels and quality

The lesson has nine baseline, nine teaching and nine different independent-validation targets over nine regions. Eight distinct stable full selected-hand observations are pooled into one confirmed example. Stability radius0.012; end-to-end observation age <150ms; capture gap≤150ms. Duplicate IDs, train/validation overlap, invalid geometry, partial/ambiguous/wrong-hand/lost/stale evidence, insufficient coverage, large mismatches and changed sessions/settings are rejected. Confirm requires explicit intended-target agreement. Retry, Skip and Cancel never invent labels.

Unlabeled sessions contribute only bounded aggregate UI-observed quality. These counts are not worker frame counts and cannot establish accuracy.

## F. Persistence

Learning defaults OFF and persists independently of camera Enable. The selected profile's Learning/personal.json stores schema/revision, flags, current and previous coefficients/context, aggregate validation metrics and the latest aggregate session summary/evaluation. Labels and landmark samples stay in memory only.

Strict bounded JSON, SHA256 checksum, ancestry reparse checks, exclusive writer lock, optimistic revision, flushed same-directory stage and atomic replacement protect the file. A prior valid backup is available. Unreadable data visibly falls back to defaults without overwriting the original; future schemas remain preserved. Read-only and concurrent writes fail visibly. Existing calibration files and Pocket storage are untouched.

## G. Feedback

The real WPF section provides opt-in, Improve, confirm/retry/skip/cancel, Evaluate, results, use-default preview, rollback/reapply and Reset. Correct, missed/false pinch, missed release, loss, wrong-hand and wrong-direction reports are counted. Target confirmations supply supervised positions; general feedback does not.

## H. Gesture shadow boundary

Gesture feedback lacks trustworthy time-aligned feature labels for a new classifier. The UI explicitly says there is insufficient evidence and **no gesture classifier was trained**. No thresholds, dwell, loss cancellation, selected-hand rules or action authority are relaxed. Personalized mapping affects only its labeled preview; ordinary practice still uses the original mapping. No OS-wide clicks or B2 actuation.

## I. Promotion

An independent set must improve mean error by at least5% and0.001 normalized units against both the uncorrected baseline and any context-compatible incumbent. P95, worst and worst-region regression tolerance is0.001; each represented region is checked against both references. These small samples are conservative engineering checks, not statistical generalization guarantees. A held-out set is evaluated once. Training runs off the UI thread; epochs invalidate completion after Stop, context/session changes, cancellation or active practice holds.

## J. Reversal and recovery

Rollback swaps accepted/previous models, including a return to identity when no prior model exists. Reapply swaps again. Reset deletes learned parameters and backup and disables learning, without touching Pocket. Numerical reload/rollback and actual fresh-process WPF reuse tests pass. Corrupt and future-schema tests verify safe startup behavior; a critical missing InvalidDataException catch was found and repaired.

## K. Privacy

No raw images, video, landmark streams or target samples are persisted or uploaded by learning. Only a hash of the already-local camera identifier is retained in context. No biometric identity, cloud training or export. Test data and aggregate hardware diagnostics remain ignored local artifacts. Normal profile metadata hashes are checked separately; physical camera images are never captured by desktop inspection.

## L. Deterministic tests

Baseline100/100 core tests in each configuration passed before changes. Final121/121 in Debug and Release, zero skips. Three independent biased/noisy distributions improved held-out mean physical-pixel error:
29.9019→0.0856;39.8697→0.1480;69.7704→0.1754. These are deliberately controlled synthetic mapping errors, **not physical accuracy measurements**. Worse labels, distribution shift, duplicate/overlapping IDs, inadequate coverage, outliers, context/domain changes, corrupt/future profiles, locked/read-only/concurrent writes, reset and safety negatives are covered.

Local evidence: artifacts/execution-18/delivery-Debug.trx and delivery-Release.trx, plus preserved earlier final and learning runs.

## M. Actual WPF integration

Two synthetic biases were fed through guarded acceptance-only observations and actual WPF controls. The full loop confirmed27 targets, fit and independently validated, saved, exited, started a fresh application process with source OFF, reloaded the same numerical prediction, rolled back, reapplied and reset without changing Pocket references. No camera and no native input were used in these supplemental tests.

Final two-seed learning runs:
20261108T201641845-70b3641d3457411695803251fb8aac53;
20261108T201702460-1961d297e17a46449298fbb14d4a3e2e.
Final two-seed demo control routes:
20261108T201627867-63e712ed3ae94c57859fd0788ad6cfef;
20261108T201648739-73b3de1b64ed4f05a70ab8868229860a.
All PASS. An additional delivery learning rerun checks the final feedback-summary fields. The preceding four app-hash linkage is5D360E9288DB710B08C6AEF6D23869E802BCF08A7C5175F53758DDA67A5A9E67; later binaries are recorded separately rather than relabeled as identical.

## N. Physical experiment

A fresh bounded amendment was explicitly authorized in conversation: at most three additional user-enabled sessions of≤180 seconds, while carrying all previous charges and retaining12 total sessions/1,800 seconds. The verified demo app was presented visibly with camera OFF. The participant must select and Enable the webcam. No new physical acquisition or confirmed physical target dataset has been observed at this report checkpoint. Baseline/train/independent physical validation, physical restart/reversal and subsequent-session accuracy therefore remain NOT RUN. Availability and permission alone are not evidence of a hardware pass.

## O. Performance

Release deterministic microbenchmark: mean training0.1451ms over100 trials; model evaluation0.1264µs with zero measured managed allocation across100,000 predictions. These are local microbenchmarks, not live camera/UI FPS. Camera FPS, source latency, preview rate, stationary jitter and pointer lag were not remeasured. Execution16 historical29.6fps cannot be claimed as a current learning-mode measurement. WPF learning-control routes took about1.7seconds using synthetic immediate observations, not human teaching.

## P. Guardian and cleanup

Version3 amendment records the original five sessions unchanged:306999ms, all released, Execution18StartCount=5. Reservation permits at most three additional starts and preserves the older global limits. Guardian cutoff remains178 seconds and release must be confirmed. Atomic camera-ledger replacement now uses File.Replace. New deterministic sub-budget coverage preserves prior charges and denies the fourth new start. No ledger reinitialization.

Native tests restored Default desktop and released test-owned app/worker processes. The first camera-off demo process was deliberately stopped before rebuilding the final feedback-summary fix; no camera, lesson or editing window was open. The committed verified app is deliberately open for participant handoff, with camera OFF and no worker. That one visible app is intentional; no active camera session or test-owned worker remains.

## Q. Pocket and Dual

The existing feature inventory remains in DEMO_FEATURE_INVENTORY.md. No feature was removed. Native standard regression passed actual capture/preview/save, persistence, floating cards, shelf/shortcuts, two-reference Workspace, recorded tracking, partial handling, calibration practice and complete demo route. Learning reset preserves the test Pocket library. Learning never routes to physical mouse input.

## R. Builds

Pinned SDK10.0.403; locked restore succeeded. Debug and Release build with zero warnings/errors. Final core tests121 each. Detailed logs are local under artifacts/execution-18. Older failed builds/runs remain preserved.

## S. Worker tests

20/20 Python worker tests PASS, zero skips, using the approved controlled native-video fixture; no physical webcam. The synthetic practice sequence has longer deliberate phases to tolerate scheduler/coalescing effects while preserving the reducer's original strict continuity/dwell conditions.

## T. Native Windows verification

Standard run20261108T201144706-ac276dd29a4e402abfea11f0a4b71894:38 PASS, zero FAIL, one BLOCKED. It includes full demo,100 click-throughs,100 drag starts/releases, typing soak, cursor independence, recorded lifecycle/faults, camera-error injection, partial preview, Pocket/Workspace/settings and cleanup. A10 Explorer/tray is BLOCKED because the private desktop has no Explorer shell. No service was killed and no guard weakened. Available125% environment was used; physical mixed-DPI and human-origin acceptance remain distinct.

The last small feedback-summary addition is covered by the delivery core/WPF learning rerun; the earlier full native result is not silently restamped as a later binary.

## U. Preserved failures and repairs

- Baseline demo control run20261108T194920628-3ed95af2d38f4eba86689a4398361d2f: Ready reached but brief Held missed. Extended only synthetic fixture phases; final native/control demo routes pass.
- First restricted test launch aborted on testhost connection timeout. Normal process communication resolved that environment restriction; original log remains.
- third-build.log: missing System.IO import in new test file, corrected.
- learning-second.trx: corrupt-profile recovery failed (missing InvalidDataException catch, repaired); one existing Pocket migration assertion failed transiently. Migration code was not changed; subsequent complete configurations pass. Root cause of that one migration failure is unconfirmed.
- learning-third.trx: camera-ledger File.Move access denial. Switched existing-file atomic update to File.Replace; subsequent ledger/core tests pass. A particular external locker was not identified.
- New WPF tests initially failed on reading the tracking window before it opened, observing stale rollback preview, and reading Pocket before opening it. Fixed queued-window preconditions, immediate numerical preview recomputation and actual Pocket navigation. Failed runs dc54bedb,fc5e083a,de9d3b44 remain local.
- Final review found candidate completion could outlive a Stop request. Stop now increments the invalidation epoch immediately. Wrong-hand/direction feedback counters were also completed.

## V. GitHub CI and exact head

Baseline PR6 head99a2646421d220a0d71fab05f1d19b237a660e18 has successful run42496720613. The new branch adds the PR6 base to the existing Windows workflow trigger list. Implementation commit e07d1d17da033dec94bd1842f70b05c3f370197b passed [Windows CI42520020144](https://github.com/yayatidattayt/Invey/actions/runs/42520020144). The final documentation-only head is verified separately through [PR7 checks](https://github.com/yayatidattayt/Invey/pull/7/checks) and the local delivery record. No hosted workflow result stands in for physical camera or isolated WPF evidence.

## W. Pull request

[Draft PR7](https://github.com/yayatidattayt/Invey/pull/7) targets codex/execution-17-demo-stabilization, implementation e07d1d17da033dec94bd1842f70b05c3f370197b. The connector returned403 for PR creation; existing authenticated Git credentials completed the authorized publication. Parent PR1–6 are preserved. No automatic merge.

## X. Remaining limitations

Physically unvalidated personalization; intended-target labels rely on honest user confirmation. Small central mapping correction only. Missing wrist/index detection remains unresolved. No learned gesture classifier or production actuation. Explorer/tray manual acceptance, mixed-DPI physical alignment, general hardware/lighting/hand reliability and long-duration physical learning remain open.

## Y. Integration recommendation

All six parent heads have successful Windows CI and no submitted review or inline review threads at inspection. Mergeability is not approval. Review and integrate1→2→3→4→5→6 in dependency order, rerunning relevant integration tests after retargeting, then review this learning PR. Keep canonical Gate A/M02/B1 blockers explicit. Do not merge the stack for convenience.

## Z. Next milestone

Complete a user-enabled physical target comparison, camera release/restart, rollback/reapply and another labeled session under the remaining bounded allowance. Investigate wrist-out detector loss separately. Obtain canonical safety/integration review before any production gesture actuation.

## Delivery verification addendum

The launchable executable is src/Pocket.App/bin/Release/net10.0-windows/Invey.exe. Its accompanying Invey.dll reports1.0.0+e07d1d17da033dec94bd1842f70b05c3f370197b, SHA25643F0FC59924A028856E0D0622229D9363DF54A4C9694D3839692C3955ED54A86. Committed-binary actual-WPF learning run20261108T202517208-194d6b02457642caa21d25dde7012f59 PASS. The final feedback-summary delivery run20261108T202106186-b52b45a87d5d4e08b5c9eb8a582ab7ae also PASS. These supplement, rather than overwrite, the earlier native and seeded binary evidence.

Both normal/legacy metadata hashes compared unchanged (two files). Ledger still has five released rows,306999ms and zero pending reservations. Text-only inspection of the visible demo home screen confirmed Ghost · Camera off. No webcam images were taken by inspection. The optional participant-readiness question has not received a response and no deliberate camera Enable has been observed; physical results remain NOT RUN, despite the authorized allowance.

From the repository folder, launch after exiting an existing instance:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Invey-Demo.ps1 -DemoProfile
```

The currently open manual-test instance already carries the bounded guardian configuration. A generic future normal launch does not renew the execution-specific test allowance. Exact final publication/CI linkage is retained in artifacts/execution-18/final-delivery.json and the PR checks; documentation-only follow-up commits do not change this verified executable's source.

Final recommendation: **READY FOR ENGINEERING REVIEW, PHYSICALLY UNVALIDATED**. Retain the PR as draft pending physical learning evidence and parent-stack review. No claim of full B1, physical accuracy improvement, Explorer acceptance or release readiness.

See PERSONAL_LEARNING_GUIDE.md for ordinary-language controls and data handling.
