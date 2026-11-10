using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using Pocket.Automation.Contracts;

namespace Pocket.Automation.Worker;
internal static partial class Program
{
    private static Reply HandleExternal(Request r,ExternalGrant grant,Dictionary<string,Capability> tokens,ref bool dispatched)
    {
        if(!grant.Permits(r) || !ExternalWindows.Current(grant))return new(r.Id,"Denied",Error:"External window grant expired or changed.");
        if(!ExternalWindows.Reachable(grant,r.X!.Value,r.Y!.Value))return r.Operation=="Observe"
            ?new(r.Id,"Observed",Observation:Observation.TargetUnavailable):new(r.Id,"Stale",Error:"Selected window is covered, hidden or unavailable.");
        Capability? capability=null;
        if(r.Operation=="Execute" && (!tokens.Remove(r.Token!,out capability) || capability.Request.Window!=r.Window || capability.Request.ProcessId!=r.ProcessId || capability.Request.Target!=r.Target || capability.Request.X!=r.X || capability.Request.Y!=r.Y || capability.Request.Grant!=r.Grant || capability.Request.Generation!=r.Generation ||
            Stopwatch.GetElapsedTime(capability.Created)>TimeSpan.FromMilliseconds(300)))return new(r.Id,"Stale");
        var root=AutomationElement.FromHandle(new(grant.Window));
        if(root.Current.ProcessId!=grant.ProcessId)return new(r.Id,"Denied");
        var element=ResolveExternal(root,r,out bool limited);
        if(r.Operation=="Observe")return Observe(r,root,element,element is null?"Unsupported":"ExternalScopedTree",r.Target!);
        if(element is null)return new(r.Id,limited?"Unavailable":"Unsupported",Error:limited?"Scoped discovery limit reached.":"No eligible pattern at Ghost.");
        var binding=Bind(root,element,r.Target!);
        if(r.Operation=="ProbePoint")
        {
            if(tokens.Count>=32)tokens.Clear();
            string token=Guid.NewGuid().ToString();tokens[token]=new(r,Stopwatch.GetTimestamp(),binding);
            // Only the current eligible control's short name is returned in memory; no values or trees.
            string label=element.Current.Name ?? "";if(label.Length>80)label=label[..80];
            return new(r.Id,"Confirmed",token,r.Target,Binding:binding,Resolution:"ExternalScopedTree",Label:label);
        }
        if(capability is null || !capability.Binding.SameElement(binding) ||
            !ExternalWindows.Reachable(grant,r.X.Value,r.Y.Value) || !ExternalWindows.Current(grant))return new(r.Id,"Stale");
        if(r.Target is "ExpandCollapse" or "SelectionItem")
        {
            if(Stopwatch.GetElapsedTime(capability.Created)>TimeSpan.FromMilliseconds(300)||binding.Before!=capability.Binding.Before)return new(r.Id,"Stale");
            if(r.Target=="ExpandCollapse")
            {
                var pattern=(ExpandCollapsePattern)element.GetCurrentPattern(ExpandCollapsePattern.Pattern);
                if(pattern.Current.ExpandCollapseState is not (ExpandCollapseState.Collapsed or ExpandCollapseState.Expanded))return new(r.Id,"Unsupported");
                dispatched=true;
                if(binding.Before==0)pattern.Expand();else pattern.Collapse();
                double after=(int)pattern.Current.ExpandCollapseState;
                return new(r.Id,after==(binding.Before==0?1:0)?"Confirmed":"Attempted",Pattern:r.Target,Value:after);
            }
            var selection=(SelectionItemPattern)element.GetCurrentPattern(SelectionItemPattern.Pattern);
            if(selection.Current.IsSelected)return new(r.Id,"Denied",Error:"Already selected.");
            dispatched=true;selection.Select();
            return new(r.Id,selection.Current.IsSelected?"Confirmed":"Attempted",Pattern:r.Target,Value:selection.Current.IsSelected?1:0);
        }
        return Execute(r,element,capability,ref dispatched);
    }
    private static AutomationElement? ResolveExternal(AutomationElement root,Request r,out bool limited)
    {
        var walker=TreeWalker.ControlViewWalker;
        var queue=new Queue<(AutomationElement Element,int Depth)>();queue.Enqueue((root,0));
        AutomationElement? selected=null;int deepest=-1,visited=0;limited=false;
        var point=new Point(r.X!.Value,r.Y!.Value);
        while(queue.Count>0)
        {
            if(++visited>256){limited=true;return null;}
            var (element,depth)=queue.Dequeue();var state=element.Current;var bounds=state.BoundingRectangle;
            if(bounds.IsEmpty||!bounds.Contains(point))continue;
            // Never fall through a sensitive/disabled child to an otherwise actionable parent
            // (for example Scroll on a container with an edit/password field under Ghost).
            if(state.ProcessId!=r.ProcessId||state.IsPassword||state.ControlType==ControlType.Edit||!state.IsEnabled||state.IsOffscreen||
                !root.Current.BoundingRectangle.Contains(bounds))return null;
            bool supported=SupportsExternal(element,r.Target!);
            if(supported && depth>=deepest){selected=element;deepest=depth;}
            if(depth>=10){limited=true;return null;}
            for(var child=walker.GetFirstChild(element);child is not null;child=walker.GetNextSibling(child))
            {
                if(queue.Count+visited>=256){limited=true;return null;}
                queue.Enqueue((child,depth+1));
            }
        }
        return selected;
    }
    private static bool SupportsExternal(AutomationElement e,string pattern)
    {
        var type=e.Current.ControlType;
        return pattern switch {
            "Invoke"=>type==ControlType.Button && Supports(e,pattern),
            "Toggle"=>(type==ControlType.CheckBox||type==ControlType.Button) && Supports(e,pattern),
            "RangeValue"=>type==ControlType.Slider && Supports(e,pattern),
            "Scroll"=>Supports(e,pattern),
            "ExpandCollapse"=>e.TryGetCurrentPattern(ExpandCollapsePattern.Pattern,out _),
            "SelectionItem"=>(type==ControlType.ListItem||type==ControlType.TabItem||type==ControlType.RadioButton)&&e.TryGetCurrentPattern(SelectionItemPattern.Pattern,out _),_=>false};
    }
}
