using System.Text.Json.Serialization;

namespace Pocket.Automation.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<MutationOutcome>))]
public enum MutationOutcome { Applied, Rejected, Unknown, Conflict }

[JsonConverter(typeof(JsonStringEnumConverter<Observation>))]
public enum Observation { ExpectedObserved, PriorStateObserved, StateConflict, TargetUnavailable, TargetChanged, Inconclusive }

public sealed record TargetBinding(string Root, string Element, double Left, double Top, double Width, double Height,
    double? Before, double? Minimum = null, double? Maximum = null)
{
    public bool SameElement(TargetBinding other) =>
        Root == other.Root && Element == other.Element && Left == other.Left && Top == other.Top &&
        Width == other.Width && Height == other.Height;
}

public sealed record Request(int Version,string Id,string Operation,long Window,int ProcessId,string? Target=null,string? Token=null,double? Value=null,double? X=null,double? Y=null,
    TargetBinding? Binding=null);
public sealed record Reply(string Id,string Status,string? Token=null,string? Pattern=null,double? Value=null,string? Error=null,string? Resolution=null,bool OutcomeUnknown=false,
    MutationOutcome? Outcome=null, TargetBinding? Binding=null, Observation? Observation=null)
{
    public Reply WithOutcome(MutationOutcome outcome) => this with
    {
        Outcome = outcome,
        OutcomeUnknown = outcome == MutationOutcome.Unknown
    };
}
public static class Protocol
{
    public const int MaxLine=16384;
    public static readonly IReadOnlyDictionary<string,string> Targets=new Dictionary<string,string>
    { ["GhostFixture.Invoke"]="Invoke",["GhostFixture.Toggle"]="Toggle",["GhostFixture.Range"]="RangeValue",["GhostFixture.Scroll"]="Scroll" };
    public static bool Valid(Request r)=>r.Version==1 && Guid.TryParse(r.Id,out _) && r.Window>0 && r.ProcessId>0 &&
        r.Operation is "Probe" or "ProbePoint" or "Execute" or "Observe" && r.Target is not null && Targets.ContainsKey(r.Target) &&
        ((!r.X.HasValue && !r.Y.HasValue && r.Operation!="ProbePoint") || (r.X.HasValue && r.Y.HasValue && double.IsFinite(r.X.Value) && double.IsFinite(r.Y.Value))) &&
        (!r.Value.HasValue || double.IsFinite(r.Value.Value)) && (r.Operation!="Execute" || Guid.TryParse(r.Token,out _)) &&
        (r.Operation=="Observe" ? r.Binding is not null && r.Token is null : r.Binding is null);
}

 
/// <summary>Fail closed when request history reaches capacity; old IDs are never silently forgotten.</summary>
public sealed class RequestLedger
{
    private readonly HashSet<Guid> _seen=new();
    private readonly int _capacity;
    public RequestLedger(int capacity=4096)=>_capacity=capacity>0?capacity:throw new ArgumentOutOfRangeException(nameof(capacity));
    public bool TryAdd(string id)=>Guid.TryParse(id,out var key)&&_seen.Count<_capacity&&_seen.Add(key);
}
