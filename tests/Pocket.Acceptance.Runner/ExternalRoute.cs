using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Pocket.Automation.Contracts;
using Pocket.Automation.Client;

namespace Pocket.Acceptance.Runner;
internal static partial class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="SetWindowPos")]private static extern bool SetWindowPosE27(nint window,nint after,int x,int y,int width,int height,uint flags);
    private static async Task RunExternalSession(string app,string output)
    {
        var(host,client)=StartAcceptanceHost(app,output);
        using(host)using(client)
        try {
            await client.Connect();var state=await client.Command("status");
            await RunExternalRoute(client,output);await RunWindowsCompatibility(client,output);
            var demoUi=await client.Command("external|demo-ui");
            int ownedDemo=DesktopGhost(demoUi).GetProperty("External").GetProperty("OwnedDemoPid").GetInt32();
            Require(ProcessExists(ownedDemo),"UI demo did not launch");
            await ExitAndVerifyAsync(client,host,state.GetProperty("OverlayHandle").GetInt64(),state.GetProperty("FixtureHandle").GetInt64());
            Require(!ProcessExists(ownedDemo),"UI-owned demo remained after Exit");
        } finally {if(!host.HasExited){host.Kill(true);Require(host.WaitForExit(3000),"External host cleanup unconfirmed");}}
    }
    private static async Task RunExternalRoute(Client client,string output)
    {
        await Measure("E27_EXTERNAL_SHARED_GHOST", "CONTROLLED_GHOST + REAL_SECOND_PROCESS_UIA", "Six patterns, real external state, denied stale/sensitive/covered targets, unchanged native cursor and cleanup",async()=>
        {
            Desktop.Guard();
            string pipeName="InveyExternalDemo-"+Guid.NewGuid().ToString("N");
            var info=new ProcessStartInfo(Path.GetFullPath("src/Invey.ExternalDemo/bin/Release/net10.0-windows/Invey.ExternalDemo.exe")) {UseShellExecute=false,CreateNoWindow=true};
            info.Environment["DOTNET_ROOT"]=Path.GetFullPath(".tools/dotnet");
            info.ArgumentList.Add("--pipe");info.ArgumentList.Add(pipeName);
            using var demo=Process.Start(info)??throw new IOException("Demo did not start");
            using var pipe=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous);
            await pipe.ConnectAsync(10000);
            using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
            using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
            async Task<JsonElement> Demo(string command="state"){await writer.WriteLineAsync(command);return JsonDocument.Parse((await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(4)))!).RootElement.Clone();}
            var outcomes=new List<object>();
            try
            {
                await client.Command("external-demo-start");await client.Command("ghost-pattern-hide");
                await client.Command("product-minimize"); // Keep the external target uncovered by the larger shell.
                await client.Command("ghost-start");await client.Command("ghost-controls");await client.Command("ghost-controls-place");
                var s=await Demo();long hwnd=s.GetProperty("Handle").GetInt64();Require(s.GetProperty("Pid").GetInt32()==demo.Id,"Not a separate process");
                // Selection/consent and actions use the actual product controller; no second pointer.
                async Task<JsonElement> Select(string pattern,string? pointName=null)
                {
                    var state=await Demo();var point=state.GetProperty("Points").GetProperty(pointName??pattern);
                    await client.Command($"external|select|{hwnd}|{pattern}");
                    await client.Command("external|move|"+point.GetProperty("X").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture)+"|"+point.GetProperty("Y").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture));
                    await client.Command("external|arm");
                    return await client.Command("external|probe");
                }
                var uiHover=await Select("Invoke");
                await Demo("permit-focus|"+Environment.ProcessId);
                await FocusTrackingAsync(client,DesktopGhost(uiHover).GetProperty("ControlsHandle").GetInt64());
                var steering=DesktopGhost(await client.Command("status")).GetProperty("KeyboardSteeringPoint");
                var ghostBeforeMouse=DesktopGhost(await client.Command("status")).GetProperty("Position").GetRawText();
                Input.Click(steering.GetProperty("X").GetDouble(),steering.GetProperty("Y").GetDouble());
                Require(DesktopGhost(await client.Command("status")).GetProperty("Position").GetRawText()==ghostBeforeMouse,"Physical mouse changed Ghost position");
                var nativeBefore=Input.Cursor();var keyboardForeground=Input.Foreground;int countBefore=(await Demo()).GetProperty("Count").GetInt32();
                Input.Key(0x45,false);Input.Key(0x45,true);
                var uiWait=Stopwatch.StartNew();while((await Demo()).GetProperty("Count").GetInt32()==countBefore&&uiWait.ElapsedMilliseconds<3000)await Task.Delay(20);
                Require((await Demo()).GetProperty("Count").GetInt32()==countBefore+1,"User-facing E action did not invoke the separate process: "+DesktopGhost(await client.Command("status")).GetProperty("External").GetRawText());
                await Until(client,state=>DesktopGhost(state).GetProperty("External").GetProperty("Unknown").GetBoolean()&&!DesktopGhost(state).GetProperty("External").GetProperty("Busy").GetBoolean());
                var nativeAfter=Input.Cursor();Require(nativeBefore.X==nativeAfter.X&&nativeBefore.Y==nativeAfter.Y,"Controlled E action moved native cursor");
                outcomes.Add(new{ActualKeyboardActivation=true,PhysicalMouseDoesNotMoveGhost=true,NativeCursorUnchanged=true,ForegroundBefore=keyboardForeground.ToInt64(),ForegroundAfter=Input.Foreground.ToInt64()});
                await client.Command("external|dismiss");await client.Command("external|stop");
                foreach(string pattern in new[]{"Invoke","Toggle","RangeValue","Scroll","ExpandCollapse","SelectionItem"})
                {
                    var hover=await Select(pattern);var external=DesktopGhost(hover).GetProperty("External");
                    Require(external.GetProperty("Hover").GetString()==pattern && external.GetProperty("HighlightVisible").GetBoolean(),"No actual hover/highlight for "+pattern+": "+external);
                    var before=await Demo();var cursor=Input.Cursor();var foreground=Input.Foreground;
                    var result=await client.Command("external|activate");var after=await Demo();var end=Input.Cursor();
                    Require(cursor.X==end.X&&cursor.Y==end.Y,"External action moved native mouse");
                    string field=pattern switch{"Invoke"=>"Count","Toggle"=>"Toggle","RangeValue"=>"Range","Scroll"=>"Scroll","ExpandCollapse"=>"Expanded",_=>"Selected"};
                    Require(before.GetProperty(field).ToString()!=after.GetProperty(field).ToString(),"External visible state did not change for "+pattern);
                    outcomes.Add(new{pattern,ProbeMs=external.GetProperty("LastProbeMs").GetDouble(),ExecuteMs=DesktopGhost(result).GetProperty("External").GetProperty("LastExecuteMs").GetDouble(),Before=before.GetProperty(field).ToString(),After=after.GetProperty(field).ToString(),CursorUnchanged=true,ForegroundBefore=foreground.ToInt64(),ForegroundAfter=Input.Foreground.ToInt64(),Feedback=DesktopGhost(result).GetProperty("External").GetProperty("Status").GetString()});
                    if(pattern=="Invoke")
                    {
                        var review=await client.Command("external|review");
                        Require(DesktopGhost(review).GetProperty("External").GetProperty("Unknown").GetBoolean(),"Invoke review manufactured a certain outcome");
                        Require((await Demo()).GetProperty("Count").GetInt32()==after.GetProperty("Count").GetInt32(),"Reconciliation replayed Invoke");
                    }
                    File.WriteAllText(Path.Combine(output,"external-pattern-progress.json"),JsonSerializer.Serialize(outcomes));
                    await client.Command("external|dismiss");await client.Command("external|stop");
                }
                var rapid=await client.Command("external|rapid|"+hwnd);
                Require(!DesktopGhost(rapid).GetProperty("External").GetProperty("Granted").GetBoolean()&&DesktopGhost(rapid).GetProperty("External").GetProperty("Worker").ValueKind==JsonValueKind.Null,"Late authorization survived Stop");
                outcomes.Add(new{RapidConsentStopRevoked=true});
                // Actual native mouse holds a real Workspace card while Ghost toggles a separate app.
                await Demo("permit-focus|"+Environment.ProcessId);
                var main=await client.Command("status");await FocusTrackingAsync(client,ProductHandle(main));AccessKey(0x57);
                var board=await Until(client,state=>Board(state).ValueKind==JsonValueKind.Object);
                long boardHandle=Board(board).GetProperty("Handle").GetInt64();
                await FocusTrackingAsync(client,boardHandle);AccessKey(0x41);
                board=await Until(client,state=>Board(state).GetProperty("Cards").GetArrayLength()==1);
                SetWindowPosE27(new(boardHandle),0,680,90,0,0,0x15);
                SetWindowPosE27(new(hwnd),0,0,0,0,0,0x13);
                var grip=Slot(await client.Command("status"),"A").GetProperty("Grip");
                Input.Move(grip.GetProperty("X").GetDouble(),grip.GetProperty("Y").GetDouble());Input.Down();
                try
                {
                    await Until(client,state=>Claim(state,"Mouse")=="A");
                    var holdInitialForeground=Input.Foreground;
                    await Select("Toggle");var heldCursor=Input.Cursor();var heldForeground=Input.Foreground;
                    var beforeHeld=(await Demo()).GetProperty("Toggle").GetBoolean();
                    var activated=await client.Command("external|activate");
                    Require((await Demo()).GetProperty("Toggle").GetBoolean()!=beforeHeld,"External action did not occur during physical hold");
                    bool mousePreserved=Claim(activated,"Mouse")=="A";
                    bool focusPreserved=Input.Foreground==holdInitialForeground;
                    if(focusPreserved)Require(mousePreserved,"Physical Mouse claim lost despite preserved foreground");
                    else Require(Claim(activated,"Mouse") is null or "A","Focus transition corrupted ownership");
                    var afterHeld=Input.Cursor();Require(heldCursor.X==afterHeld.X&&heldCursor.Y==afterHeld.Y,"External action displaced held mouse");
                    outcomes.Add(new{ActualMouseHold="Workspace A",GhostExternal="Toggle",MouseClaimPreserved=mousePreserved,FocusPreserved=focusPreserved,ForegroundBefore=holdInitialForeground.ToInt64(),ForegroundBeforeAction=heldForeground.ToInt64(),ForegroundAfter=Input.Foreground.ToInt64(),SupportedDuringHeldMouse=mousePreserved});
                }
                finally{Input.ReleaseAll();}
                await client.Command("external|dismiss");await client.Command("external|stop");await client.Command("product-close-board");
                foreach(var point in new[]{"Password","Disabled"})
                {
                    var reject=await Select("Invoke",point);Require(DesktopGhost(reject).GetProperty("External").GetProperty("Hover").ValueKind==JsonValueKind.Null,"Sensitive/disabled point accepted");
                    await client.Command("external|stop");
                }
                await Demo("reset-scroll");await Task.Delay(50);
                var sensitive=await Select("Scroll","SensitiveScroll");
                Require(DesktopGhost(sensitive).GetProperty("External").GetProperty("Hover").ValueKind==JsonValueKind.Null,"Sensitive child fell through to parent scroll capability");
                var secretBefore=(await Demo()).GetProperty("Scroll").GetDouble();await client.Command("external|activate");
                Require((await Demo()).GetProperty("Scroll").GetDouble()==secretBefore,"Sensitive-point scroll executed");
                await client.Command("external|stop");
                await Select("Toggle");var beforeCover=(await Demo()).GetProperty("Toggle").GetBoolean();await Demo("cover");var covered=await client.Command("external|activate");Require((await Demo()).GetProperty("Toggle").GetBoolean()==beforeCover,"Covered control executed");await Demo("uncover");await client.Command("external|stop");
                await client.Command("ghost-stop");await client.Command("external|stop");
                // Low-level extension negatives use same supervised worker, never fixture credentials.
                var choice=ExternalWindows.List().Single(c=>c.Window==hwnd);
                var grant=ExternalWindows.Consent(choice,Guid.NewGuid(),["Toggle"]);
                await using(var worker=new WorkerClient(Path.GetFullPath(".tools/dotnet/dotnet.exe"),Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll"),grant))
                {
                    var state=await Demo();var p=state.GetProperty("Points").GetProperty("Toggle");
                    Request Probe()=>new(2,Guid.NewGuid().ToString(),"ProbePoint",hwnd,demo.Id,"Toggle",X:p.GetProperty("X").GetDouble(),Y:p.GetProperty("Y").GetDouble(),Grant:grant.Id,Generation:grant.Generation);
                    var probe=Probe();var cap=await worker.RequestAsync(probe);Require(cap.Token is not null,"No capability: "+JsonSerializer.Serialize(cap));await Task.Delay(350);
                    var expired=await worker.RequestAsync(probe with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token});Require(expired.Outcome!=MutationOutcome.Applied,"Expired capability executed");
                    cap=await worker.RequestAsync(Probe());await Demo("move");
                    var moved=await worker.RequestAsync(probe with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token});Require(moved.Outcome!=MutationOutcome.Applied,"Moved target executed");
                    state=await Demo();p=state.GetProperty("Points").GetProperty("Toggle");
                    var fresh=Probe();cap=await worker.RequestAsync(fresh);int crashedPid=worker.ActiveProcessId!.Value;
                    using(var ownedWorker=Process.GetProcessById(crashedPid)){ownedWorker.Kill();Require(ownedWorker.WaitForExit(2000),"Fault worker did not exit");}
                    var crashed=await worker.RequestAsync(fresh with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token});
                    Require(crashed.Outcome!=MutationOutcome.Applied,"Worker replacement executed old token");
                    outcomes.Add(new{WorkerCrash=crashed.Status});
                    var denied=await worker.RequestAsync(Probe() with{Generation=Guid.NewGuid()});Require(denied.Status=="Denied","Wrong generation accepted");
                    outcomes.Add(new{Expired=expired.Status,Moved=moved.Status,WrongGeneration=denied.Status});
                }
                // An actually exited separate process cannot reuse its freshly hovered token.
                var secondInfo=new ProcessStartInfo(Path.GetFullPath("src/Invey.ExternalDemo/bin/Release/net10.0-windows/Invey.ExternalDemo.exe")){UseShellExecute=false,CreateNoWindow=true};
                secondInfo.Environment["DOTNET_ROOT"]=Path.GetFullPath(".tools/dotnet");
                using(var second=Process.Start(secondInfo)??throw new IOException("Second lifetime fixture did not start"))
                {
                    try
                    {
                        ExternalWindows.Choice? target=null;var ready=Stopwatch.StartNew();
                        while(ready.ElapsedMilliseconds<4000){target=ExternalWindows.List().FirstOrDefault(c=>c.ProcessId==second.Id);if(target is not null)break;await Task.Delay(20);}
                        Require(target is not null,"Second process window missing");
                        var secondGrant=ExternalWindows.Consent(target!,Guid.NewGuid(),["Toggle"]);
                        await using var lifetimeWorker=new WorkerClient(Path.GetFullPath(".tools/dotnet/dotnet.exe"),Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll"),secondGrant);
                        var point=s.GetProperty("Points").GetProperty("Toggle");
                        var request=new Request(2,Guid.NewGuid().ToString(),"ProbePoint",target!.Window,second.Id,"Toggle",X:point.GetProperty("X").GetDouble(),Y:point.GetProperty("Y").GetDouble(),Grant:secondGrant.Id,Generation:secondGrant.Generation);
                        var token=await lifetimeWorker.RequestAsync(request);Require(token.Token is not null,"Fresh lifetime token missing");
                        second.Kill();Require(second.WaitForExit(2000),"Owned lifetime target did not close");
                        var gone=await lifetimeWorker.RequestAsync(request with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=token.Token});
                        Require(gone.Outcome!=MutationOutcome.Applied,"Closed process capability executed");outcomes.Add(new{ClosedTarget=gone.Status});
                    }
                    finally{if(!second.HasExited){second.Kill();second.WaitForExit(2000);}}
                }
                await client.Command("external|recorded");var deniedSource=await client.Command($"external|select|{hwnd}|Toggle");Require(!DesktopGhost(deniedSource).GetProperty("External").GetProperty("Granted").GetBoolean(),"Recorded gained external grant");
                await client.Command("ghost-start");await Select("Invoke");
                await Demo("slow-provider");
                long updatesBefore=DesktopGhost(await client.Command("status")).GetProperty("PointerUpdates").GetInt64();
                var timeoutClock=Stopwatch.StartNew();var timed=await client.Command("external|activate");
                var uncertain=DesktopGhost(timed).GetProperty("External");
                Require(uncertain.GetProperty("Unknown").GetBoolean(),"Blocked provider did not preserve unknown outcome");
                Require(DesktopGhost(timed).GetProperty("PointerUpdates").GetInt64()>updatesBefore+10,"Provider blocked the WPF pointer dispatcher");
                Require(uncertain.GetProperty("Worker").ValueKind==JsonValueKind.Null,"Blocked worker was not independently stopped");
                outcomes.Add(new{ProviderTimeoutMs=timeoutClock.ElapsedMilliseconds,Unknown=true,WpfResponsive=true});
                var duplicate=await client.Command("external|activate");Require(DesktopGhost(duplicate).GetProperty("External").GetProperty("Unknown").GetBoolean(),"Uncertain action auto-retried");
                await client.Command("external|stop");
                demo.Kill();Require(demo.WaitForExit(3000),"Hung owned demo did not exit");
                // Closed window grant is rejected even after the old HWND was hovered.
                var closed=await client.Command("external|activate");Require(!DesktopGhost(closed).GetProperty("External").GetProperty("Granted").GetBoolean(),"Closed target retained grant");
                await client.Command("external|stop");
                File.WriteAllText(Path.Combine(output,"external-results.json"),JsonSerializer.Serialize(outcomes));
                return new{SeparateProcess=true,Patterns=outcomes,CameraOpened=false,CursorUnchanged=true,WorkerExited=true};
            }
            finally {await client.Command("external|stop");if(!demo.HasExited){demo.Kill();demo.WaitForExit(3000);}}
        });
    }
}

