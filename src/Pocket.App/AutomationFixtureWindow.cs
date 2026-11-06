using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Pocket.App;

/// <summary>Controlled A2 test target only; never shown in the consumer path.</summary>
internal sealed class AutomationFixtureWindow : Window
{
    private readonly CheckBox _toggle=new(){Content="Controlled toggle"};
    private readonly FixtureSlider _range=new(){Minimum=0,Maximum=100};
    private readonly ScrollViewer _scroll=new(){Height=100,VerticalScrollBarVisibility=ScrollBarVisibility.Visible};
    private readonly StackPanel _panel=new(){Margin=new(20)};
    private int _invocations;
    private Button _invoke=null!;
    public AutomationFixtureWindow()
    {
        Title="GHOST controlled A2 fixture";Left=80;Top=120;Width=400;Height=440;
        AutomationProperties.SetAutomationId(this,"GhostFixture.Window");
        AutomationProperties.SetAutomationId(_toggle,"GhostFixture.Toggle");
        AutomationProperties.SetAutomationId(_range,"GhostFixture.Range");
        AutomationProperties.SetAutomationId(_scroll,"GhostFixture.Scroll");
        _panel.Children.Add(new TextBlock());
        _panel.Children.Add(_toggle);_panel.Children.Add(_range);_panel.Children.Add(_scroll);Content=_panel;
        Configure("normal");
    }
    private Button NewInvoke()
    {
        var button=new Button{Content="Benign invocation counter"};
        button.Click+=(_,_)=>{_invocations++;button.Content="Invocations: "+_invocations;};
        AutomationProperties.SetAutomationId(button,"GhostFixture.Invoke");return button;
    }
    public void Configure(string mode)
    {
        if(mode=="scroll-end"){_scroll.ScrollToEnd();return;}
        _invoke=NewInvoke();_panel.Children.RemoveAt(0);_panel.Children.Insert(0,_invoke);
        _toggle.IsEnabled=true;_range.IsEnabled=true;_range.ReadOnly=mode=="read-only";_range.IgnoreWrites=mode=="ignore-range";_range.RejectWrites=mode=="reject-range";
        if(_range.IgnoreWrites||_range.RejectWrites)_range.Value=13;
        var content=new StackPanel();for(int i=0;i<(mode=="no-scroll"?1:60);i++)content.Children.Add(new TextBlock{Text="Controlled row "+i,Height=24});
        _scroll.Content=content;_scroll.ScrollToTop();
        switch(mode)
        {
            case "disabled":_invoke.IsEnabled=false;_toggle.IsEnabled=false;_range.IsEnabled=false;break;
            case "duplicate":
                var grid=new Grid();_panel.Children.RemoveAt(0);grid.Children.Add(_invoke);grid.Children.Add(NewInvoke());_panel.Children.Insert(0,grid);break;
            case "missing":_invoke.Visibility=Visibility.Collapsed;AutomationProperties.SetAutomationId(_invoke,"Removed");break;
            case "no-pattern":var text=new TextBlock{Text="No Invoke pattern"};AutomationProperties.SetAutomationId(text,"GhostFixture.Invoke");_panel.Children.RemoveAt(0);_panel.Children.Insert(0,text);break;
        }
        UpdateLayout();
    }
    private sealed class FixtureSlider : Slider
    {
        public bool ReadOnly {get;set;}
        public bool IgnoreWrites {get;set;}
        public bool RejectWrites {get;set;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new FixtureSliderPeer(this);
    }
    private sealed class FixtureSliderPeer(FixtureSlider owner) : SliderAutomationPeer(owner),IRangeValueProvider
    {
        public override object GetPattern(PatternInterface patternInterface)=>patternInterface==PatternInterface.RangeValue?this:base.GetPattern(patternInterface);
        public bool IsReadOnly=>owner.ReadOnly;
        public double LargeChange=>owner.LargeChange;
        public double SmallChange=>owner.SmallChange;
        public double Maximum=>owner.Maximum;
        public double Minimum=>owner.Minimum;
        public double Value=>owner.Value;
        public void SetValue(double value){if(IsReadOnly||owner.RejectWrites)throw new InvalidOperationException("Controlled range rejection.");if(!owner.IgnoreWrites)owner.Value=value;}
    }
    public object Snapshot()
    {
        var p=_invoke.IsLoaded?_invoke.PointToScreen(new Point(_invoke.ActualWidth/2,_invoke.ActualHeight/2)):new Point();
        return new{Invocations=_invocations,Toggle=_toggle.IsChecked==true,Range=_range.Value,ScrollOffset=_scroll.VerticalOffset,InvokePoint=new{p.X,p.Y}};
    }
}
