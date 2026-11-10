# Ghost external controls — experimental, camera off

Ghost can send selected semantic actions to a deliberately authorized window. It does not become a second native Windows mouse, click arbitrary pixels, drag arbitrary external objects or type into applications. Webcam/recorded/optical preview cannot issue external commands. Only controlled Ghost is eligible in this milestone.

## Camera-free demonstration

From the repository root run `scripts\Start-Invey-Presentation.cmd`. It uses an isolated demo profile and keeps the camera off.

1. Choose **Try Ghost Beta**, then **Start preview**.
2. Open **External controls** → **Open synthetic external demo**. This launches a separate WPF executable with synthetic content, not a Pocket card or webcam replay.
3. Check **Enable external controls for this session**, choose **Find eligible windows**, select **Invey.ExternalDemo**, choose an action type, then **Authorize selected window**.
4. Choose **Arm external action**. Return to Try Ghost, enable keyboard steering and use arrows/WASD to move the one Ghost over the chosen control. Shift makes smaller moves; Ctrl makes larger moves.
5. A thin target border and status show the supported pattern. Press **E** once in the focused steering panel, or deliberately press **Activate current action** in External Controls. No hover activation occurs. This is a keyboard-controlled action, not a detected webcam pinch.
6. Observe the external counter/checkbox/slider/scroll/details/selection. Invoke can remain **Outcome unknown** because UIA returning cannot prove the command's visible effect. Inspect the app, optionally review, then deliberately dismiss the reviewed outcome; do not blindly repeat the command.
7. **Stop Ghost**, Escape in the steering panel, **Stop external controls**, closing External Controls or turning its checkbox off revokes new capabilities. Exit Invey closes its owned demo and supervised worker. An already dispatched command cannot be rolled back; its uncertain outcome remains explicit.

Each authorization selects one action: Invoke; Toggle; Scroll (small vertical increment); RangeValue (the bounded midpoint, shown before activation); ExpandCollapse (toggle); SelectionItem (select an unselected item). Not all controls expose these patterns. Disabled, sensitive/edit/password, stale, covered, hidden, wrong-process and ambiguous/unavailable targets cannot act. Move the ordinary mouse as usual: it does not drive Ghost, and Ghost never teleports it.

## Compatibility and focus

General external windows must be visible top-level windows in this user's session, at medium integrity and no higher than Invey. Elevated/protected apps are not a generic supported route. The exact project-owned synthetic demo executable has a narrow same-integrity exception for this elevated test environment; this does not authorize unrelated elevated windows. Discovery is bounded to the explicitly selected root and current point; it does not save another app's UI tree or text values.

External providers may change foreground/keyboard focus. Cursor independence does not imply focus independence, and keyboard steering handles keys only in its own focused panel. Return focus deliberately if an action moves it. Do not assume an existing physical drag survives every external action; consult the report's tested control results. No touch/pen injection backend is enabled.

Select only a benign window and a reversible, understood operation. Purchases, destructive commands, credentials, unknown commands and privileged UI are outside this MVP. The project's six-control demo is verified separately from genuine installed-app compatibility; see EXECUTION_27_REPORT.md for exact outcomes and environment gaps.

## Physical tracking

The separately approved E27 baseline/hybrid comparison is complete. Creator reported less jitter and better wrist-out behavior, but fast-motion jumps, flicker and instability persist: **CHANGES REQUIRED**, not stable-pointer acceptance. No image-only handoff was logged; subjective wrist improvement cannot be attributed to optical fallback. Hybrid designated-still RMS11.8px; baseline motion invalidates a numerical jitter-improvement comparison.

Version10 ledger has16 released starts,1459827ms,0 pending. All starts are consumed. No more live tests or quota amendment is authorized. The A/B launcher scripts are retained for reproducibility, not as renewed camera permission. Pocket, Workspace, controlled/recorded Ghost and this camera-free separate-process demo remain available. Camera/recorded/optical sources cannot invoke external actions or physically qualified Pocket gestures.
