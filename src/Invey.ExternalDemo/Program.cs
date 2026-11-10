using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Invey.ExternalDemo;
internal sealed class DemoWindow:Window
{
    private readonly FaultButton _button=new(){Content="Increment harmless counter",Height=36};
    private readonly CheckBox _toggle=new(){Content="Demo option",Height=30};
    private readonly Slider _range=new(){Minimum=0,Maximum=100,Value=20,Height=28};
    private readonly Expander _expand=new(){Header="Demo details",Content=new TextBlock{Text="Expanded demonstration content"}};
    private readonly ListBox _list=new(){Height=68,ItemsSource=new[]{"First demo item","Second demo item"},SelectedIndex=0};
    private readonly ScrollViewer _scroll=new(){Height=95,VerticalScrollBarVisibility=ScrollBarVisibility.Visible};
    private readonly PasswordBox _innerSecret=new(){Height=26};
    private readonly PasswordBox _password=new(){Height=26};
    private readonly Button _disabled=new(){Content="Disabled demo action",IsEnabled=false,Height=26};
    private readonly TextBlock _counter=new();
    private int _count;
    private Window? _cover;
    public DemoWindow()
    {
        Title="Invey External Demo · synthetic content";Left=60;Top=60;Width=440;Height=600;
        var panel=new StackPanel{Margin=new(20)};Content=panel;
        panel.Children.Add(new TextBlock{Text="Separate-process semantic control demo",FontSize=18,TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new TextBlock{Text="Synthetic content only · no webcam or native input injection",TextWrapping=TextWrapping.Wrap});
        foreach(var (element,id) in new (FrameworkElement,string)[]{(_button,"Invoke"),(_toggle,"Toggle"),(_range,"RangeValue"),(_expand,"ExpandCollapse"),(_list,"SelectionItem"),(_scroll,"Scroll"),(_password,"Password"),(_disabled,"Disabled")})
        {AutomationProperties.SetAutomationId(element,"ExternalDemo."+id);panel.Children.Add(element);}
        var lines=new StackPanel();lines.Children.Add(_innerSecret);for(int i=0;i<35;i++)lines.Children.Add(new TextBlock{Text=$"Synthetic line {i}",Height=22});_scroll.Content=lines;
        panel.Children.Add(_counter);_counter.Text="Counter: 0";
        _button.Click+=(_,_)=>_counter.Text=$"Counter: {++_count}";
    }
    private Point Center(FrameworkElement element)=>element.PointToScreen(new(element.ActualWidth/2,element.ActualHeight/2));
    public object State
    {
        get {var item=(FrameworkElement)_list.ItemContainerGenerator.ContainerFromIndex(1);
            return new{Pid=Environment.ProcessId,Handle=new WindowInteropHelper(this).Handle.ToInt64(),Count=_count,Toggle=_toggle.IsChecked,Range=_range.Value,Expanded=_expand.IsExpanded,Selected=_list.SelectedIndex,Scroll=_scroll.VerticalOffset,
                Points=new Dictionary<string,Point>{["Invoke"]=Center(_button),["Toggle"]=Center(_toggle),["RangeValue"]=Center(_range),["ExpandCollapse"]=_expand.PointToScreen(new(40,10)),["SelectionItem"]=Center(item),["Scroll"]=Center(_scroll),["SensitiveScroll"]=Center(_innerSecret),["Password"]=Center(_password),["Disabled"]=Center(_disabled)}};}
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern bool AllowSetForegroundWindow(int process);
    public async Task Serve(string name)
    {
        using var pipe=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        await pipe.WaitForConnectionAsync().WaitAsync(TimeSpan.FromSeconds(60));
        using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
        using var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true){AutoFlush=true};
        while(await reader.ReadLineAsync() is {} command)
        {
            if(command.Length>100)break;
            if(command.StartsWith("permit-focus|",StringComparison.Ordinal)&&int.TryParse(command[13..],out int permitted))AllowSetForegroundWindow(permitted);
            if(command=="reset-scroll")_scroll.ScrollToTop();
            if(command=="slow-provider")_button.Delay=true;
            if(command=="cover") { _cover=new(){Title="Demo obstruction",Left=Left,Top=Top,Width=Width,Height=Height,Content=new TextBlock{Text="Covered target must reject"},Topmost=true};_cover.Show(); }
            if(command=="uncover"){_cover?.Close();_cover=null;}
            if(command=="move")Left+=40;
            if(command=="hide")Hide();
            if(command=="show")Show();
            await writer.WriteLineAsync(JsonSerializer.Serialize(State));
            if(command=="exit"){_cover?.Close();Close();break;}
        }
    }
}
internal sealed class FaultButton:Button
{
    internal bool Delay;
    protected override AutomationPeer OnCreateAutomationPeer()=>new Peer(this);
    private sealed class Peer(FaultButton button):ButtonAutomationPeer(button),IInvokeProvider
    {
        public override object GetPattern(PatternInterface pattern)=>pattern==PatternInterface.Invoke?this:base.GetPattern(pattern);
        void IInvokeProvider.Invoke()
        {
            if(button.Delay)Thread.Sleep(10000); // Private test-owned provider fault; supervisor must kill its worker.
            button.Dispatcher.Invoke(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
        }
    }
}
internal static class Program
{
    [STAThread] public static void Main(string[] args)
    {
        var app=new Application();var window=new DemoWindow();
        if(args.Length==2&&args[0]=="--pipe")window.Loaded+=async(_,_)=>{try{await window.Serve(args[1]);}catch(Exception e){Trace.TraceWarning(e.GetType().Name);window.Close();}};
        app.Run(window);
    }
}
