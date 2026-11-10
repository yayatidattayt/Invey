using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using Pocket.Automation.Contracts;

namespace Pocket.Automation.Worker;

internal static partial class Program
{
    private sealed record Capability(Request Request, long Created, TargetBinding Binding);
    [MTAThread] private static int Main(string[] args)
    {
        SetProcessDpiAwarenessContext(new nint(-4));
        ExternalGrant? external=null;
        int allowedPid;long lifetime;
        if(args.Length==2 && args[0]=="--external-grant")
        {
            try{external=JsonSerializer.Deserialize<ExternalGrant>(args[1]);}catch(JsonException){return 2;}
            if(external is null || !external.Valid || !ExternalWindows.Current(external))return 2;
            allowedPid=external.ProcessId;lifetime=external.Lifetime;
        }
        else if(args.Length!=4 || args[0]!="--fixture-pid" || !int.TryParse(args[1],out allowedPid) || args[2]!="--lifetime" || !long.TryParse(args[3],out lifetime))return 2;
        var tokens=new Dictionary<string,Capability>();var requests=new RequestLedger();
        try
        {
            while(ReadBounded() is { } line)
            {
                Request? request = null;
                bool dispatched = false;
                Reply result;
                try
                {
                    request = JsonSerializer.Deserialize<Request>(line) ?? throw new InvalidDataException("Missing request.");
                    if (!Protocol.Valid(request) || !requests.TryAdd(request.Id))
                        result = new(request.Id, "Denied", Error: "Invalid request, duplicate ID or session limit.");
                    else
                        result = external is null ? request.Version==1 ? Handle(request, allowedPid, lifetime, tokens, ref dispatched) : new(request.Id,"Denied")
                            : HandleExternal(request,external,tokens,ref dispatched);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    result = new(request?.Id ?? "invalid", "Unsupported", Error: ex.GetType().Name);
                }

                if (request?.Operation == "Execute")
                {
                    result = result.WithOutcome(dispatched
                        ? result.Status == "Confirmed" ? MutationOutcome.Applied : MutationOutcome.Unknown
                        : result.Status is "Stale" or "AmbiguousTarget" ? MutationOutcome.Conflict : MutationOutcome.Rejected);
                }
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
            return 0;
        }
        catch(IOException){return 3;}
    }
    private static Reply Handle(Request request, int allowedPid, long lifetime,
        Dictionary<string, Capability> tokens, ref bool dispatched)
    {
        Capability? capability = null;
        if (request.Operation == "Execute" &&
            (!tokens.Remove(request.Token!, out capability) ||
             capability.Request.Window != request.Window || capability.Request.ProcessId != request.ProcessId ||
             capability.Request.Target != request.Target || capability.Request.X != request.X ||
             capability.Request.Y != request.Y ||
             Stopwatch.GetElapsedTime(capability.Created) > TimeSpan.FromMilliseconds(300)))
            return new(request.Id, "Stale");

        var root = request.ProcessId == allowedPid ? GetFixture(request.Window, allowedPid, lifetime) : null;
        if (root is null)
            return request.Operation switch
            {
                "Observe" => new(request.Id, "Observed", Observation: Observation.TargetUnavailable),
                "Execute" => new(request.Id, "Stale", Error: "Original fixture lifetime is unavailable."),
                _ => new(request.Id, "Denied", Error: "Registered fixture lifetime required.")
            };

        if (request.Operation == "Observe" && string.Join(",", root.GetRuntimeId()) != request.Binding!.Root)
            return new(request.Id, "Observed", Observation: Observation.TargetChanged);

        var (element, status) = Resolve(root, request);
        string pattern = Protocol.Targets[request.Target!];

        if (request.Operation == "Observe")
            return Observe(request, root, element, status, pattern);

        if (request.Operation is "Probe" or "ProbePoint")
        {
            if (element is null || !Supports(element, pattern))
                return new(request.Id, status == "ScopedTree" ? "Unsupported" : status);

            var binding = Bind(root, element, pattern);
            if (tokens.Count >= 32) tokens.Clear();
            string token = Guid.NewGuid().ToString();
            tokens[token] = new(request, Stopwatch.GetTimestamp(), binding);
            return new(request.Id, "Confirmed", token, pattern, Resolution: status, Binding: binding);
        }

        if (element is null || capability is null)
            return new(request.Id, "Stale");

        if (!capability.Binding.SameElement(Bind(root, element, "")) ||
            Stopwatch.GetElapsedTime(capability.Created) > TimeSpan.FromMilliseconds(300))
            return new(request.Id, "Stale", Error: "Target identity or capability lifetime changed.");

        return Execute(request, element, capability, ref dispatched);
    }

    private static AutomationElement? GetFixture(long hwnd, int pid, long lifetime)
    {
        if (!IsWindow(new nint(hwnd))) return null;
        GetWindowThreadProcessId(new nint(hwnd), out uint actual);
        if (actual != pid) return null;
        using var process = Process.GetProcessById(pid);
        if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != lifetime ||
            process.ProcessName is not ("Invey" or "Pocket.App")) return null;
        var root = AutomationElement.FromHandle(new nint(hwnd));
        return root.Current.AutomationId == "GhostFixture.Window" ? root : null;
    }

