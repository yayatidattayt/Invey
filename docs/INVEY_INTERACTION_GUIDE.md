# Using Invey’s two pointers

Ghost is a separate logical pointer. The ordinary Windows mouse keeps its normal position and behavior. The current complete demonstration uses explicitly CONTROLLED input, with the camera off. Real hand actuation remains gated.

## Launch the isolated demo

From the repository folder in PowerShell:

```powershell
./scripts/Start-Invey-DualCursor.ps1
```

This uses the existing demo library with two labeled synthetic references, preserves normal data, and retains the exhausted camera guardian. For normal saved references, explicitly add -NormalProfile.

## Try it

1. Open **Controlled Ghost Demo** from Invey’s main window.
2. Press **Start preview**. A small Ghost ring and a Pocket tab appear; the Windows cursor stays independent.
3. Press **Refresh targets**, choose **Reveal Pocket shelf**, and press **Move Ghost to target**.
4. Press **Arm Pocket control**, then **Activate**. Preview alone cannot open anything.
5. Refresh targets, choose **Open [reference title]**, move Ghost there, and Activate. The real saved reference opens in a floating window.
6. Refresh targets again and choose **Card · [title]**. Move Ghost there, press **Press**, use the direction buttons to move it, then **Release**.
7. Use **Pin** or **Unpin** while Ghost is over that card. **Hide** closes that card; its saved reference remains in Pocket and can be reopened.
8. To dismiss the shelf, aim at **Dismiss Pocket shelf** and Activate. This disarms control; pinned cards stay visible. Arm again for another action.

Controls act only on visible, reachable Invey targets. A pinned card may legitimately cover another target. Move the covering card or use a supported keyboard command. Ghost does not click through it.

**Disarm** keeps a preview while preventing commands. **Stop**, Escape in the control window, or **Pause Ghost** on the main window releases the logical hold. The main Pause action also stops tracking. The Tracking lab’s own **Stop** releases its worker/camera; stopping only the controlled demo does not claim a camera-release result.

## Workspace and both owners

Load different references into A and B. Refresh targets in Controlled Ghost Demo and aim at **Workspace B**. Arm, Press, move and Release through the same desktop pointer.

For a hands-free controlled motion example, Workspace’s labeled **Simulated Ghost demo** uses the shared pointer to move B. The physical mouse can hold A independently. A target already owned by Mouse cannot be acquired by Ghost. Closing Workspace releases its claims.

Workspace is a normal focusable window. Its unique **Enlarge** shortcut is Alt+E. Floating-card and passive-pointer commands avoid activation, but universal focus behavior across unrelated applications is not promised.

## Recorded or camera preview

Open **Ghost Tracking**, then **Calibrate**. Choose the display, selected hand, mapping mirror and working region; save your settings. Enable **Desktop Ghost preview · no live actions**, then deliberately enable Recorded tracking. Fresh qualified observations move the same desktop ring.

The camera-image mirror is separate from horizontal pointer mapping. Accepted personal correction is used only when its saved context matches, before the one precision filter. Partial/visual continuation remains preview-only.

Recorded/camera sources cannot arm Pocket commands. Starting Controlled Ghost Demo explicitly replaces the desktop preview source. Starting tracking by itself never arms control.

The existing real-camera allowance is exhausted. This execution grants no new start. Physical hand accuracy, live gestures, mixed-DPI hardware and cold isolated-fingertip acquisition remain separate qualification work.

## Loss and recovery

Missing, ambiguous or stale evidence releases a held target. Stop invalidates the source token, so an old event cannot restart a drag. Closed/hidden targets unregister or become unavailable. After interruption, start/aim/arm deliberately again.

Camera-free native tests verify the supported workflow on an isolated Windows desktop. They do not establish physical hand control on the ordinary desktop.
