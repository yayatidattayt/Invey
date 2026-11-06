using System.Text.Json;
using Forms=System.Windows.Forms;

namespace Pocket.App;

/// <summary>Owned standard WinForms/Win32 controls for compatibility observations, private desktop only.</summary>
internal static class StandardProviderFixture
{
    public static void Show(string directory)
    {
        var form=new Forms.Form{Name="GhostFixture.Window",Text="GHOST standard provider fixture",Left=850,Top=70,Width=400,Height=400};
        int invoked=0;
        var button=new Forms.Button{Name="GhostFixture.Invoke",Text="Benign invocation",Left=20,Top=20,Width=200};
        var toggle=new Forms.CheckBox{Name="GhostFixture.Toggle",Text="Benign toggle",Left=20,Top=70,Width=200};
        var range=new Forms.HScrollBar{Name="GhostFixture.Range",Minimum=0,Maximum=100,LargeChange=1,Left=20,Top=120,Width=200};
        var scroll=new Forms.Panel{Name="GhostFixture.Scroll",Left=20,Top=180,Width=200,Height=100,AutoScroll=true};
        scroll.Controls.Add(new Forms.Label{Text="Controlled standard provider content",Width=180,Height=900});
        void Snapshot()=>File.WriteAllText(Path.Combine(directory,"state"),JsonSerializer.Serialize(new{Invocations=invoked,Toggle=toggle.Checked,Range=range.Value,ScrollOffset=-scroll.AutoScrollPosition.Y}));
        button.Click+=(_,_)=>{invoked++;Snapshot();};toggle.CheckedChanged+=(_,_)=>Snapshot();range.ValueChanged+=(_,_)=>Snapshot();scroll.Scroll+=(_,_)=>Snapshot();
        form.Controls.AddRange(new Forms.Control[]{button,toggle,range,scroll});
        form.Shown += (_, _) => { Snapshot(); AcceptanceHost.PublishReady(directory, form.Handle); };
        form.Show();
    }
}