    private static TargetBinding Bind(AutomationElement root, AutomationElement element, string pattern)
    {
        var bounds = element.Current.BoundingRectangle;
        var identity = new TargetBinding(string.Join(",", root.GetRuntimeId()), string.Join(",", element.GetRuntimeId()),
            bounds.Left, bounds.Top, bounds.Width, bounds.Height, null);
        return pattern switch
        {
            "ExpandCollapse" when element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out var expand) =>
                identity with { Before=(int)((ExpandCollapsePattern)expand).Current.ExpandCollapseState },
            "SelectionItem" when element.TryGetCurrentPattern(SelectionItemPattern.Pattern,out var select) =>
                identity with { Before=((SelectionItemPattern)select).Current.IsSelected?1:0 },
            "Toggle" when element.TryGetCurrentPattern(TogglePattern.Pattern, out var toggle) =>
                identity with { Before = (int)((TogglePattern)toggle).Current.ToggleState },
            "RangeValue" when element.TryGetCurrentPattern(RangeValuePattern.Pattern, out var range) =>
                BindRange(identity, ((RangeValuePattern)range).Current),
            "Scroll" when element.TryGetCurrentPattern(ScrollPattern.Pattern, out var scroll) =>
                identity with { Before = Finite(((ScrollPattern)scroll).Current.VerticalScrollPercent) },
            _ => identity
        };
    }

    private static TargetBinding BindRange(TargetBinding identity, RangeValuePattern.RangeValuePatternInformation range) =>
        identity with { Before = Finite(range.Value), Minimum = Finite(range.Minimum), Maximum = Finite(range.Maximum) };

    private static double? Finite(double value) => double.IsFinite(value) ? value : null;

    private static Reply Observe(Request request, AutomationElement root, AutomationElement? element, string status, string pattern)
    {
        if (element is null)
            return new(request.Id, "Observed", Observation: status == "AmbiguousTarget" ? Observation.TargetChanged : Observation.TargetUnavailable);

        var original = request.Binding!;
        // Compare opaque identity before reading the pattern of a replacement element.
        var current = Bind(root, element, "");
        if (!original.SameElement(current))
            return new(request.Id, "Observed", Observation: Observation.TargetChanged);

        current = Bind(root, element, pattern);
        if (!original.SameElement(current))
            return new(request.Id, "Observed", Observation: Observation.TargetChanged);

        if (current.Before is not double value || original.Before is not double before)
            return new(request.Id, "Observed", Observation: Observation.Inconclusive);

        Observation observation = pattern switch
        {
            "RangeValue" when current.Minimum != original.Minimum || current.Maximum != original.Maximum => Observation.StateConflict,
            "RangeValue" when request.Value is double expected && Matches(value, expected) => Observation.ExpectedObserved,
            "ExpandCollapse" when before is 0 or 1 && value==1-before => Observation.ExpectedObserved,
            "SelectionItem" when before==0 && value==1 => Observation.ExpectedObserved,
            "Toggle" when before is 0 or 1 && value == 1 - before => Observation.ExpectedObserved,
            "Scroll" when value > before && value <= 100 => Observation.ExpectedObserved,
            _ when value == before => Observation.PriorStateObserved,
            _ => Observation.StateConflict
        };
        return new(request.Id, "Observed", Pattern: pattern, Value: value, Observation: observation);
    }

    private static bool Matches(double value, double expected) =>
        double.IsFinite(value) && Math.Abs(value - expected) <= 1e-9 * Math.Max(1, Math.Abs(expected));

    private static (AutomationElement? Element,string Status) Resolve(AutomationElement root,Request r)
    {
        // Scope starts at an independently authorized HWND. Desktop-global hit testing can return the overlay.
        // Tree enumeration is confined to this controlled fixture; the supervisor bounds blocked provider calls.
        var matches=root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,r.Target));
        if(matches.Count>1)return(null,"AmbiguousTarget");
        if(matches.Count!=1)return(null,"Unsupported");
        var e=matches[0];var state=e.Current;var bounds=state.BoundingRectangle;
        if(state.ProcessId!=r.ProcessId || !state.IsEnabled || state.IsPassword || state.IsOffscreen || bounds.IsEmpty ||
            !root.Current.BoundingRectangle.Contains(bounds))return(null,"Unsupported");
        if(r.X.HasValue && !bounds.Contains(new System.Windows.Point(r.X.Value,r.Y!.Value)))return(null,"Unsupported");
        return(e,"ScopedTree");
    }
    private static bool Supports(AutomationElement e,string pattern)=>pattern switch
    {
        "Invoke"=>e.TryGetCurrentPattern(InvokePattern.Pattern,out _),"Toggle"=>e.TryGetCurrentPattern(TogglePattern.Pattern,out _),
        "RangeValue"=>e.TryGetCurrentPattern(RangeValuePattern.Pattern,out _),"Scroll"=>e.TryGetCurrentPattern(ScrollPattern.Pattern,out _),_=>false
    };
    private static Reply Execute(Request r, AutomationElement e, Capability capability, ref bool dispatched)
    {
        string pattern=r.Version==2?r.Target!:Protocol.Targets[r.Target!];
        var original = capability.Binding;
        bool Expired() => Stopwatch.GetElapsedTime(capability.Created) > TimeSpan.FromMilliseconds(300);
        switch(pattern)
        {
            case "Invoke":
                var invoke = (InvokePattern)e.GetCurrentPattern(InvokePattern.Pattern);
                if (Expired()) return new(r.Id, "Stale");
                dispatched = true;
                invoke.Invoke();
                return new(r.Id, "Attempted", Pattern: pattern);
            case "Toggle":
                var toggle=(TogglePattern)e.GetCurrentPattern(TogglePattern.Pattern);var before=toggle.Current.ToggleState;
                if (before is not (ToggleState.On or ToggleState.Off)) return new(r.Id, "Unsupported");
                if ((int)before != original.Before) return new(r.Id, "Stale", Error: "Toggle state changed.");
                if (Expired()) return new(r.Id, "Stale");
                dispatched = true;
                toggle.Toggle();var after=toggle.Current.ToggleState;
                return new(r.Id,after==(before==ToggleState.On?ToggleState.Off:ToggleState.On)?"Confirmed":"Attempted",Pattern:pattern,Value:(int)after);
            case "RangeValue":
                var range=(RangeValuePattern)e.GetCurrentPattern(RangeValuePattern.Pattern);
                var state = range.Current;
                double minimum = state.Minimum, maximum = state.Maximum;
                if(r.Value is null || !double.IsFinite(minimum) || !double.IsFinite(maximum) || r.Value<minimum || r.Value>maximum || state.IsReadOnly)return new(r.Id,"Denied");
                if (minimum != original.Minimum || maximum != original.Maximum || Finite(state.Value) != original.Before)
                    return new(r.Id, "Stale", Error: "Range state changed.");
                if (Expired()) return new(r.Id, "Stale");
                dispatched = true;
                range.SetValue(r.Value.Value);double actual=range.Current.Value;
                bool matched=Matches(actual, r.Value.Value);
                return new(r.Id,matched?"Confirmed":"Attempted",Pattern:pattern,Value:Finite(actual),Error:matched?null:"Requested value not observed.");
            case "Scroll":
                var scroll=(ScrollPattern)e.GetCurrentPattern(ScrollPattern.Pattern);double start=scroll.Current.VerticalScrollPercent;
                if(!scroll.Current.VerticallyScrollable || !double.IsFinite(start) || start<0 || start>=100)return new(r.Id,"Unsupported");
                if (start != original.Before) return new(r.Id, "Stale", Error: "Scroll position changed.");
                if (Expired()) return new(r.Id, "Stale");
                dispatched = true;
                scroll.Scroll(ScrollAmount.NoAmount,ScrollAmount.SmallIncrement);double position=scroll.Current.VerticalScrollPercent;
                return new(r.Id,double.IsFinite(position)&&position>start&&position<=100?"Confirmed":"Attempted",Pattern:pattern,Value:Finite(position));
            default:return new(r.Id,"Unsupported");
        }
    }
    private static string? ReadBounded()
    {
        var text=new StringBuilder();int c;
        while((c=Console.In.Read())!=-1){if(c=='\n')return text.ToString().TrimEnd('\r');if(text.Length>=Protocol.MaxLine)throw new IOException("Oversized IPC.");text.Append((char)c);}
        if(text.Length>0)throw new IOException("Partial IPC.");return null;
    }
    [DllImport("user32.dll")]private static extern bool SetProcessDpiAwarenessContext(nint context);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint hwnd,out uint pid);
    [DllImport("user32.dll")]private static extern bool IsWindow(nint hwnd);
}
