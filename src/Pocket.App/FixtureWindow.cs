using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Pocket.App;

/// <summary>A1 native test surface, not a UI Automation integration.</summary>
public sealed class FixtureWindow : Window
{
    private int _clicks,_dragStarts,_dragEnds,_dragCanceled,_textChanges;
    private bool _dragActive;
    private readonly TextBlock _count=new();
    private Button _button=null!;
    private TextBox _editor=null!;
    private Slider _slider=null!;
    public FixtureWindow()
    {
        Title="Invey A1 native fixture"; Width=680; Height=500; Left=60; Top=80;
        var panel=new StackPanel { Margin=new(24) };
        panel.Children.Add(new TextBlock { Text="Physical input fixture",FontSize=20,FontWeight=FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text="Place visible GHOST/Pocket over these controls. Counts record real routed events; no input is injected.",TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,16) });
        var button=new Button { Content="Click counter (target 100)",Height=80 };
        _button=button;
        button.Click+=(_,_)=>{_clicks++; Update();}; panel.Children.Add(button);
        var editor=new TextBox { AcceptsReturn=true,Height=100,TextWrapping=TextWrapping.Wrap,Margin=new(0,12,0,12) };
        _editor=editor;
        System.Windows.Automation.AutomationProperties.SetName(editor,"Typing continuity test; text is never logged");
        editor.TextChanged+=(_,_)=>{_textChanges++;Update();}; panel.Children.Add(editor);
        var slider=new Slider { Minimum=0,Maximum=100,Height=48 };
        _slider=slider;
        System.Windows.Automation.AutomationProperties.SetName(slider,"Physical mouse drag continuity");
        slider.AddHandler(Thumb.DragStartedEvent,new DragStartedEventHandler((_,_)=>{_dragStarts++;_dragActive=true;Update();}));
        slider.AddHandler(Thumb.DragCompletedEvent,new DragCompletedEventHandler((_,e)=>{_dragEnds++;if(e.Canceled)_dragCanceled++;_dragActive=false;Update();}));
        panel.Children.Add(slider); panel.Children.Add(_count); Content=panel; Update();
    }
    private void Update()=>_count.Text=$"Clicks: {_clicks} · Drag starts/ends: {_dragStarts}/{_dragEnds} · Canceled: {_dragCanceled} · Active: {_dragActive} · Text changes: {_textChanges}";
    public object Counters()=>new { Clicks=_clicks,DragStarts=_dragStarts,DragEnds=_dragEnds,DragCanceled=_dragCanceled,DragActive=_dragActive,TextChanges=_textChanges };
    internal void ResetTest(){_editor.Clear();_slider.Value=0;_clicks=_dragStarts=_dragEnds=_dragCanceled=_textChanges=0;_dragActive=false;Update();}
    internal object AcceptanceSnapshot()
    {
        object Center(FrameworkElement element){var p=element.PointToScreen(new Point(element.ActualWidth/2,element.ActualHeight/2));var a=element.PointToScreen(new Point(0,0));return new {X=p.X,Y=p.Y,Left=a.X,Top=a.Y,Width=(p.X-a.X)*2,Height=(p.Y-a.Y)*2};}
        var thumb=FindThumb(_slider);
        return new { Clicks=_clicks,DragStarts=_dragStarts,DragEnds=_dragEnds,DragCanceled=_dragCanceled,DragActive=_dragActive,
            TextLength=_editor.Text.Length,CaretIndex=_editor.CaretIndex,SelectionLength=_editor.SelectionLength,EditorFocused=_editor.IsKeyboardFocused,TextDigest=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(_editor.Text))),
            Button=Center(_button),Editor=Center(_editor),Thumb=thumb is null?null:Center(thumb),SliderValue=_slider.Value };
    }
    private static Thumb? FindThumb(DependencyObject root)
    {
        for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++)
        {var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);if(child is Thumb thumb)return thumb;var nested=FindThumb(child);if(nested!=null)return nested;}
        return null;
    }
}
