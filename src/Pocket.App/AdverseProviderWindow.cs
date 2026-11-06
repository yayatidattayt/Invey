using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Interop;

namespace Pocket.App;

/// <summary>Separate test-only process. Blocks real provider callbacks, never GHOST's main dispatcher.</summary>
internal sealed class AdverseProviderWindow : Window
{
    public AdverseProviderWindow(string directory)
    {
        Title="GHOST adverse UIA provider";Left=850;Top=70;Width=400;Height=300;
        AutomationProperties.SetAutomationId(this,"GhostFixture.Window");
        var range = new AdverseRange(directory) { Minimum = 0, Maximum = 100, Value = 10, Width = 220 };
        AutomationProperties.SetAutomationId(range, "GhostFixture.Range");
        var toggle = new AdverseToggle(directory) { Content = "Controlled toggle target" };
        AutomationProperties.SetAutomationId(toggle, "GhostFixture.Toggle");
        var scroll = new AdverseScroll(directory) { Content = "Controlled scroll at 0%", Tag = 0d };
        AutomationProperties.SetAutomationId(scroll, "GhostFixture.Scroll");
        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children = { new AdverseButton(directory) { Content = "Controlled provider query target" }, range, toggle, scroll }
        };
        ContentRendered += (_, _) => AcceptanceHost.PublishReady(directory, new WindowInteropHelper(this).Handle);
    }

    private sealed class AdverseRange(string directory) : Slider
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new AdverseRangePeer(this, directory);
    }

    private sealed class AdverseRangePeer(Slider owner, string directory) : SliderAutomationPeer(owner), System.Windows.Automation.Provider.IRangeValueProvider
    {
        private Slider Range => (Slider)Owner;
        public override object GetPattern(PatternInterface pattern) => pattern == PatternInterface.RangeValue ? this : base.GetPattern(pattern);
        public bool IsReadOnly => false;
        public double LargeChange => Range.LargeChange;
        public double SmallChange => Range.SmallChange;
        public double Maximum => Range.Maximum;
        public double Minimum => Range.Minimum;
        public double Value
        {
            get
            {
                if (File.Exists(Path.Combine(directory, "mutation-slow-validation"))) Thread.Sleep(400);
                return Range.Value;
            }
        }
        public void SetValue(double value) => ApplyMutation(directory, () => Range.Value = value, () => Range.Value = 33);
    }

    private sealed class AdverseToggle(string directory) : CheckBox
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new AdverseTogglePeer(this, directory);
    }

    private sealed class AdverseTogglePeer(CheckBox owner, string directory) : CheckBoxAutomationPeer(owner), System.Windows.Automation.Provider.IToggleProvider
    {
        private CheckBox ToggleOwner => (CheckBox)Owner;
        public override object GetPattern(PatternInterface pattern) => pattern == PatternInterface.Toggle ? this : base.GetPattern(pattern);
        public ToggleState ToggleState => ToggleOwner.IsChecked == true ? ToggleState.On : ToggleState.Off;
        public void Toggle() => ApplyMutation(directory, () => ToggleOwner.IsChecked = ToggleOwner.IsChecked != true);
    }

    private sealed class AdverseScroll(string directory) : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new AdverseScrollPeer(this, directory);
    }

    private sealed class AdverseScrollPeer(Button owner, string directory) : ButtonAutomationPeer(owner), System.Windows.Automation.Provider.IScrollProvider
    {
        public override object GetPattern(PatternInterface pattern) => pattern == PatternInterface.Scroll ? this : base.GetPattern(pattern);
        public bool HorizontallyScrollable => false;
        public bool VerticallyScrollable => true;
        public double HorizontalScrollPercent => -1;
        public double VerticalScrollPercent => (double)((Button)Owner).Tag;
        public double HorizontalViewSize => 100;
        public double VerticalViewSize => 20;
        public void SetScrollPercent(double horizontal, double vertical) => throw new NotSupportedException();
        public void Scroll(ScrollAmount horizontal, ScrollAmount vertical)
        {
            if (horizontal != ScrollAmount.NoAmount || vertical != ScrollAmount.SmallIncrement) throw new NotSupportedException();
            ApplyMutation(directory, () =>
            {
                var button = (Button)Owner;
                button.Tag = Math.Min(100, VerticalScrollPercent + 10);
                button.Content = $"Controlled scroll at {button.Tag}%";
            });
        }
    }

    private static void ApplyMutation(string directory, Action apply, Action? concurrent = null)
    {
        string arm = Path.Combine(directory, "mutation-arm");
        string mode = File.Exists(arm) ? File.ReadAllText(arm) : "";
        File.AppendAllText(Path.Combine(directory, "mutation-calls"), mode + Environment.NewLine);
        if (mode == "before") WaitForRelease(directory);
        apply();
        File.WriteAllText(Path.Combine(directory, "mutation-applied"), "Applied");
        if (mode == "after") WaitForRelease(directory);
        if (File.Exists(Path.Combine(directory, "mutation-concurrent"))) concurrent?.Invoke();
    }

    private static void WaitForRelease(string directory)
    {
        File.WriteAllText(Path.Combine(directory, "mutation-entered"), "Mutation callback");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(directory, "mutation-release")) && watch.Elapsed < TimeSpan.FromSeconds(12))
            Thread.Sleep(10);
    }
    private sealed class AdverseButton(string directory) : Button
    {
        protected override AutomationPeer OnCreateAutomationPeer()=>new AdversePeer(this,directory);
    }
    private sealed class AdversePeer(Button owner,string directory) : ButtonAutomationPeer(owner)
    {
        private void Fault(string callback)
        {
            string arm=Path.Combine(directory,"arm");
            if(!File.Exists(arm))return;
            string mode=File.ReadAllText(arm);
            File.WriteAllText(Path.Combine(directory,"entered"),callback);
            if(mode=="throw")throw new InvalidOperationException("Controlled provider failure.");
            if(mode=="slow"){Thread.Sleep(150);return;}
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(!File.Exists(Path.Combine(directory,"release")) && watch.Elapsed<TimeSpan.FromSeconds(30))Thread.Sleep(10);
            File.WriteAllText(Path.Combine(directory,"released"),callback);
        }
        protected override string GetAutomationIdCore(){Fault("GetAutomationIdCore");return "GhostFixture.Invoke";}
        public override object GetPattern(PatternInterface patternInterface){Fault("GetPattern");return base.GetPattern(patternInterface);}
    }
}
