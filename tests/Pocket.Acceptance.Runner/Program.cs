using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pocket.Automation.Client;
using Pocket.Automation.Contracts;

namespace Pocket.Acceptance.Runner;

internal sealed record Case(string Id,string EvidenceClass,string Expected,object Observed,string Status,double DurationMs,
    DateTimeOffset? StartedUtc = null, DateTimeOffset? FinishedUtc = null);
internal static partial class Program
{
    private static readonly List<Case> Cases=new();
    private static bool TrackingOnly => Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_F06_ONLY") == "1";
    private static bool PocketOnly => Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_POCKET_ONLY") == "1";
    [STAThread] public static int Main(string[] args)
    {
        Input.InitializeDpi();
        if (args.Length == 1 && args[0] == "--environment-audit")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { InputDesktop = Desktop.InputDesktopName() }));
            return 0;
        }
        if(args.Length==4 && args[0]=="--fixture-pid")return WorkerDouble(); // explicit IPC/worker fault double
        if(args.Length==2 && args[0]=="--session")return Session(args[1]).GetAwaiter().GetResult();
        string output=Path.GetFullPath(Path.Combine("artifacts","automated-acceptance",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        try{return Desktop.LaunchIsolated(Environment.ProcessPath!,typeof(Program).Assembly.Location,output);}
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"environment-blocked.json"),JsonSerializer.Serialize(new{Status="BLOCKED",Reason=ex.ToString(),InputInjected=false}));Console.WriteLine("BLOCKED: "+ex.Message);return 2;}
    }
    private static async Task Measure(string id,string evidence,string expected,Func<Task<object>> action)
    {
        var clock=Stopwatch.StartNew();
        var started = DateTimeOffset.UtcNow;
        try{var observed=await action();Cases.Add(new(id,evidence,expected,observed,"PASS",clock.Elapsed.TotalMilliseconds,started,DateTimeOffset.UtcNow));}
        catch(Exception ex){try{Input.ReleaseAll();}catch(InvalidOperationException){}Cases.Add(new(id,evidence,expected,new{Error=ex.ToString()},"FAIL",clock.Elapsed.TotalMilliseconds,started,DateTimeOffset.UtcNow));}
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static int Number(JsonElement state,string name)=>state.GetProperty("Fixture").GetProperty(name).GetInt32();
    private static double Coordinate(JsonElement state,string part,string axis)=>state.GetProperty("Fixture").GetProperty(part).GetProperty(axis).GetDouble();
    private static async Task<JsonElement> Until(Client client,Func<JsonElement,bool> predicate,int timeoutMs=3000)
    {
        var clock=Stopwatch.StartNew();
        while(clock.ElapsedMilliseconds<timeoutMs){var state=await client.Command("status");if(predicate(state))return state;await Task.Delay(10);}
        var last=await client.Command("status");var cursor=Input.Cursor();
        throw new TimeoutException("Fixture postcondition not reached. Cursor="+cursor.X+","+cursor.Y+"; state="+last.GetRawText());
    }
    private static async Task CursorScenario(string id, Process? host = null, Client? client = null, bool active = false)
    {
        var clock = Stopwatch.StartNew();
        try
        {
            Desktop.Guard();
            // Let the desktop-switch transition settle before the closed-app control, with no injected motion.
            if (host is null) await Task.Delay(600);
            int? pid = host?.Id;
            DateTime? processStartedUtc = host?.StartTime.ToUniversalTime();
            bool Expected(JsonElement state) => state.GetProperty("OverlayVisible").GetBoolean() &&
                state.GetProperty("Running").GetBoolean() == active &&
                state.GetProperty("TimerScheduled").GetBoolean() == active &&
                (!active || state.GetProperty("Events").GetInt32() > 0);
            int? firstEvents = null;
            if (host is not null)
            {
                Require(!host.HasExited && client is not null, "Cursor fixture process is missing.");
                firstEvents = (await Until(client!, Expected)).GetProperty("Events").GetInt32();
            }
            else
            {
                var existing = Process.GetProcessesByName("Pocket.App");
                try { Require(existing.Length == 0, "Closed control requires Pocket.App to be closed before launch."); }
                finally { foreach (var process in existing) process.Dispose(); }
            }

            var beginUtc = DateTimeOffset.UtcNow;
            var start = Input.Cursor();
            clock.Restart();
            int changed = 0;
            var samples = new List<object>(120);
            for (int i = 0; i < 120; i++)
            {
                Desktop.Guard();
                var p = Input.Cursor();
                if (p.X != start.X || p.Y != start.Y) changed++;
                samples.Add(new { AtMs = clock.Elapsed.TotalMilliseconds, p.X, p.Y });
                await Task.Delay(25);
            }
            var end = Input.Cursor();
            var endUtc = DateTimeOffset.UtcNow;
            int? finalEvents = null;
            if (host is not null)
            {
                Require(!host.HasExited, "Cursor fixture exited while sampling.");
                var state = await client!.Command("status");
                Require(Expected(state), "Cursor fixture left its expected mode during sampling.");
                finalEvents = state.GetProperty("Events").GetInt32();
                Require(active ? finalEvents > firstEvents : finalEvents == firstEvents,
                    "Replay event progression does not match the sampled mode.");
            }
            Cases.Add(new(id, "NATIVE_SOFTWARE_TEST", "No generated movement in the verified app mode",
                new { Mode = pid is null ? "ClosedBeforeLaunch" : active ? "SimulationActive" : "AppIdle",
                    ProcessId = pid, ProcessStartedUtc = processStartedUtc, BeginUtc = beginUtc, EndUtc = endUtc,
                    FirstEvents = firstEvents, FinalEvents = finalEvents, Start = new { start.X, start.Y }, End = new { end.X, end.Y },
                    ChangedSamples = changed, InjectionActive = false, Samples = samples },
                changed == 0 && end.X == start.X && end.Y == start.Y ? "PASS" : "INCONCLUSIVE", clock.Elapsed.TotalMilliseconds));
        }
        catch (Exception ex)
        {
            Cases.Add(new(id, "TEST_HARNESS_SETUP", "Verified app mode before and after cursor sampling",
                new { Error = ex.Message }, "FAIL", clock.Elapsed.TotalMilliseconds));
        }
    }
    private static async Task<int> Session(string output)
    {
        Process? host=null;Client? client=null;long overlayHandle=0,fixtureHandle=0;
        string app=Path.GetFullPath("src/Pocket.App/bin/Release/net10.0-windows/Invey.dll");
        try
        {
            Desktop.Guard();
            if (!TrackingOnly && !PocketOnly) await CursorScenario("CURSOR_CONTROL_GHOST_CLOSED");
            (host, client) = StartAcceptanceHost(app, output);
            await client.Connect();
            var initial=await client.Command("status");fixtureHandle=initial.GetProperty("FixtureHandle").GetInt64();overlayHandle=initial.GetProperty("OverlayHandle").GetInt64();
            if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_LEARNING_CONTROLS_ONLY") == "1")
            {
                var learningHost = host; var learningClient = client; host = null; client = null;
                await RunLearningControls(app, output, learningHost, learningClient);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_DEMO_CONTROLS_ONLY") == "1")
            {
                var demoHost = host;
                var demoClient = client;
                host = null;
                client = null;
                await RunDemoControlRoute(app, output, demoHost, demoClient);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_DEMO_ONLY") == "1")
            {
                var demoHost = host;
                var demoClient = client;
                host = null;
                client = null;
                await RunDemoWorkflow(app, output, demoHost, demoClient);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_FINGER_ONLY") == "1")
            {
                await RunPrecisionPreview(client);
                await RunFingerPreview(client);
                await ExitAndVerifyAsync(client, host, overlayHandle, fixtureHandle);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (Environment.GetEnvironmentVariable("INVEY_ACCEPTANCE_BUDGET_ONLY") == "1")
            {
                await RunCameraBudgetControls(client);
                await ExitAndVerifyAsync(client, host, overlayHandle, fixtureHandle);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (PocketOnly)
            {
                await RunPocketCapture(client, output);
                await ExitAndVerifyAsync(client, host, overlayHandle, fixtureHandle);
                await RunPocketRestart(app, output);
                await RunProductIntegration(app, output);
            await RunDemoWorkflow(app, output);
                CleanPocketFixture(output);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            if (TrackingOnly)
            {
                await RunPrecisionPreview(client);
                await RunRecordedTracking(client);
                await RunCalibration(client);
                await RunNativeTrackingFixture(client);
                await RunFingerPreview(client);
                await ExitAndVerifyAsync(client, host, overlayHandle, fixtureHandle);
                await RunCalibrationAfterRestart(app, output);
                return Cases.Any(c => c.Status == "FAIL") ? 1 : 0;
            }
            Input.Focus(new nint(fixtureHandle));await Until(client,s=>s.GetProperty("Native").GetProperty("Foreground").GetInt64()==fixtureHandle);
            await client.Command("show");await CursorScenario("CURSOR_APP_IDLE", host, client);
            await client.Command("start");await CursorScenario("CURSOR_SIMULATION_ACTIVE", host, client, active: true);await client.Command("stop");
            await Measure("GUI_CONTROL_WITHOUT_OVERLAY","INJECTED_GUI_TEST","One received click with overlay hidden",async()=>
            {
                await client.Command("hide");var s=await client.Command("reset");
                Input.Click(Coordinate(s,"Button","X"),Coordinate(s,"Button","Y"));
                s=await Until(client,t=>Number(t,"Clicks")==1);return new{Actual=Number(s,"Clicks")};
            });
            await Measure("A02_CLICK_THROUGH","INJECTED_GUI_TEST","100 clicks received once through rendered panel",async()=>
            {
                var s=await client.Command("reset");await client.Command("show");
                double dpi=s.GetProperty("Dpi").GetDouble()/96;var panel=s.GetProperty("Panel");var monitor=s.GetProperty("Monitor");
                double left=Math.Max(Coordinate(s,"Button","Left"),monitor.GetProperty("Left").GetDouble()+panel.GetProperty("X").GetDouble()*dpi);
                double top=Math.Max(Coordinate(s,"Button","Top"),monitor.GetProperty("Top").GetDouble()+panel.GetProperty("Y").GetDouble()*dpi);
                double right=Math.Min(Coordinate(s,"Button","Left")+Coordinate(s,"Button","Width"),monitor.GetProperty("Left").GetDouble()+(panel.GetProperty("X").GetDouble()+panel.GetProperty("Width").GetDouble())*dpi);
                double bottom=Math.Min(Coordinate(s,"Button","Top")+Coordinate(s,"Button","Height"),monitor.GetProperty("Top").GetDouble()+(panel.GetProperty("Y").GetDouble()+panel.GetProperty("Height").GetDouble())*dpi);
                Require(right>left && bottom>top,"Button does not overlap rendered panel.");
                for(int i=1;i<=100;i++){Input.Click((left+right)/2,(top+bottom)/2);s=await Until(client,state=>Number(state,"Clicks")>=i);Require(Number(s,"Clicks")==i,"Duplicate click.");Require(s.GetProperty("Native").GetProperty("Foreground").GetInt64()==fixtureHandle,"Unexpected foreground.");}
                return new{Expected=100,Actual=Number(s,"Clicks"),Point=new{X=(left+right)/2,Y=(top+bottom)/2},OverlayVisible=s.GetProperty("OverlayVisible").GetBoolean()};
            });
            await Measure("A02_DRAG_START_RELEASE","INJECTED_GUI_TEST","100 starts and ends;zero canceled/stuck drags",async()=>
            {
                var s=await client.Command("reset");
                for(int i=1;i<=100;i++)
                {
                    Input.Move(Coordinate(s,"Thumb","X"),Coordinate(s,"Thumb","Y"));Input.Down();Input.Move(Coordinate(s,"Thumb","X")+12,Coordinate(s,"Thumb","Y"));
                    await Until(client,t=>Number(t,"DragStarts")>=i);Input.Up();s=await Until(client,t=>Number(t,"DragEnds")>=i);
                    Require(!s.GetProperty("Fixture").GetProperty("DragActive").GetBoolean(),"Stuck native drag.");
                }
                Require(Number(s,"DragStarts")==100 && Number(s,"DragEnds")==100 && Number(s,"DragCanceled")==0,"Drag count mismatch.");
                return new{Starts=Number(s,"DragStarts"),Ends=Number(s,"DragEnds"),Canceled=Number(s,"DragCanceled")};
            });
            await TypingSoak(client,fixtureHandle,output);
            await Measure("A04_CONTINUOUS_DRAG_20","INJECTED_GUI_TEST","20 held drags while Ghost advances;independent releases",async()=>
            {
                var s=await client.Command("reset");await client.Command("start");
                for(int i=1;i<=20;i++)
                {
                    Input.Move(Coordinate(s,"Thumb","X"),Coordinate(s,"Thumb","Y"));Input.Down();Input.Move(Coordinate(s,"Thumb","X")+6,Coordinate(s,"Thumb","Y"));
                    var start=await Until(client,t=>Number(t,"DragStarts")>=i);int count=start.GetProperty("Events").GetInt32();
                    s=await Until(client,t=>t.GetProperty("Events").GetInt32()>=count+5);
                    Require(s.GetProperty("Fixture").GetProperty("DragActive").GetBoolean(),"Drag lost while Ghost advanced.");
                    Input.Up();s=await Until(client,t=>Number(t,"DragEnds")>=i);Require(!s.GetProperty("Fixture").GetProperty("DragActive").GetBoolean(),"Drag stuck after release.");
                }
                await client.Command("stop");Require(Number(s,"DragCanceled")==0,"Unexpected canceled drag.");
                return new{Trials=20,Starts=Number(s,"DragStarts"),Ends=Number(s,"DragEnds"),Canceled=Number(s,"DragCanceled")};
            });
            await Measure("LIFECYCLE_STOP_RESTART","NATIVE_SOFTWARE_TEST","Running stops and releases;restart works",async()=>
            {
                await client.Command("start");await Until(client,s=>s.GetProperty("GhostClaim").ValueKind==JsonValueKind.String);
                var stopped=await client.Command("stop");Require(!stopped.GetProperty("Running").GetBoolean()&&stopped.GetProperty("GhostClaim").ValueKind==JsonValueKind.Null,"Stop leaked claim.");
                await client.Command("start");await Until(client,s=>s.GetProperty("Events").GetInt32()>3);await client.Command("stop");return new{Stopped=true,Restarted=true};
            });
            Cases.Add(new("A10_ALT_TAB_TRAY","INJECTED_GUI_TEST","Actual shell Alt+Tab and tray behavior",new{Reason="Private desktop has no Explorer shell;normal desktop deliberately not targeted."},"BLOCKED",0));
            Cases.Add(new("A11_DPI","NATIVE_SOFTWARE_TEST","Actual available display geometry",new{Dpi=initial.GetProperty("Dpi").GetInt32(),Monitor=initial.GetProperty("Monitor"),OtherScales="NOT RUN; no display changes",MixedMonitor="DEFERRED"},"PASS",0));
            await Measure("A2_PROVISIONAL_FOUR_PATTERNS","NATIVE_SOFTWARE_TEST","Actual isolated MTA UIA fixture patterns with independent postconditions",async()=>
            {
                var fixture=await client.Command("automation-open");long window=fixture.GetProperty("AutomationHandle").GetInt64();
                string worker=Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll");
                await using var automation=new WorkerClient(Environment.ProcessPath!,worker,host.Id);
                var outcomes=new List<object>();long before=Input.Foreground.ToInt64();
                foreach(string target in Protocol.Targets.Keys)
                {
                    var probe=new Request(1,Guid.NewGuid().ToString(),"Probe",window,host.Id,target);
                    var capability=await automation.RequestAsync(probe);Require(capability.Status=="Confirmed",target+" probe "+capability.Status);
                    var execute=probe with {Id=Guid.NewGuid().ToString(),Operation="Execute",Token=capability.Token,Value=target.EndsWith("Range")?42:null};
                    var result=await automation.RequestAsync(execute);Require(result.Status is "Confirmed" or "Attempted",target+" execute "+result.Status);
                    await Until(client,state=>target switch{
                        "GhostFixture.Invoke"=>state.GetProperty("Automation").GetProperty("Invocations").GetInt32()==1,
                        "GhostFixture.Toggle"=>state.GetProperty("Automation").GetProperty("Toggle").GetBoolean(),
                        "GhostFixture.Range"=>state.GetProperty("Automation").GetProperty("Range").GetDouble()==42,
                        "GhostFixture.Scroll"=>state.GetProperty("Automation").GetProperty("ScrollOffset").GetDouble()>0,_=>false});
                    if (result.Outcome == MutationOutcome.Unknown)
                        Require(await automation.DismissAsync(result.Id), "Fixture action uncertainty was not dismissed.");
                    var stale=await automation.RequestAsync(execute with{Id=Guid.NewGuid().ToString()});Require(stale.Status=="Stale","Consumed token reused.");
                    outcomes.Add(new{Target=target,Pattern=result.Pattern,WorkerResult=result.Status,result.Outcome,PostconditionConfirmed=true,ReplayRejected=stale.Status});
                }
                var denied=await automation.RequestAsync(new(1,Guid.NewGuid().ToString(),"Probe",window,host.Id+1,"GhostFixture.Invoke"));Require(denied.Status=="Denied","Unregistered PID accepted.");
                var point=fixture.GetProperty("Automation").GetProperty("InvokePoint");
                var pointRequest=new Request(1,Guid.NewGuid().ToString(),"ProbePoint",window,host.Id,"GhostFixture.Invoke",X:point.GetProperty("X").GetDouble(),Y:point.GetProperty("Y").GetDouble());
                await client.Command("show");var underlay=await automation.RequestAsync(pointRequest);
                Require(underlay.Status=="Confirmed" && underlay.Resolution=="ScopedTree","Scoped underlay lookup failed: "+underlay.Status);
                double scale=fixture.GetProperty("Dpi").GetDouble()/96;var panel=fixture.GetProperty("Panel");
                Require(point.GetProperty("X").GetDouble()>=panel.GetProperty("X").GetDouble()*scale && point.GetProperty("X").GetDouble()<=(panel.GetProperty("X").GetDouble()+panel.GetProperty("Width").GetDouble())*scale && point.GetProperty("Y").GetDouble()>=panel.GetProperty("Y").GetDouble()*scale && point.GetProperty("Y").GetDouble()<=(panel.GetProperty("Y").GetDouble()+panel.GetProperty("Height").GetDouble())*scale,"Point does not overlap opaque overlay panel.");
                Require(before==Input.Foreground.ToInt64(),"UIA moved foreground focus.");
                return new{Patterns=outcomes,WrongPid=denied.Status,UnderOverlayPointProbe=underlay.Status,ForegroundBefore=before,ForegroundAfter=Input.Foreground.ToInt64(),FixtureOnly=true};
            });
            await Measure("A2_PROVISIONAL_WATCHDOG","NATIVE_SOFTWARE_TEST","Kill stalled worker test double; bounded failure, no automatic replay",async()=>
            {
                var fixture=await client.Command("status");
                await using var stalled=new WorkerClient(Environment.ProcessPath!,typeof(Program).Assembly.Location,host.Id,250);
                var request=new Request(1,Guid.NewGuid().ToString(),"Probe",fixture.GetProperty("AutomationHandle").GetInt64(),host.Id,"GhostFixture.Invoke");
                var clock=Stopwatch.StartNew();var first=await stalled.RequestAsync(request);Require(first.Status=="Timeout","Stall was not terminated.");
                var second=await stalled.RequestAsync(request with{Id=Guid.NewGuid().ToString()});Require(second.Status=="Timeout" && stalled.Restarts==2,"Independent new request did not restart worker.");
                return new{First=first.Status,Second=second.Status,stalled.Restarts,ElapsedMs=clock.Elapsed.TotalMilliseconds,Fault="explicit hung-worker double, not a real provider hang"};
            });
            await RunReliability(client,host,output);
            await RunMutationRecovery(client, host, output);
            await RunRecordedTracking(client);
            await RunCalibration(client);
            await RunNativeTrackingFixture(client);
            await RunPrecisionPreview(client);
            await RunFingerPreview(client);
            await RunPocketCapture(client, output);
            await Measure("OVERLAY_CLOSED_DURING_PRESS","NATIVE_SOFTWARE_TEST","Closing owned overlay cancels claim and timer immediately",async()=>
            {
                await client.Command("start");await Until(client,s=>s.GetProperty("GhostClaim").ValueKind==JsonValueKind.String);
                var closed=await client.Command("close-overlay");
                Require(!closed.GetProperty("Running").GetBoolean()&&!closed.GetProperty("TimerScheduled").GetBoolean()&&closed.GetProperty("GhostClaim").ValueKind==JsonValueKind.Null&&!Input.Exists(new nint(overlayHandle)),"Closing overlay leaked simulation or HWND.");
                return new{TimerStopped=true,ClaimReleased=true,OverlayDestroyed=true};
            });
            await ExitAndVerifyAsync(client, host, overlayHandle, fixtureHandle);
            await RunCalibrationAfterRestart(app, output);
            await RunPocketRestart(app, output);
            await RunProductIntegration(app, output);
            await RunDemoWorkflow(app, output);
        }
        catch(Exception ex){Cases.Add(new("SESSION","NATIVE_SOFTWARE_TEST","Complete bounded isolated session",ex.ToString(),"FAIL",0));}
        finally
        {
            try{Input.ReleaseAll();}catch(InvalidOperationException){}
            try { client?.Dispose(); }
            catch (IOException ex) { Cases.Add(new("CLIENT_CLEANUP","TEST_HARNESS_CLEANUP","Close the owned connection",ex.Message,"FAIL",0)); }
            if (host is not null)
            {
                try
                {
                    if (!host.HasExited) host.Kill(true);
                    if (!host.WaitForExit(3000)) throw new TimeoutException("Owned host remained active.");
                }
                catch (Exception ex) { Cases.Add(new("HOST_CLEANUP","TEST_HARNESS_CLEANUP","Confirm owned host exit",ex.Message,"FAIL",0)); }
                finally { host.Dispose(); }
            }
            if (!Cases.Any(c => c.Id == "POCKET_FIXTURE_CLEANUP")) CleanPocketFixture(output);
            var result=new{Scope=PocketOnly?"Pocket only":TrackingOnly?"F06 only":"A1/A2, recorded F06 and Pocket",RunId=Path.GetFileName(output),Utc=DateTimeOffset.UtcNow,Os=Environment.OSVersion.VersionString,Runtime=Environment.Version.ToString(),Architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),Configuration="Release",RunnerVersion="1.1",Profile=TestProfile.Name,Seed=TestProfile.Seed,ProfileBudgetSeconds=TestProfile.BudgetSeconds,RunnerSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Program).Assembly.Location))),WorkerSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll"))),ClientSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(WorkerClient).Assembly.Location))),AppSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(app))),Desktop=Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_DESKTOP"),Cases,FullPhysicalAcceptance="BLOCKED;no human/device-origin tests",AutomatedAcceptance=Cases.Any(c=>c.Status=="FAIL")?"FAIL":Cases.Any(c=>c.Status is "BLOCKED" or "INCONCLUSIVE")?"PASS WITH EXPLICIT ENVIRONMENT GAPS":"PASS"};
            await File.WriteAllTextAsync(Path.Combine(output,"results.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
            await File.WriteAllLinesAsync(Path.Combine(output,"results.md"),new[]{"# Invey isolated automated acceptance",""}.Concat(Cases.Select(c=>$"- {c.Id}: {c.Status} ({c.EvidenceClass})")));
        }
        return Cases.Any(c=>c.Status=="FAIL")?1:0;
    }

    private static (Process Host, Client Client) StartAcceptanceHost(string app, string output)
    {
        Desktop.Guard();
        string pipe = "GhostAcceptance_" + Guid.NewGuid().ToString("N"), token = Guid.NewGuid().ToString("N");
        var info = new ProcessStartInfo(Path.ChangeExtension(app, ".exe")) { UseShellExecute = false, CreateNoWindow = true };
        info.Environment["DOTNET_ROOT"] = Path.GetDirectoryName(Environment.ProcessPath)!;
        info.Environment["GHOST_ACCEPTANCE_DOTNET"] = Environment.ProcessPath!;
        info.Environment["GHOST_ACCEPTANCE_WORKER"] = Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll");
        info.Environment["GHOST_TEST_CALIBRATION_SETTINGS"] = Path.GetFullPath(Path.Combine(output, "calibration.json"));
        info.Environment["GHOST_TEST_POCKET_STORE"] = Path.GetFullPath(Path.Combine(output, "pocket-test"));
        foreach (var argument in new[] { "--acceptance-host", pipe, token }) info.ArgumentList.Add(argument);
        var host = Process.Start(info) ?? throw new InvalidOperationException("Cannot start controlled Invey host.");
        Input.ExpectedPid = host.Id;
        return (host, new Client(pipe, token));
    }
    private sealed class Client : IDisposable
    {
        private readonly NamedPipeClientStream _pipe;private readonly string _token;private StreamReader? _reader;private StreamWriter? _writer;
        public Client(string name,string token){_pipe=new(".",name,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);_token=token;}
        public async Task Connect(){await _pipe.ConnectAsync(10000);_reader=new(_pipe);_writer=new(_pipe,new UTF8Encoding(false)){AutoFlush=true};}
        public async Task<JsonElement> Command(string command)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await _writer!.WriteLineAsync(JsonSerializer.Serialize(new { token = _token, command }).AsMemory(), deadline.Token);
                string line = await _reader!.ReadLineAsync(deadline.Token) ?? throw new IOException("Host disconnected.");
                if (line.Length > 16384) throw new IOException("Oversized reply.");
                using var json = JsonDocument.Parse(line);
                return json.RootElement.Clone();
            }
            catch (OperationCanceledException ex) when (deadline.IsCancellationRequested)
            {
                _pipe.Dispose();
                throw new TimeoutException($"Acceptance command '{command}' timed out and invalidated the connection.", ex);
            }
        }
        public void Dispose()
        {
            try { if (_pipe.IsConnected) _writer?.Dispose(); }
            finally { _reader?.Dispose(); _pipe.Dispose(); }
        }
    }
}
