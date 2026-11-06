using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Pocket.Automation.Client;
using Pocket.Automation.Contracts;

namespace Pocket.Acceptance.Runner;

internal static class TestProfile
{
    public static string Name {get;}=Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_PROFILE")??"standard";
    public static int Seed {get;}=int.TryParse(Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_SEED"),out int seed)?seed:73021;
    public static int Cycles=>Name switch{"short"=>20,"standard"=>60,"extended"=>100,_=>throw new InvalidOperationException("Unknown test profile.")};
    public static int BudgetSeconds=>Name switch{"short"=>150,"standard"=>240,"extended"=>360,_=>throw new InvalidOperationException("Unknown test profile.")};
}
internal static partial class Program
{
    private static int WorkerDouble()
    {
        string? line=Console.ReadLine();if(line is null)return 0;
        var r=System.Text.Json.JsonSerializer.Deserialize<Request>(line)!;
        switch(Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT"))
        {
            case "correlation":Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new Reply("unknown","Confirmed")));return 0;
            case "oversized":Console.WriteLine(new string('x',Protocol.MaxLine+1));return 0;
            case "partial":Console.Write("{");return 0;
            case "malformed":Console.WriteLine("{");return 0;
            case "exit":return 9;
            case "duplicate":var reply=System.Text.Json.JsonSerializer.Serialize(new Reply(r.Id,"Confirmed"));Console.WriteLine(reply);Console.WriteLine(reply);Console.ReadLine();new ManualResetEvent(false).WaitOne();return 3;
            case "execute-stall": case "execute-late": case "execute-malformed":
                if (r.Operation != "Probe") return 9;
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new Reply(r.Id, "Confirmed",
                    Guid.NewGuid().ToString(), Binding: new("fixture", "double", 1, 1, 1, 1, null))));
                string? actionLine = Console.ReadLine();
                if (actionLine is null) return 0;
                string? fault = Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT");
                if (fault == "execute-malformed")
                {
                    Console.WriteLine("{");
                    return 9;
                }
                if (fault == "execute-late")
                {
                    Thread.Sleep(900);
                    var action = System.Text.Json.JsonSerializer.Deserialize<Request>(actionLine)!;
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new Reply(action.Id, "Confirmed").WithOutcome(MutationOutcome.Applied)));
                    File.WriteAllText(Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_LATE_ACK")!, "Late reply written");
                }
                new ManualResetEvent(false).WaitOne();
                return 3;
            default:new ManualResetEvent(false).WaitOne();return 3;
        }
    }
    private static async Task TypingSoak(Client client,long fixtureHandle,string output)
    {
        await Measure("A03_TYPING_SOAK","INJECTED_GUI_TEST","Exact seeded text with per-chunk digest, focus and lifecycle acknowledgement",async()=>
        {
            using var audit=new StreamWriter(Path.Combine(output,"typing-chunks.jsonl")){AutoFlush=true};
            var random=new Random(TestProfile.Seed);var batches=new List<object>();var latencies=new List<double>();int total=0;
            for(int batch=0;batch<TestProfile.Cycles/20;batch++)
            {
                var s=await client.Command("reset");Input.Click(Coordinate(s,"Editor","X"),Coordinate(s,"Editor","Y"));
                await client.Command("start");var expected=new StringBuilder();
                for(int cycle=0;cycle<20;cycle++)
                {
                    s = await Until(client,t=>t.GetProperty("Events").GetInt32()>=cycle*45+2,5000);
                    Require(s.GetProperty("Native").GetProperty("Foreground").GetInt64()==fixtureHandle &&
                        s.GetProperty("Fixture").GetProperty("EditorFocused").GetBoolean(), "Typing fixture lost focus before injection.");
                    string text="GHOST "+random.Next(100000,999999)+" "+new string(Enumerable.Range(0,random.Next(12,33)).Select(_=>(char)random.Next(65,91)).ToArray())+".";
                    expected.Append(text);string hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expected.ToString())));
                    var watch=Stopwatch.StartNew();Input.Text(text);
                    try
                    {
                        s=await Until(client,t=>Number(t,"TextLength")==expected.Length && t.GetProperty("Fixture").GetProperty("TextDigest").GetString()==hash);
                    }
                    catch (TimeoutException)
                    {
                        await audit.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(new { Seed = TestProfile.Seed, Batch = batch,
                            Cycle = cycle, ExpectedLength = expected.Length, ExpectedDigest = hash, Status = "No exact acknowledgement" }));
                        throw;
                    }
                    latencies.Add(watch.Elapsed.TotalMilliseconds);
                    await audit.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(new{Seed=TestProfile.Seed,Batch=batch,Cycle=cycle,ExpectedLength=expected.Length,ExpectedDigest=hash,AcknowledgedLength=Number(s,"TextLength"),AcknowledgedDigest=s.GetProperty("Fixture").GetProperty("TextDigest").GetString(),Events=s.GetProperty("Events").GetInt32(),LatencyMs=watch.Elapsed.TotalMilliseconds}));
                    Require(s.GetProperty("Native").GetProperty("Foreground").GetInt64()==fixtureHandle && s.GetProperty("Fixture").GetProperty("EditorFocused").GetBoolean(),"Typing focus moved.");
                }
                s=await Until(client,t=>!t.GetProperty("Running").GetBoolean(),5000);
                await client.Command("stop");Require(!s.GetProperty("TimerScheduled").GetBoolean(),"Typing batch leaked timer.");
                total+=expected.Length;batches.Add(new{Batch=batch,Cycles=20,Characters=expected.Length,Digest=s.GetProperty("Fixture").GetProperty("TextDigest").GetString(),ExactMatch=true});
            }
            return new{Profile=TestProfile.Name,Seed=TestProfile.Seed,Cycles=TestProfile.Cycles,Characters=total,SuccessfulMatches=latencies.Count,Mismatches=0,MissingAcknowledgements=0,Timeouts=0,ExtraCharacters=0,DuplicateAcknowledgements="Not an event stream; exact length and digest checked for each serial snapshot",RecoveredFailures=0,AcknowledgementMaxMs=latencies.Max(),AcknowledgementMeanMs=latencies.Average(),Batches=batches};
        });
    }
    private static async Task RunReliability(Client client,Process host,string output)
    {
        string worker=Path.GetFullPath("src/Pocket.Automation.Worker/bin/Release/net10.0-windows/Pocket.Automation.Worker.dll");
        await Measure("A2_EDGE_CONTRACTS","NATIVE_SOFTWARE_TEST","Adverse pattern, identity, expiry, duplicate and coordinate checks",async()=>
        {
            var state=await client.Command("automation-normal");long window=state.GetProperty("AutomationHandle").GetInt64();
            await using var automation=new WorkerClient(Environment.ProcessPath!,worker,host.Id);
            Request Probe(string target="GhostFixture.Invoke")=>new(1,Guid.NewGuid().ToString(),"Probe",window,host.Id,target);
            async Task<Reply> Execute(string target,double? value=null)
            {
                var r=Probe(target);var cap=await automation.RequestAsync(r);Require(cap.Status=="Confirmed","Probe: "+cap.Status);
                return await automation.RequestAsync(r with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token,Value=value});
            }
            var results=new List<object>();
            foreach(string mode in new[]{"duplicate","disabled","missing","no-pattern"})
            {
                await client.Command("automation-"+mode);var reply=await automation.RequestAsync(Probe());
                Require(reply.Status==(mode=="duplicate"?"AmbiguousTarget":"Unsupported"),mode+" unexpectedly accepted: "+reply.Status);
                if(mode=="disabled")foreach(string target in new[]{"GhostFixture.Toggle","GhostFixture.Range"})Require((await automation.RequestAsync(Probe(target))).Status=="Unsupported","Disabled pattern accepted.");
                results.Add(new{Mode=mode,reply.Status});
            }
            await client.Command("automation-normal");
            foreach(double value in new[]{0d,100d,42.125d,-1d,101d})
            {
                var reply=await Execute("GhostFixture.Range",value);Require(reply.Status==(value<0||value>100?"Denied":"Confirmed"),"Range boundary: "+reply.Status);
                if(value is >=0 and <=100)await Until(client,s=>s.GetProperty("Automation").GetProperty("Range").GetDouble()==value);
                results.Add(new{Range=value,reply.Status});
            }
            await client.Command("automation-read-only");var readOnly=await Execute("GhostFixture.Range",42);
            Require(readOnly.Status=="Denied","Read-only range accepted.");await client.Command("automation-normal");
            await client.Command("automation-ignore-range");var mismatch=await Execute("GhostFixture.Range",42);
            Require(mismatch.Outcome==MutationOutcome.Unknown&&mismatch.Value==13,"Ignored write falsely confirmed.");
            var unchanged = await automation.ReconcileAsync(mismatch.Id);
            Require(unchanged.Observation == Observation.PriorStateObserved && unchanged.Outcome == MutationOutcome.Unknown, "Old value resolved an uncertain write.");
            Require(await automation.DismissAsync(mismatch.Id), "Could not dismiss ignored-write uncertainty.");
            await client.Command("automation-reject-range");var rejection=await Execute("GhostFixture.Range",42);
            Require(rejection.Status=="Unsupported","Provider rejection falsely confirmed.");
            Require(rejection.Outcome == MutationOutcome.Unknown && await automation.DismissAsync(rejection.Id), "Provider exception was treated as proven non-dispatch.");
            await client.Command("automation-normal");
            bool prior=(await client.Command("status")).GetProperty("Automation").GetProperty("Toggle").GetBoolean();
            for(int i=0;i<4;i++){var reply=await Execute("GhostFixture.Toggle");prior=!prior;await Until(client,s=>s.GetProperty("Automation").GetProperty("Toggle").GetBoolean()==prior);Require(reply.Status=="Confirmed","Toggle failed.");}
            await client.Command("automation-no-scroll");var noScroll=await Execute("GhostFixture.Scroll");Require(noScroll.Status=="Unsupported","Non-scrollable control accepted.");
            await client.Command("automation-normal");await client.Command("automation-scroll-end");
            var end=await Execute("GhostFixture.Scroll");Require(end.Status=="Unsupported","Scroll end should be unavailable.");
            await client.Command("automation-normal");
            var expired=Probe();var cap=await automation.RequestAsync(expired);await Task.Delay(350);
            Require((await automation.RequestAsync(expired with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token})).Status=="Stale","Expired token accepted.");
            var replaced=Probe();cap=await automation.RequestAsync(replaced);await client.Command("automation-replace");
            Require((await automation.RequestAsync(replaced with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token})).Status=="Stale","Replacement retained authority.");
            int invocationBefore=(await client.Command("status")).GetProperty("Automation").GetProperty("Invocations").GetInt32();
            var invoke=Probe();cap=await automation.RequestAsync(invoke);
            var once=invoke with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token};
            var invoked = await automation.RequestAsync(once);
            Require(invoked.Status=="Attempted" && invoked.Outcome==MutationOutcome.Unknown,"Benign Invoke dispatch failed.");
            Require((await automation.RequestAsync(once)).Status=="Denied","Duplicate action ID accepted.");
            await Until(client,s=>s.GetProperty("Automation").GetProperty("Invocations").GetInt32()==invocationBefore+1);
            Require((await automation.ReconcileAsync(once.Id)).Observation == Observation.Inconclusive, "Generic Invoke produced an invented postcondition.");
            await client.Command("automation-replace");
            Require((await automation.ReconcileAsync(once.Id)).Observation == Observation.TargetChanged, "Replacement element was observed as the original.");
            Require(await automation.DismissAsync(once.Id), "Could not dismiss Invoke uncertainty.");
            var duplicate=Probe();Require((await automation.RequestAsync(duplicate)).Status=="Confirmed","Initial request denied.");
            Require((await automation.RequestAsync(duplicate)).Status=="Denied","Duplicate ID accepted.");
            var outside=Probe() with{Operation="ProbePoint",X=-99999,Y=-99999};Require((await automation.RequestAsync(outside)).Status=="Unsupported","Outside point accepted.");
            Require((await automation.RequestAsync(Probe() with{Window=state.GetProperty("FixtureHandle").GetInt64()})).Status=="Denied","Wrong HWND accepted.");
            Require((await automation.RequestAsync(Probe() with{Window=long.MaxValue})).Status=="Denied","Invalid HWND accepted.");
            using var canceled=new CancellationTokenSource();canceled.Cancel();
            Require((await automation.RequestAsync(Probe(),canceled.Token)).Status=="Canceled","Pre-cancel ignored.");
            var original=Probe();cap=await automation.RequestAsync(original);await client.Command("automation-recreate");
            Require((await automation.RequestAsync(original with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token})).Status is "Denied" or "Stale","Destroyed window retained authority.");
            return new{Patterns=results,ToggleTransitions=4,NoScroll=noScroll.Status,AtScrollEnd=end.Status,ReadOnlyRange=readOnly.Status,MismatchedReadback=mismatch.Status,ProviderRejectedWrite=rejection.Status,ExpiryRejected=true,ReplacedControlRejected=true,DuplicateRequestRejected=true,OutsidePointRejected=true,WrongHwndRejected=true,InvalidHwndRejected=true,DuplicateActionRejected=true,PreCancel=true,DestroyedWindowRejected=true};
        });
        await Measure("A2_IPC_FAULTS","DETERMINISTIC_TEST","Malformed, partial, oversized, mismatched and exited-worker replies fail closed",async()=>
        {
            var fixture=await client.Command("status");var results=new List<object>();
            string? previous=Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT");
            try
            {
                foreach(string fault in new[]{"correlation","oversized","partial","malformed","exit"})
                {
                    Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT",fault);
                    await using var automation=new WorkerClient(Environment.ProcessPath!,typeof(Program).Assembly.Location,host.Id,1000);
                    var result=await automation.RequestAsync(new(1,Guid.NewGuid().ToString(),"Probe",fixture.GetProperty("AutomationHandle").GetInt64(),host.Id,"GhostFixture.Invoke"));
                    Require(result.Status=="Unavailable",fault+" accepted: "+result.Status);Require(automation.ActiveProcessId is null,"Faulted worker retained.");
                    results.Add(new{Fault=fault,result.Status});
                }
                Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT","duplicate");
                await using(var duplicate=new WorkerClient(Environment.ProcessPath!,typeof(Program).Assembly.Location,host.Id,1000))
                {
                    Request Next()=>new(1,Guid.NewGuid().ToString(),"Probe",fixture.GetProperty("AutomationHandle").GetInt64(),host.Id,"GhostFixture.Invoke");
                    Require((await duplicate.RequestAsync(Next())).Status=="Confirmed","Double first reply missing.");
                    Require((await duplicate.RequestAsync(Next())).Status=="Unavailable","Leftover duplicate reply accepted for new request.");
                }
                results.Add(new{Fault="duplicate-response",Status="Unavailable"});
                Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT","execute-stall");
                await using var stalled=new WorkerClient(Environment.ProcessPath!,typeof(Program).Assembly.Location,host.Id,500);
                var probe = new Request(1, Guid.NewGuid().ToString(), "Probe", fixture.GetProperty("AutomationHandle").GetInt64(), host.Id, "GhostFixture.Invoke");
                var capability = await stalled.RequestAsync(probe);
                var uncertain=await stalled.RequestAsync(probe with { Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token });
                Require(uncertain.Status=="Timeout"&&uncertain.OutcomeUnknown,"Uncertain action reported certainty.");
                Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT", "execute-malformed");
                await using var malformed = new WorkerClient(Environment.ProcessPath!, typeof(Program).Assembly.Location, host.Id);
                capability = await malformed.RequestAsync(probe);
                var invalid = await malformed.RequestAsync(probe with { Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token });
                Require(invalid.Status == "Unavailable" && invalid.Outcome == MutationOutcome.Unknown, "Malformed mutation acknowledgement reported certainty.");
                return new{Faults=results,ExecuteTimeout=uncertain.Status,uncertain.OutcomeUnknown,MalformedExecute=invalid.Outcome,AutomaticRetries=0};
            }
            finally{Environment.SetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER_FAULT",previous);}
        });
        await Measure("A2_PERFORMANCE_SPOTCHECK","NATIVE_SOFTWARE_TEST","Bounded cold/warm fixture latency and worker resource samples; disposal observed",async()=>
        {
            var fixture=await client.Command("automation-normal");var samples=new List<double>();int pid;
            long working,privateBytes;int handles;double cpu;
            await using(var automation=new WorkerClient(Environment.ProcessPath!,worker,host.Id))
            {
                for(int i=0;i<21;i++)
                {
                    var clock=Stopwatch.StartNew();
                    var reply=await automation.RequestAsync(new(1,Guid.NewGuid().ToString(),"Probe",fixture.GetProperty("AutomationHandle").GetInt64(),host.Id,"GhostFixture.Invoke"));
                    Require(reply.Status=="Confirmed","Latency probe failed: "+reply.Status);samples.Add(clock.Elapsed.TotalMilliseconds);
                }
                pid=automation.ActiveProcessId??throw new InvalidOperationException("Missing worker.");
                using var process=Process.GetProcessById(pid);process.Refresh();working=process.WorkingSet64;privateBytes=process.PrivateMemorySize64;handles=process.HandleCount;cpu=process.TotalProcessorTime.TotalMilliseconds;
            }
            Require(!ProcessExists(pid),"Disposed worker survived.");var sorted=samples.Skip(1).Order().ToArray();
            return new{ColdMs=samples[0],WarmSamples=sorted.Length,WarmMeanMs=sorted.Average(),WarmP95Ms=sorted[(int)Math.Ceiling(sorted.Length*.95)-1],WorkingSetBytes=working,PrivateBytes=privateBytes,HandleCount=handles,CumulativeCpuMs=cpu,WorkerGone=true,LongDurationLeakCertification=false};
        });
        await StandardCompatibility(worker,output);
        await Measure("A2_REAL_PROVIDER_RECOVERY","NATIVE_SOFTWARE_TEST","Real blocked provider callback; kill worker; responsive primary host; explicit new-request recovery",async()=>
        {
            string directory=Path.Combine(output,"adverse-provider");Directory.CreateDirectory(directory);
            var info=new ProcessStartInfo(Path.GetFullPath("src/Pocket.App/bin/Release/net10.0-windows/Invey.exe")){UseShellExecute=false,CreateNoWindow=true};
            info.Environment["DOTNET_ROOT"]=Path.GetDirectoryName(Environment.ProcessPath)!;
            info.ArgumentList.Add("--adverse-provider");info.ArgumentList.Add(directory);
            using var provider=Process.Start(info)??throw new IOException("Cannot start adverse provider.");
            int pid=provider.Id;var timings=new List<object>();int? killedWorker=null;
            try
            {
                async Task WaitFile(string name,int ms=5000){var w=Stopwatch.StartNew();while(!File.Exists(Path.Combine(directory,name))&&w.ElapsedMilliseconds<ms)await Task.Delay(10);Require(File.Exists(Path.Combine(directory,name)),"Provider marker missing: "+name);}
                await WaitFile("ready");long window=long.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"ready")));
                Request Probe()=>new(1,Guid.NewGuid().ToString(),"Probe",window,pid,"GhostFixture.Invoke");
                await using var automation=new WorkerClient(Environment.ProcessPath!,worker,pid,1800);
                var warm=await automation.RequestAsync(Probe());Require(warm.Status=="Confirmed","Adverse provider warmup: "+warm.Status);
                await File.WriteAllTextAsync(Path.Combine(directory,"arm"),"slow");
                var watch=Stopwatch.StartNew();var slow=await automation.RequestAsync(Probe());
                Require(slow.Status=="Confirmed","Slow provider: "+slow.Status);timings.Add(new{Mode="slow",slow.Status,ElapsedMs=watch.Elapsed.TotalMilliseconds});
                File.Delete(Path.Combine(directory,"entered"));await File.WriteAllTextAsync(Path.Combine(directory,"arm"),"hang");
                watch.Restart();var pending=automation.RequestAsync(Probe());await WaitFile("entered");
                killedWorker=automation.ActiveProcessId;Require(killedWorker.HasValue,"No worker process to supervise.");
                var heartbeat=Stopwatch.StartNew();await client.Command("status");double responsive=heartbeat.Elapsed.TotalMilliseconds;
                var busy=await automation.RequestAsync(Probe());Require(busy.Status=="Busy","Concurrent queue was not rejected.");
                var timed=await pending;double elapsed=watch.Elapsed.TotalMilliseconds;
                Require(timed.Status=="Timeout" && !timed.OutcomeUnknown && elapsed<3500,"Provider read did not time out safely.");
                Require(!ProcessExists(killedWorker.GetValueOrDefault()),"Timed-out worker still exists.");
                string callback=await File.ReadAllTextAsync(Path.Combine(directory,"entered"));
                // Releasing a query does not dispatch an action. Action timeouts remain OutcomeUnknown.
                File.Delete(Path.Combine(directory,"arm"));await File.WriteAllTextAsync(Path.Combine(directory,"release"),"release");
                await WaitFile("released");var recovered=await automation.RequestAsync(Probe());Require(recovered.Status=="Confirmed"&&automation.Restarts==2,"Fresh worker did not recover.");
                var oldToken=await automation.RequestAsync(new(1,Guid.NewGuid().ToString(),"Execute",window,pid,"GhostFixture.Invoke",Token:warm.Token));
                Require(oldToken.Status=="Stale","Worker restart retained old capability.");
                timings.Add(new{OldTokenAfterRestart=oldToken.Status});
                timings.Add(new{Mode="hang",Callback=callback,timed.Status,ElapsedMs=elapsed,PrimaryHeartbeatMs=responsive,Busy=busy.Status,WorkerGone=true,Recovered=recovered.Status,automation.Restarts});
                File.Delete(Path.Combine(directory,"release"));File.Delete(Path.Combine(directory,"entered"));await File.WriteAllTextAsync(Path.Combine(directory,"arm"),"hang");
                using var cancellation=new CancellationTokenSource();
                pending=automation.RequestAsync(Probe(),cancellation.Token);await WaitFile("entered");int? canceledWorker=automation.ActiveProcessId;
                cancellation.Cancel();var canceled=await pending;Require(canceled.Status=="Canceled"&&canceledWorker.HasValue&&!ProcessExists(canceledWorker.Value),"Blocked provider cancellation leaked worker.");
                timings.Add(new{Mode="cancel",canceled.Status,WorkerGone=true});
                File.Delete(Path.Combine(directory,"arm"));await File.WriteAllTextAsync(Path.Combine(directory,"release"),"release");
                await Task.Delay(40);await File.WriteAllTextAsync(Path.Combine(directory,"arm"),"throw");
                var failed=await automation.RequestAsync(Probe());Require(failed.Status=="Unsupported","Provider exception did not fail closed: "+failed.Status);
                File.Delete(Path.Combine(directory,"arm"));var afterFailure=await automation.RequestAsync(Probe());Require(afterFailure.Status=="Confirmed","Provider exception poisoned recovery.");
                timings.Add(new{Mode="provider-exception",failed.Status,Recovered=afterFailure.Status});
                File.Delete(Path.Combine(directory,"release"));File.Delete(Path.Combine(directory,"entered"));
                await File.WriteAllTextAsync(Path.Combine(directory,"arm"),"hang");pending=automation.RequestAsync(Probe());await WaitFile("entered");
                provider.Kill(true);Require(provider.WaitForExit(3000),"Provider termination failed.");
                var exited=await pending;Require(exited.Status is "Unsupported" or "Denied" or "Unavailable" or "Timeout","Terminated provider accepted: "+exited.Status);
                var afterExit=await automation.RequestAsync(Probe());Require(afterExit.Status=="Denied","Dead PID retained authorization: "+afterExit.Status);
                timings.Add(new{Mode="provider-terminated",exited.Status,NextRequest=afterExit.Status,ProviderGone=true});
                return new{ProviderClass="WPF custom AutomationPeer in separate Pocket.App process",RealProviderCallbackEntered=true,ReadQueryOnly=true,Timings=timings,ProviderKilledInFinally=true,ActionRollbackGuarantee=false};
            }
            finally
            {
                if(!provider.HasExited)provider.Kill(true);
                Require(provider.WaitForExit(3000),"Provider did not exit.");
                File.WriteAllText(Path.Combine(directory,"cleanup.json"),System.Text.Json.JsonSerializer.Serialize(new{ProviderPid=pid,ProviderGone=!ProcessExists(pid),LastTimedOutWorker=killedWorker,WorkerGone=killedWorker is null||!ProcessExists(killedWorker.Value)}));
            }
        });
    }
    private static async Task StandardCompatibility(string worker,string output)
    {
        await Measure("A2_STANDARD_PROVIDER_COMPATIBILITY","NATIVE_SOFTWARE_TEST","Observe only owned WinForms/Win32 controls; supported actions need independent state",async()=>
        {
            string directory=Path.Combine(output,"standard-provider");Directory.CreateDirectory(directory);
            var info=new ProcessStartInfo(Path.GetFullPath("src/Pocket.App/bin/Release/net10.0-windows/Invey.exe")){UseShellExecute=false,CreateNoWindow=true};
            info.Environment["DOTNET_ROOT"]=Path.GetDirectoryName(Environment.ProcessPath)!;
            info.ArgumentList.Add("--standard-provider");info.ArgumentList.Add(directory);
            using var provider=Process.Start(info)??throw new IOException("Standard fixture failed.");
            var outcomes=new List<object>();
            try
            {
                var watch=Stopwatch.StartNew();while(!File.Exists(Path.Combine(directory,"ready"))&&watch.ElapsedMilliseconds<5000)await Task.Delay(10);
                Require(File.Exists(Path.Combine(directory,"ready")),"Standard fixture not ready.");
                long window=long.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"ready")));
                await using var automation=new WorkerClient(Environment.ProcessPath!,worker,provider.Id);
                foreach(string target in Protocol.Targets.Keys)
                {
                    var r=new Request(1,Guid.NewGuid().ToString(),"Probe",window,provider.Id,target);
                    var cap=await automation.RequestAsync(r);
                    Require(cap.Status == "Confirmed", target + " known WinForms capability was unavailable: " + cap.Status);
                    var action=await automation.RequestAsync(r with{Id=Guid.NewGuid().ToString(),Operation="Execute",Token=cap.Token,Value=target.EndsWith("Range")?42:null});
                    Require(action.Status is "Confirmed" or "Attempted",target+" action "+action.Status);
                    bool matched=false;watch.Restart();
                    while(!matched&&watch.ElapsedMilliseconds<2000)
                    {
                        using var json=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"state")));
                        var s=json.RootElement;matched=target switch{
                            "GhostFixture.Invoke"=>s.GetProperty("Invocations").GetInt32()==1,
                            "GhostFixture.Toggle"=>s.GetProperty("Toggle").GetBoolean(),
                            "GhostFixture.Range"=>s.GetProperty("Range").GetDouble()==42,
                            "GhostFixture.Scroll"=>s.GetProperty("ScrollOffset").GetDouble()>0,_=>false};
                        if(!matched)await Task.Delay(10);
                    }
                    Require(matched,target+" standard provider postcondition failed.");
                    if (action.Outcome == MutationOutcome.Unknown)
                        Require(await automation.DismissAsync(action.Id), "Could not dismiss fixture uncertainty.");
                    outcomes.Add(new{Target=target,Probe=cap.Status,Action=action.Status,action.Outcome,Postcondition="Confirmed independently"});
                }
                return new{ProviderClass="Controlled WinForms Button, CheckBox, HScrollBar and scrolling Panel",Outcomes=outcomes,ThirdPartyApplicationsTested=false};
            }
            finally{if(!provider.HasExited)provider.Kill(true);Require(provider.WaitForExit(3000),"Standard fixture cleanup failed.");}
        });
    }
    private static bool ProcessExists(int pid){try{using var process=Process.GetProcessById(pid);return !process.HasExited;}catch(ArgumentException){return false;}}
}
