# Invey / Spatial Minimalism

Invey is the product. Ghost is its independent interaction system; Pocket is optional and Cube is planned. No recurring paid runtime or service is required.

Implemented foundation: Base #0B0E13, Surface #151B24, Elevated #243141, Ink #EDF3FA, Muted #A7B5C7, Accent #248CFF, Cyan #54D9F2. Shared resources are in Themes/Spatial.xaml. Segoe UI Variable with Segoe UI fallback, 14 DIP body, 24–26 DIP headings, 8 DIP button radius, visible cyan keyboard focus. Accent marks interaction, not whole panels. No light-mode toggle.

Home uses a left collapsible navigation rail linked to existing windows. Tooltips and automation names survive compact mode. Pause remains available in the rail. Cube is explanatory text, not a dead command. Existing launch commands, diagnostics, feedback, replay and Exit remain accessible in the scrolling content.

Tesseract: bit-coded ±1 four-dimensional vertices, 32 one-coordinate edges; perspective 4D→3D followed by yaw/pitch and perspective screen projection. FrameworkElement draws only wire edges. Drag impulses clamp to40 DIP/event; arrows rotate; Home resets; Escape/capture loss/hide/unload release drag. No ambient render timer or inertia yet, so no continuing animation under reduced motion. Camera, optical tracking and pointer filtering are untouched.

Ghost cursor has transparent fill in both desktop and legacy overlays. Existing real mode and hover affect the ring; passive native styles remain unchanged. Camera preview grows to300 DIP on a dark surface, retaining original sequence matching, aspect ratio, mirror and clean/fingertip/full overlay modes.

Remaining design work: seamless WindowChrome shell, bounded inertia, animated rail transitions, complete control templates including dropdown/list states, feature-window hardcoded surface audit, substantial Ghost layout/status redesign, responsive preview height, accessibility/high-contrast review, per-monitor DPI and visual state qualification. Detachable preview deferred pending ownership review. This is a foundation, not the completed design system.

Native chrome reference reviewed: https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome?view=windowsdesktop-10.0 . Existing native chrome retained until replacement is tested.
