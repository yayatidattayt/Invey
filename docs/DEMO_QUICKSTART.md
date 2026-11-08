# Invey demo quickstart

The camera-free control-route rehearsal passed twice. Automated native focus was blocked in the remote test environment, so complete the short manual checklist below before the friend demonstration.

From the repository folder in PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Invey-Demo.ps1 -DemoProfile
```

This changes policy for that PowerShell process only. It requires no administrator permission and changes no global policy. The camera stays off. The optional profile is %LOCALAPPDATA%\Invey-Demo\Execution17, separate from the normal Invey and legacy libraries. It receives two labeled synthetic references on first initialization. They are sample content, not desktop captures. Later launches preserve demo edits and captures.

Use the same command without -DemoProfile for your normal profile. Exit Invey before switching. Close the app using **Exit** (Alt+X in the main window), rather than assuming the title-bar close has exited the tray app.

The two demo samples have been prepared on the verified laptop. To prepare them without opening windows, add -DemoProfile -PrepareOnly. This never starts tracking.

Use -CheckOnly to inspect prerequisites without opening the application. Add -Rebuild only if the script reports an outdated or missing build; it uses the existing locked restore and pinned SDK and stops on failure.

## A short route

1. **Pocket**: open either DEMO reference with **Open reference**. Pin the floating card, tuck the shelf, and reopen Pocket.
2. **Capture region**: select the intended display. Select a non-private area, choose Preview, then Save. Cancel is always available; a black preview requires explicit confirmation. Your demo capture enters only the demo library.
3. **Workspace**: select a saved reference and Load A; select the other and Load B. Select a target and use Move, Smaller/Larger, Fit or100% zoom. **Simulated Ghost demo** moves B for about two seconds using the logical channel (click again to stop); it is not camera control.
4. **Ghost Tracking**: leave Recorded selected. Enable the recorded model sequence. The optional image preview uses the approved sample, never a fake live feed. Choose **SIMULATED calibration practice** while stopped for the controlled practice sequence, then Enable and Calibrate.
5. **Stop** before changing sources. **Settings** contains the existing shortcut configuration and conflict feedback.
6. Exit and relaunch with the same demo command to show saved references and supported pinned-card restoration.

## Live camera and remote use

No automated preparation activates the webcam. The creator may deliberately select Live camera, Find cameras, select a device, select Show local camera view, and Enable when physically appropriate. Stop releases it. Existing diagnostic guardian limits are preserved. Remote software may expose different cameras, monitors or DPI; use Refresh displays and select a valid display.

Physical wrist-out finger tracking remains unreliable. No Ghost-to-Pocket or OS-wide gesture actuation is demonstrated. Recorded and simulated input do not prove physical accuracy.

## Manual pre-demo checks

Confirm the normal tray is available in the intended remote session; test the remote display and real capture area; check the two demo samples; verify shortcuts are not taken by the remote client; confirm sound/video-sharing arrangements separately. These conditions cannot be guaranteed by an isolated desktop rehearsal.
