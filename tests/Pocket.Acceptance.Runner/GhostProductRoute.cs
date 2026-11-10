using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pocket.Acceptance.Runner;
internal static partial class Program
{
    private static JsonElement DesktopGhost(JsonElement s)=>Product(s).GetProperty("DesktopGhost");
    private static string DesktopMode(JsonElement s)=>DesktopGhost(s).GetProperty("Mode").GetString()!;
    private static async Task RunGhostProductRoute(string app,string output,Process? supplied=null,Client? connection=null)
    {
        await Measure("E20_DUAL_POINTER_PRODUCT", "SYNTHETIC_INTENT + ACTUAL_WPF_TARGETS + NATIVE_ISOLATED_WINDOWS",
            "One passive pointer reveals real shelf, opens/moves/pins actual card, supports independent Mouse/Ghost Workspace claims and persists across restart",async()=>
        {
            var(host,client)=supplied is not null && connection is not null?(supplied,connection):StartAcceptanceHost(app,output);
            var timings=new List<double>();long cardHandle=0;double parkedY=0,parkedX=0;
            async Task<JsonElement> Send(string command){var watch=Stopwatch.StartNew();var s=await client.Command(command);timings.Add(watch.Elapsed.TotalMilliseconds);return s;}
            async Task Aim(string id)=>_ = await Send("ghost-aim|"+id);
            using(host)using(client)
            try
            {
                if(connection is null)await client.Connect();
                var initial=await Send("dual-demo-start");Require(CameraOff(initial),"Product startup activated tracking.");
                await Until(client,s=>Product(s).GetProperty("WindowLoaded").GetBoolean());
                var spatial=(await Send("spatial-ui")).GetProperty("PreviewResult");
                Require(spatial.GetProperty("Compact").GetBoolean()&&spatial.GetProperty("Expanded").GetBoolean()&&spatial.GetProperty("CameraOff").GetBoolean(),"Spatial navigation/camera-off check failed.");
                File.WriteAllText(Path.Combine(output,"spatial-ui.json"),spatial.GetRawText());
                await Send("ghost-pattern-hide");
                await Send("ghost-controls");
                await Send("ghost-controls-place");
                long editor=initial.GetProperty("FixtureHandle").GetInt64();
                var fixture=(await Send("status")).GetProperty("Fixture").GetProperty("Editor");
                await FocusTrackingAsync(client,editor);
                Input.Click(fixture.GetProperty("X").GetDouble(),fixture.GetProperty("Y").GetDouble());
                await Until(client,s=>s.GetProperty("Fixture").GetProperty("EditorFocused").GetBoolean()&&
                    s.GetProperty("Native").GetProperty("Foreground").GetInt64()==editor);
                Input.Text("dual-before");var cursor=Input.Cursor();
                await Send("ghost-control|Start preview");await Aim("shelf:reveal");
                await Send("ghost-activate");Require(!ShelfVisible(await Send("status")),"Preview issued a command.");
                var armed=await Send("ghost-control|Arm Pocket control");Require(DesktopMode(armed)=="Armed","Explicit controlled arm failed.");
                await Send("ghost-activate");
                var shelf=await Until(client,s=>ShelfVisible(s)&&ProductPocket(s).GetProperty("Count").GetInt32()>=2);
                var rows=await Until(client,s=>DesktopGhost(s).GetProperty("Targets").EnumerateArray().Any(t=>t.GetProperty("Id").GetString()!.StartsWith("reference:")));
                string reference=DesktopGhost(rows).GetProperty("Targets").EnumerateArray().First(t=>t.GetProperty("Id").GetString()!.StartsWith("reference:")).GetProperty("Id").GetString()!;
                await Aim(reference);await Send("ghost-press");await Send("ghost-release");
                var opened=await Until(client,s=>ProductPocket(s).GetProperty("Cards").GetInt32()>0);
                cardHandle=ProductPocket(opened).GetProperty("ViewerHandle").GetInt64();
                var target=await Until(client,s=>DesktopGhost(s).GetProperty("Targets").EnumerateArray().Any(t=>t.GetProperty("Id").GetString()!.StartsWith("card:")));
                string card=DesktopGhost(target).GetProperty("Targets").EnumerateArray().First(t=>t.GetProperty("Id").GetString()!.StartsWith("card:")).GetProperty("Id").GetString()!;
                await Aim(card);var before=await Send("ghost-press");Require(DesktopMode(before)=="Holding","Card not acquired.");
                double oldX=ProductPocket(before).GetProperty("ViewerBounds").GetProperty("X").GetDouble();
                await Send("ghost-move|48|12");
                var moved=await Until(client,s=>ProductPocket(s).GetProperty("ViewerBounds").GetProperty("X").GetDouble()>oldX+30);
                Require(DesktopGhost(moved).GetProperty("ActiveTarget").GetString()==card,"Movement used a different target.");
                var monitor=initial.GetProperty("Monitor");var bounds=ProductPocket(moved).GetProperty("ViewerBounds");
                parkedX=monitor.GetProperty("Left").GetDouble()+10;
                parkedY=DesktopGhost(moved).GetProperty("WorkArea").GetProperty("Bottom").GetDouble()-bounds.GetProperty("Height").GetDouble()-10;
                int dx=(int)(parkedX-bounds.GetProperty("X").GetDouble()),dy=(int)(parkedY-bounds.GetProperty("Y").GetDouble());
                await Send($"ghost-move|{dx}|{dy}");await Send("ghost-release");await Aim(card);
                await Send("ghost-pin");await Until(client,s=>ProductPocket(s).GetProperty("Pinned").GetBoolean());
                Require(ActualPinned(cardHandle),"Native pin style did not change.");
                await Send("ghost-unpin");await Until(client,s=>!ProductPocket(s).GetProperty("Pinned").GetBoolean());
                Require(!ActualPinned(cardHandle),"Native topmost style survived Unpin.");
                await Send("ghost-gesture-replay");
                var gestureEvidence=DesktopGhost(await Send("status")).GetProperty("GestureReplay");
                Require(gestureEvidence.GetProperty("ActualCardMoved").GetBoolean()&&gestureEvidence.GetProperty("AmbiguityCanceled").GetBoolean(),"Mapped gesture fixture did not prove card movement and cancellation.");
                var replayBounds=ProductPocket(await Send("status")).GetProperty("ViewerBounds");
                parkedX=replayBounds.GetProperty("X").GetDouble();parkedY=replayBounds.GetProperty("Y").GetDouble();
                await Send("ghost-start");await Aim(card);await Send("ghost-arm");
                await Send("ghost-pin");Require(ActualPinned(cardHandle),"Re-pin failed.");
                await Aim("shelf:dismiss");await Send("ghost-activate");await Until(client,s=>!ShelfVisible(s));
                Require(IsWindowVisible(new(cardHandle)),"Dismiss hid pinned card.");
                var editorState=await Send("status");var afterCursor=Input.Cursor();
                Require(cursor.X==afterCursor.X&&cursor.Y==afterCursor.Y&&Input.Foreground.ToInt64()==editor&&
                    editorState.GetProperty("Fixture").GetProperty("EditorFocused").GetBoolean(),"Ghost altered native pointer or editor focus.");
                Input.Text("dual-after");
                string digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("dual-beforedual-after")));
                await Until(client,s=>s.GetProperty("Fixture").GetProperty("TextDigest").GetString()==digest);

                // Exercise the user-facing keyboard steering controls with actual input on the isolated desktop.
                await Send("ghost-controls");
                await Send("ghost-control|Start preview");
                var controlState=await Send("status");
                var steering=DesktopGhost(controlState).GetProperty("KeyboardSteeringPoint");
                Require(steering.ValueKind==JsonValueKind.Object,"Keyboard steering control is not visible.");
                double toggleX=steering.GetProperty("X").GetDouble(),toggleY=steering.GetProperty("Y").GetDouble();
                var keyboardStart=DesktopGhost(controlState).GetProperty("Position");
                Input.Click(toggleX,toggleY);var nativeCursor=Input.Cursor();
                Input.Key(0x44,false);Input.Key(0x44,true);
                var keyboardMoved=await Until(client,s=>DesktopGhost(s).GetProperty("Position").GetProperty("X").GetDouble()>keyboardStart.GetProperty("X").GetDouble()+32);
                var afterKeyboard=Input.Cursor();Require(afterKeyboard.X==nativeCursor.X&&afterKeyboard.Y==nativeCursor.Y&&DesktopMode(keyboardMoved)=="Preview",
                    "Keyboard steering moved the Windows cursor or changed its action state.");
                Input.Key(0x1B,false);Input.Key(0x1B,true);
                var keyboardStopped=await Until(client,s=>DesktopMode(s)=="Off");
                Require(DesktopGhost(keyboardStopped).GetProperty("RenderedPoint").ValueKind==JsonValueKind.Null,"Escape did not immediately hide the stopped Ghost pointer.");
                var feedback=await Send("feedback-open");
                Require(Product(feedback).GetProperty("FeedbackVisible").GetBoolean()&&Product(feedback).GetProperty("FeedbackButtonVisible").GetBoolean(),"Built-in feedback form did not open.");
                await Send("feedback-close");
                var image=await Send("preview-protocol-controls");
                Require(image.GetProperty("PreviewResult").GetProperty("RepeatedImagePollsDeduplicated").GetBoolean(),"WPF preview repeated-image regression failed.");
                var practice=await Send("ghost-practice-ui");
                Require(practice.GetProperty("PreviewResult").GetProperty("GuidedLesson").GetBoolean(),"Tracking UI gesture lesson failed.");
                var delivery=await Send("ghost-observation-delivery");
                var cadence=delivery.GetProperty("PreviewResult").GetProperty("ObservationDelivery");
                Require(cadence.GetArrayLength()==2,"30/60Hz WPF delivery measurements missing.");
                File.WriteAllText(Path.Combine(output,"observation-delivery.json"),cadence.GetRawText());
                var hybrid=await Send("ghost-hybrid-source");
                Require(hybrid.GetProperty("PreviewResult").GetProperty("SharedPointer").GetBoolean(),"Recorded hybrid optical path did not move the actual shared pointer.");
                File.WriteAllText(Path.Combine(output,"hybrid-source.json"),hybrid.GetProperty("PreviewResult").GetRawText());

                // Real mouse drag on A with shared Ghost input on B, checking rendered local positions.
                await FocusTrackingAsync(client,ProductHandle(editorState));AccessKey(0x57);
                var board=await Until(client,s=>Board(s).ValueKind==JsonValueKind.Object);
                await FocusTrackingAsync(client,Board(board).GetProperty("Handle").GetInt64());
                AccessKey(0x41);await Until(client,s=>Board(s).GetProperty("Cards").GetArrayLength()==1);
                AccessKey(0x52);Press(0x23);AccessKey(0x42);
                var two=await Until(client,s=>Board(s).GetProperty("Cards").GetArrayLength()==2);
                var grip=Slot(two,"A").GetProperty("Grip");double mouseX=grip.GetProperty("X").GetDouble(),mouseY=grip.GetProperty("Y").GetDouble();
                Input.Move(mouseX,mouseY);Input.Down();
                try
                {
                    await Until(client,s=>Claim(s,"Mouse")=="A");await Send("ghost-start");await Aim("workspace:B");await Send("ghost-arm");await Send("ghost-press");
                    var claimed=await Until(client,s=>Claim(s,"Mouse")=="A"&&Claim(s,"Ghost")=="B");
                    double aX=Slot(claimed,"A").GetProperty("X").GetDouble(),bX=Slot(claimed,"B").GetProperty("X").GetDouble();
                    var mouseBefore=Input.Cursor();await Send("ghost-move|40|0");
                    var advanced=await Until(client,s=>Slot(s,"B").GetProperty("X").GetDouble()>bX+20);
                    var mouseAfter=Input.Cursor();Require(mouseAfter.X==mouseBefore.X&&mouseAfter.Y==mouseBefore.Y&&Slot(advanced,"A").GetProperty("X").GetDouble()==aX,"Pointers crossed ownership.");
                    var logical=DesktopGhost(advanced).GetProperty("Position");var rendered=DesktopGhost(advanced).GetProperty("RenderedPoint");
                    Require(Math.Abs(logical.GetProperty("X").GetDouble()-rendered.GetProperty("X").GetDouble())<2&&
                        Math.Abs(logical.GetProperty("Y").GetDouble()-rendered.GetProperty("Y").GetDouble())<2,"Rendered and routed pointers disagree.");
                    Require((ReadDemoWindowStyle(new(DesktopGhost(advanced).GetProperty("OverlayHandle").GetInt64()),-20).ToInt64()&0x080800A0)==0x080800A0,"Passive styles missing.");
                    Input.Move(mouseX+16,mouseY);await Until(client,s=>Slot(s,"A").GetProperty("X").GetDouble()>aX+8);
                    await Send("ghost-release");Require(Claim(await Send("status"),"Mouse")=="A","Ghost release canceled Mouse.");
                    await Aim("workspace:B");await Send("ghost-press");await Send("ghost-loss");
                    var lost=await Send("status");Require(Claim(lost,"Ghost") is null&&Claim(lost,"Mouse")=="A","Loss failed ownership cancellation.");
                    await Send("ghost-press");Require(Claim(await Send("status"),"Ghost") is null,"Late intent rearmed after loss.");
                }
                finally{Input.Up();}
                AccessKey(0x47);await Until(client,s=>Claim(s,"Ghost")=="B");
                var replaced=await Send("ghost-start");string generation=DesktopGhost(replaced).GetProperty("Generation").GetString()!;
                await Task.Delay(200);
                var replacement=await Send("status");Require(DesktopMode(replacement)=="Preview"&&DesktopGhost(replacement).GetProperty("Generation").GetString()==generation,
                    "An old simulation timer stopped its replacement source.");
                await Aim(card);await Send("ghost-arm");await Send("ghost-press");await Send("product-close-board");
                Require(DesktopGhost(await Send("status")).GetProperty("ActiveTarget").GetString()==card,
                    "Closing Workspace canceled an unrelated floating-card claim.");
                await Send("ghost-release");
                await Send("ghost-stop");var stopped=await Send("status");Require(!DesktopGhost(stopped).GetProperty("OverlayVisible").GetBoolean()&&DesktopGhost(stopped).GetProperty("ActiveTarget").ValueKind==JsonValueKind.Null,"Stop left pointer or claim.");
                Require(CameraOff(stopped),"Controlled route activated camera.");
                await FocusTrackingAsync(client,ProductHandle(stopped));AccessKey(0x54);
                var lab=await Until(client,s=>Product(s).GetProperty("TrackingHandle").GetInt64()!=0);
                long labHandle=Product(lab).GetProperty("TrackingHandle").GetInt64();
                await Send("ghost-recorded-preview");await FocusTrackingAsync(client,labHandle);AccessKey(0x45);
                var preview=await Until(client,s=>DesktopGhost(s).GetProperty("OverlayVisible").GetBoolean()&&
                    DesktopGhost(s).GetProperty("Source").GetString()=="Recorded"&&DesktopMode(s)=="Preview",12000);
                await Send("ghost-arm");Require(DesktopMode(await Send("status"))!="Armed","Recorded inference gained action authority.");
                int worker=DemoTracking(preview).GetProperty("LastWorkerPid").GetInt32();
                await FocusTrackingAsync(client,labHandle);AccessKey(0x54);
                await Until(client,s=>DemoTracking(s).GetProperty("State").GetString()=="Stopped"&&!DesktopGhost(s).GetProperty("OverlayVisible").GetBoolean());
                Require(!ProcessExists(worker),"Recorded preview worker survived Stop.");
                await Send("demo-close");await client.Command("exit");await host.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(host.ExitCode==0&&!Input.Exists(new(cardHandle)),"Owned product failed cleanup.");
            }
            finally{FinishProductHost(host);}
            var(restarted,second)=StartAcceptanceHost(app,output);
            using(restarted)using(second)
            try
            {
                await second.Connect();await second.Command("dual-demo-start");
                var ready=await Until(second,s=>Product(s).GetProperty("WindowLoaded").GetBoolean());
                await FocusTrackingAsync(second,ProductHandle(ready));AccessKey(0x50);
                var restored=await Until(second,s=>ProductPocket(s).ValueKind==JsonValueKind.Object&&ProductPocket(s).GetProperty("Cards").GetInt32()>0);
                var b=ProductPocket(restored).GetProperty("ViewerBounds");
                Require(ProductPocket(restored).GetProperty("Pinned").GetBoolean()&&Math.Abs(b.GetProperty("X").GetDouble()-parkedX)<4&&
                    Math.Abs(b.GetProperty("Y").GetDouble()-parkedY)<4,"Ghost placement/pin failed restart persistence.");
                await second.Command("ghost-start");
                string restoredCard=DesktopGhost(await second.Command("status")).GetProperty("Targets").EnumerateArray().First(t=>t.GetProperty("Id").GetString()!.StartsWith("card:")).GetProperty("Id").GetString()!;
                await second.Command("ghost-aim|"+restoredCard);await second.Command("ghost-arm");await second.Command("ghost-press");
                long restoredHandle=ProductPocket(restored).GetProperty("ViewerHandle").GetInt64();
                await FocusTrackingAsync(second,restoredHandle);AccessKey(0x48); // Real supported Hide while Ghost owns it.
                var hidden=await Until(second,s=>ProductPocket(s).GetProperty("Cards").GetInt32()==0);
                Require(DesktopGhost(hidden).GetProperty("ActiveTarget").ValueKind==JsonValueKind.Null&&!Input.Exists(new(restoredHandle)),"Closed card retained Ghost ownership.");
                await second.Command("ghost-stop");
                await second.Command("demo-close");await second.Command("exit");await restarted.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(restarted.ExitCode==0,"Restart exit failed.");
            }
            finally{FinishProductHost(restarted);}
            timings.Sort();return new{SharedPointer=true,RealShelf=true,RealCardMoved=true,NativePinUnpin=true,ConcurrentMouse=true,KeyboardSteering=true,EscapeStop=true,
                EditorExactTyping=true,PassiveStyles=true,PointerRenderMatches=true,LossCanceled=true,RestartPersisted=true,RecordedPreviewConnected=true,RecordedActionsDenied=true,ClosedCardReleased=true,
                MedianRoundTripMs=timings[timings.Count/2],P95RoundTripMs=timings[(int)(timings.Count*.95)],CameraOpened=false};
        });
    }
}
