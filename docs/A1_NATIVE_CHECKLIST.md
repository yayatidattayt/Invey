# A1 — Physical acceptance and commands

Run from GHOST_CODEX_PROJECT_READY with dummy fixture content. No injected input can substitute for physical acceptance.

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','--no-restore','-p:Platform=x64','-bl:artifacts/build-debug.binlog')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Debug','--no-build','--no-restore','--logger','trx;LogFileName=a1-core.trx','--results-directory','artifacts/tests')
& .\scripts\dotnet.ps1 -Arguments @('run','--project','src/Pocket.App/Pocket.App.csproj','-c','Debug','--no-build')
```

Release: replace Debug by Release in build/test commands. Native smoke: pass @('src/Pocket.App/bin/Release/net10.0-windows/Pocket.App.dll','--native-smoke') to the wrapper. It opens dummy-content windows for20 cycles, writes a unique artifacts/a1-native/<UTC-time-GUID>/native-smoke.json, and exits. Read its explicit Status; exit0 does not certify physical acceptance. Old evidence is preserved.

## Guided human session (Execution 01.5)

Launch Release controller with `& .\scripts\dotnet.ps1 -Arguments @('run','--project','src/Pocket.App/Pocket.App.csproj','-c','Release','--no-build')`. Use actual physical devices. Results start as NOT RUN in A1_PHYSICAL_ACCEPTANCE_RESULTS.md; agent-run UI automation is not human evidence.

1. Preparation: Open test fixture, then Show overlay. Move the fixture so its click button lies behind the opaque Pocket rectangle and GHOST ring. Note resolution/scale and starting counters. PASS preparation if visible content/control locations are clear; otherwise describe setup problem.
2. Mouse/clicks: physically move freely, then perform100 clicks and100 drag starts as required by AC-F02, crossing rendered panel/ring. Use slider for drag-start counter. Expect100/100 received, no unexpected cursor jumps/actions, each drag releases (Active=False, canceled count unchanged for normal drags). Export a named JSON afterward. Counter events cannot prove device origin: explicitly report physical use. Record failure trial/counter/region.
3. Typing: Start timed replay; focus fixture editor within5s, type a harmless known sentence through20 show/hide cycles. Expect exact match and no focus theft while replay continues. Report only exact-match yes/no, not text content; export JSON. If one sentence ends too early, repeat/continue typing through all cycles.
4. Continuous drag: repeat replay as needed; physically hold and move the slider while Pocket moves,20 trials. Expect continuous drag and correct release, no canceled drag/stuck Active state. Report attempted/passed counts, mismatches/cancel count and JSON path.
5. Stationary mouse: a separate replay after positioning the cursor; keep mouse/touchpad untouched for the replay. Expect exactly unchanged cursor coordinates for>=100 Ghost moves. Export after replay; moving to Export afterward is not part of recorded replay. If touched inadvertently, mark INCONCLUSIVE and repeat, never PASS.
6. Switching/lifecycle: use physical Alt+Tab while replay runs; expect normal switching, no overlay entry/activation and continuing simulation. Try controller/tray Open, Settings, Stop, stop during drag, restart, controller close/reopen, second launch and final Exit. Expect canceled Ghost only, no duplicate loop/window, no leftover blocker. Export before Exit; report visible outcomes because export alone cannot certify menus/Alt+Tab.
7. DPI: repeat overlap/alignment at100/125/150% where available and test display-change cancellation/recreation. Change Windows settings only if you agree; record any configuration not tried/unavailable. Mixed-monitor interaction remains deferred. No general multi-monitor acceptance claimed.

Return: `A02 clicks__/100; drag starts__/100; A03 exact-match__/20 cycles; A04__/20; A05 stationary yes/no; A10 yes/no; DPI tested__; JSON filenames__; physical mouse/keyboard used yes/no`. Add PASS/FAIL/INCONCLUSIVE and concise reproduction for any failure. H observations are user-reported until reviewed, not retroactively agent-observed. No arbitrary keystrokes/clipboard/screenshots requested.

1. Record Windows/app build, integrity, monitor resolution/DPI and foreground handle. Open native fixture; place it behind visible Pocket/GHOST.
2. A02: physical100/100 clicks/drag starts through opaque panel/GHOST; record fixture counter and scrolling. Empty transparent pixels are insufficient.
3. A03: Start timed replay, focus editor during5s countdown, type through20 show/hide cycles. No character loss or forced foreground restoration.
4. A04: maintain physical slider/text drag while GHOST moves Pocket,20 trials; record actual drag continuity separately from focus.
5. A05: physical mouse stationary through>=100 GHOST movements; compare exported cursor coordinates. Accidental physical movement makes trial inconclusive.
6. A01/A10: second launch reopens single instance; tray Open/Settings/Stop/Exit; close/reopen; Start twice/Stop mid-drag/restart; Alt+Tab/taskbar exclude overlay. Exit must remove all app windows/timers.
7. A11: alignment and behavior at100/125/150% when available; resolution/DPI change cancels/hides then recreation works. Change display settings only with user agreement.
8. Export numeric evidence; log observed/expected/counts/actual evidence paths and PASS/FAIL/NOT RUN. No screenshots/private content without explicit permission.

P0 cursor movement caused by GHOST, input blocking, passive focus theft or stuck drag blocks A1. Current automated smoke does not replace these trials. Remain at A1 review; no webcam/UIA.

