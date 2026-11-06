# GHOST isolated Windows acceptance runner

The Execution 04 short profile includes gated provider mutations, failed termination and late acknowledgement, and the WPF fixture recovery controls. The typing check batches each synthetic chunk and retains its expected digest if the acknowledgement fails. Results remain scoped to the runner's private desktop.

## Execution03 runner — current instructions

Run from the project root:

```powershell
& .\scripts\Run-AutomatedAcceptance.ps1 -Build -Profile short -Seed 73021
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile standard -Seed 99173
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile extended -Seed 41007
```

Default profile is standard. Typing profiles run20/60/100 cycles in20-cycle batches, with seeded variable chunks, exact SHA256/length acknowledgement, focus checks and stop/restart between batches. Timeout budgets are150/240/360 seconds; the parent's desktop-restoration watchdog fires5 seconds later. These replace the older105/110-second constants below.

New guarded fixture-only checks cover scoped under-overlay physical-point lookup, duplicate/disabled/missing/replaced elements, token expiry and window recreation, range bounds/fractional/read-only/rejected/ignored values, toggle transitions, scroll boundary/non-scrollable state, strict IPC correlation and duplicate replies, provider cancellation/termination, worker restart and resource spot samples. WPF AutomationPeer callbacks run in a separate disposable provider process; an entry marker proves a genuine provider callback blocked, distinct from the explicit hung-worker double. A separate standard WinForms/Win32 fixture supplies independent compatibility state.

Final harness versions export per-chunk typing-chunks.jsonl, main results.json/results.md, adverse-provider entry/release/cleanup markers and binary hashes. Earlier run formats remain intact. The primary app is queried independently while the provider hangs. Timeouts never automatically retry actions; OutcomeUnknown means an action could already have reached a provider. Killing the worker is containment, not rollback. Neither B1 nor consumer external-control UI is enabled.

Latest actual evidence and unexecuted cases are recorded in docs/EXECUTION_03_REPORT.md. Earlier instructions/results below describe historical Execution02 behavior.


Run from project root: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-AutomatedAcceptance.ps1 -Build

Requires the existing local .NET10 SDK and ordinary interactive Winsta0/Default session. Creates a unique private desktop, launches a separate runner child and a guarded development-mode GHOST fixture process there, switches only for bounded test duration, restores original desktop in finally/timeout watchdog, and kills only the job-owned acceptance tree. Refuses locked/secure/unexpected input desktops. Does not change display scaling, install drivers, target unrelated apps or capture screen pixels.

Input.cs contains TEST-ONLY SendInput. No product project references it. Before each event it verifies input-desktop identity and foreground fixture PID. Parent job object kills test children on teardown; supervisor timeout105s and restoration watchdog110s bound ordinary child stalls. Pressed mouse/modifier cleanup occurs only on the designated test desktop. Abrupt supervisor termination/power loss restoration is not certified; normal failures and child exit are handled. Do not interrupt the short desktop switch with manual input; raw hardware is shared and manual movement would make cursor controls inconclusive.

Evidence: unique artifacts/automated-acceptance/<UTC-time-GUID>/results.json and results.md, including classes LOGIC_TEST (separate TRX), NATIVE_SOFTWARE_TEST and INJECTED_GUI_TEST. No HUMAN_PHYSICAL_TEST or DEVICE_ORIGIN_TEST is implied. Overall JSON's environment-gap summary is not full A1 acceptance.

Cases:
- Three120-sample GetPhysicalCursorPos controls with injection disabled: Ghost closed, app idle, replay active. No cursor reset.
- No-overlay click control,100 clicks through rendered panel,100 drag starts/ends,560 deterministic characters across20 replay cycles verified by per-chunk and final SHA256,20 active native drag/Ghost progression trials.
- Stop/restart, overlay close during active press, process/window cleanup.
- Actual current125% DPI metadata; other scales not fabricated. Mathematical transforms are covered by Core tests.
- Alt+Tab/tray marked BLOCKED: private desktop has no Explorer shell. Normal desktop deliberately not automated.
- A2-PROVISIONAL: four UIA patterns against an explicitly registered benign fixture, consumed-token/wrong-PID rejection, under-overlay point query with honest Unsupported, bounded watchdog using a clearly labeled stalled-worker test double.

Fault history retained:
1. Initial desktop-name check exposed missing Unicode marshalling (reported 'D'); corrected wide-string declaration.
2. Initial injected routing timed out with and without overlay. Failure snapshots exposed DPI mapping disagreement. Runner now establishes per-monitor awareness and product launched through manifest-bearing .exe with local DOTNET_ROOT, not DLL-hosted manifest assumptions. No-overlay and overlay routing then passed.
3. One later run had a text digest mismatch. Per-chunk exact digest acknowledgements strengthened synchronization (previously only length acknowledged); final rerun passed. Earlier failed run retained; long-run typing stability still merits further soak. No root cause attributed to GHOST from that one mismatch.
4. Overlay closure previously had no explicit replay-timer closure subscription; now Closed cancels the timer/claim and detaches on disposal, validated by native regression.

Artifacts preserve both failed and passed histories. No source commit assumed; AppSha256 identifies tested binary. Newly initialized local repository uses codex/a1-automation-a2-provisional, no release merge, all existing source preserved and uncommitted. Pre-A2 snapshot artifacts/a1-before-provisional-a2.zip.

A2 worker/client are fixture-only provisional slices. No consumer external-action toggle, arbitrary app integration, UIAccess, cloud, camera or B1 work. IPC uses fixed known targets, finite values, bounded messages,300ms single-use capabilities with runtime-ID validation; synchronous COM is isolated on windowless MTA. Watchdog never retries an uncertain action automatically. Real-provider hangs and real-app compatibility remain unverified.
