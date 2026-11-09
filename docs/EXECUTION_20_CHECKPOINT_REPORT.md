# Execution20 — Workspace bridge checkpoint

**CHANGES REQUIRED — Execution20 is incomplete.** This checkpoint finishes a bounded controlled-source Workspace adapter. It does not satisfy the shelf/floating-card integration milestone.

## A–F. Baseline, architecture, routing and intent

Clean baseline4bfe9b321c50e67e2cc5ea4f81a19f287e355a9d fetched before branching to codex/execution-20-interaction-engine. The full PR8 stack is retained. Existing InteractionState remains the sole ownership reducer. GhostInteraction adds controlled-source session tokens, explicit arming, target registration/hit testing, fresh-event gates and Stop/Pause invalidation. Target positions are board-local DIPs; no display-to-board tracking adapter is connected yet. The adapter accepts explicitly synthetic intent only. Its full-evidence boolean is not a live gesture classifier or sufficient live authorization.

## G–M. Product integration boundary

Actual Workspace references now respond through this coordinator rather than direct Ghost reducer calls. Existing simulation and private-desktop control routes use it. Mouse claims remain independent. Source replacement, partial/visual/missing full evidence, age≥150ms, out-of-order input and target removal cancel Ghost claims. Live tracking, personal correction and passive overlay remain separate existing implementations. Shelf and floating cards retain normal commands but are NOT registered with this coordinator yet. There is no claim that live tracking now moves Pocket cards.

## N–R. Verification

Baseline Debug132 PASS; baseline Release131 PASS/one preserved migration access failure IOException0x80070005. New Debug/Release suites137 PASS each. Both builds have zero warnings/errors. The new deterministic tests cover independent claims, late events, invalid gesture evidence, source replacement, target removal, Pause and idempotent Stop.

Native run20261109T133100866-9b26f70112b34be28ad917b85dbadca3 passes capture, restart, 30 independent Workspace trials, shelf/shortcuts and cleanup. Its wider demo fails at mouse resize because the intentionally pinned card covers the Workspace button. The test now uses the real existing Larger keyboard command, retaining the resize postcondition; the original failure remains preserved. Rerun results follow in local artifacts/execution-20/pocket-rerun.log. No physical or optical latency measurement is claimed; integrated latency instrumentation remains pending.

## S–V. Hardware, cleanup and failures

No new hardware permission is granted. Ledger remains version4, ten released rows809686ms, zero pending, zero available Execution18 starts. No camera startup or normal-desktop injection was performed. Tests use the guarded isolated desktop. Preserved migration/focus failures above are not converted into skips. Final independent desktop/process audit must be inspected before delivery.

## W–Z. Integration, delivery and next work

This branch inherits the complete stacked candidate. Parent PRs must be reviewed in dependency order1→8; do not merge/squash/close automatically. A fresh exact-head CI/review/conflict audit of every parent remains pending. This checkpoint is not a completed stack review or release recommendation.

Required remaining work: production snapshot/gesture adapter and display transforms; shared passive pointer; independently armed user control; shelf/reference/card registry with lifecycle registration; card movement/pin/persistence dispatch; accepted personal correction integration; measured latency; new complete product end-to-end route; full tracking/learning regression and exact final published-head CI. Do not report the requested milestone complete until actual shelf and floating-card content responds through the new router in WPF tests.

Future physical plan, requiring fresh explicit authorization: selected-hand alignment and direction, deliberate pinch/release counts and false activations, acquisition/drag/loss/ambiguity/Stop trials, concurrent real mouse/editor focus, independent worker exit and camera release. Synthetic/native tests remain distinct from physical evidence. Live actuation stays gated.

Checkpoint outcome: CHANGES REQUIRED. Rerun971f58a2 and targeted demob9d0fe8b also failed the resize postcondition. Keyboard/focus changes did not resolve it and were reverted to preserve the existing fixture. The exact focus/occlusion mechanism remains unconfirmed. Two native runs independently passed30 Workspace ownership trials each. Worker regression29 PASS/no skips. No claim of a green complete demo or full regression is made. Further integration is stopped at this unresolved reliability boundary; no shelf/card commands are partially installed.
