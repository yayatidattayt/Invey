# Try Invey Beta

## Start safely

Use the isolated beta profile first. It contains clearly labeled synthetic sample references and does not use your normal Pocket library.

1. Double-click `scripts/Start-Invey-Beta.cmd`, or from the repository folder run `./scripts/Start-Invey-Beta.ps1`.
2. The launcher prints the source commit, exact `Invey.exe` path and executable SHA-256 before starting. It checks the Release build, local runtime, optional recorded-tracking files and duplicate app instances. It never enables the camera.
3. The app title/profile notice should identify the demo profile. Choose **Pocket** to see the two sample references. They are sample images, not screen captures.

To check without starting the app: `./scripts/Start-Invey-Beta.ps1 -CheckOnly`.

To rebuild with the pinned SDK after source changes are committed: `./scripts/Start-Invey-Beta.ps1 -Rebuild`.

To open the normal Invey library instead, use `./scripts/Start-Invey-Beta.ps1 -UseNormalProfile`. Do this only when you intend to work with your real saved references. Beta and normal profiles are separate.

## See and move Ghost

1. In the main window choose **Try Ghost Beta**.
2. Select the display and choose **Start preview**. A small ring is Ghost. The ordinary Windows mouse is unchanged.
3. Check **Enable keyboard steering in this window**. This deliberately gives this panel keyboard focus. Use **W/A/S/D** or the arrow keys. Hold Shift for 16-pixel steps; Ctrl moves farther. The visible ring and target hit-test use the same coordinates.
4. Move the control panel if it covers the area you want to point at. The **Aim** and **Refresh targets** controls remain under **Accessibility and advanced controls** as a fallback.
5. Ghost status shows the target under the ring. An orange ring means armed; green means holding.

Keyboard steering only reads keys while this panel is active and steering is checked. It does not claim to preserve keyboard focus in another app while consuming keys. Clicking another app switches focus normally; if a card was held, losing the panel releases it.

## Open and move a Pocket card

1. Keep **Try Ghost Beta** open and start preview.
2. Move the ring onto the small **Pocket** tab near the right edge of the selected display.
3. Choose **Arm Pocket**, then press and release Space (or use the **Grab / press** and **Release** buttons) over the tab to reveal the Pocket shelf.
4. Move to a saved reference row and press/release Space to open its floating card.
5. Move Ghost over the card, hold Space, move with WASD/arrows, and release Space. The card follows Ghost; the Windows mouse does not move.
6. With Ghost over the card, use **Pin** or **Unpin**. Use **Hide card** to close it. Only registered, visible Invey-owned targets can respond.
7. Choose **Stop Ghost** or press Escape. Stop cancels any hold and hides the ring.

The ordinary mouse remains independently usable. It may activate whichever window you click, as expected. A passive Ghost action does not promise to preserve every foreground focus state.

## Workspace and recorded tracking

- Choose **Workspace** to load two saved references and use the ordinary mouse controls. The camera-free controlled route demonstrates independent logical ownership; it does not create a second native Windows cursor.
- Choose **Ghost Tracking** for calibration/practice or a clearly labeled recorded/simulated preview. Recorded video is not proof of physical hand accuracy and cannot operate Pocket through live gesture intent.
- **Settings** retains shortcut, calibration and personal learning controls. Learning is optional for the controlled beta.

## Report a problem

Choose **Report a problem**, select a category, and fill in the short description, steps, expected result and actual result. Nothing is sent or saved automatically. **Copy feedback report** deliberately copies the text so you can review it and paste it into ChatGPT. Optional diagnostics contain only Ghost mode/source and timing counts; no images, frames, screen text, target names, coordinates or identifiers are included.

## Camera and experimental behavior

Camera is off at startup. The previously audited diagnostic camera allowance is exhausted; do not try to work around the guardian. Live camera preview and live hand-to-Pocket control are not qualified by this beta. No gesture can currently operate Pocket in production. See [the physical test plan](INVEY_LIVE_PHYSICAL_TEST_PLAN.md) before considering a separately authorized session.

## Close

Choose **Stop Ghost**, then **Exit** in the main Invey window. Do not terminate it through Task Manager unless it is genuinely unresponsive; report the issue instead. The launcher will refuse to start a second instance and will not kill the existing app.
