# Invey Beta — hands-on checklist

Run this from the isolated beta profile using [Try Invey Beta](TRY_INVEY_BETA.md). Mark exactly one result for every item. Do not use a camera unless a separate physical-test authorization is approved.

Status choices: **PASS / FAIL / NOT TESTED**. Add brief notes for failures. A result from the isolated automation run is not a human usability result.

| # | Check | Result | Notes |
|---:|---|---|---|
| 1 | Beta app launches and identifies the isolated profile | NOT TESTED | |
| 2 | Ghost ring appears after Start preview | NOT TESTED | |
| 3 | Ordinary Windows mouse remains normal and does not jump | NOT TESTED | |
| 4 | WASD/arrows move Ghost independently | NOT TESTED | |
| 5 | Ghost can aim at a visible Pocket target | NOT TESTED | |
| 6 | Ghost can grab/open a saved reference | NOT TESTED | |
| 7 | Ghost can move a floating card | NOT TESTED | |
| 8 | Release ends the card move | NOT TESTED | |
| 9 | Pocket tab/shelf opens and tucks | NOT TESTED | |
| 10 | Pin and unpin work | NOT TESTED | |
| 11 | Card placement and pin state persist after restart | NOT TESTED | |
| 12 | Workspace A and B remain independent | NOT TESTED | |
| 13 | No unexpected focus change during passive Ghost interaction | NOT TESTED | |
| 14 | Stop/Escape releases immediately and hides Ghost | NOT TESTED | |
| 15 | App exits cleanly with no Invey process left | NOT TESTED | |
| 16 | Taskbar, tray, Alt+Tab and ordinary shortcuts behave normally | NOT TESTED | |
| 17 | Real webcam preview, only with separate explicit authorization | NOT TESTED | |
| 18 | Physical pinch/drag, only after separate authorization and qualification | NOT TESTED | |

## Paste-back report

```text
Invey Beta manual test
Date:
Branch/commit shown by launcher:
Profile used: Beta / Normal (manual use only)

1 Launch: PASS / FAIL / NOT TESTED —
2 Ghost visible: PASS / FAIL / NOT TESTED —
3 Windows mouse normal: PASS / FAIL / NOT TESTED —
4 Ghost movement independent: PASS / FAIL / NOT TESTED —
5 Target aim: PASS / FAIL / NOT TESTED —
6 Open reference: PASS / FAIL / NOT TESTED —
7 Move card: PASS / FAIL / NOT TESTED —
8 Release: PASS / FAIL / NOT TESTED —
9 Shelf: PASS / FAIL / NOT TESTED —
10 Pin/unpin: PASS / FAIL / NOT TESTED —
11 Restart persistence: PASS / FAIL / NOT TESTED —
12 Workspace A/B: PASS / FAIL / NOT TESTED —
13 Focus: PASS / FAIL / NOT TESTED —
14 Stop: PASS / FAIL / NOT TESTED —
15 Exit/process cleanup: PASS / FAIL / NOT TESTED —
16 Windows taskbar/tray/Alt+Tab/shortcuts: PASS / FAIL / NOT TESTED —
17 Webcam preview (separate authorization only): PASS / FAIL / NOT TESTED —
18 Physical pinch/drag (separate authorization only): PASS / FAIL / NOT TESTED —

Most confusing part:
What I expected:
What actually happened:
Reproduction steps:
Feedback report copied: yes/no
```
