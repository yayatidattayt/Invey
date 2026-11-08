using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Pocket.App.References;
using Pocket.App.Tracking;

namespace Pocket.App;

/// <summary>Supplemental WPF control-route checks only. This is not native input acceptance.</summary>
internal static class DemoControlFixture
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var item in Children(child)) yield return item;
    }

    internal static void Apply(MainWindow main, string command)
    {
        AcceptanceHost.ValidateDesktop();
        var parts = command.Split('|');
        if (parts.Length != 3) throw new InvalidOperationException("Invalid demo control command.");
        Window root = parts[1] switch
        {
            "main" => main,
            "pocket" => Application.Current.Windows.OfType<PocketWorkspace>().Single(),
            "workspace" => Application.Current.Windows.OfType<DualWorkspace>().Single(),
            "tracking" => Application.Current.Windows.OfType<RecordedTrackingWindow>().Single(),
            "capture" => Application.Current.Windows.OfType<CaptureWindow>().Single(),
            "card" => Application.Current.Windows.OfType<Window>().Single(window =>
                Children(window).OfType<CheckBox>().Any(box => Text(box.Content) == "Pin above other windows")),
            "settings" => Application.Current.Windows.OfType<Window>().Single(window => window.Title == "Invey settings"),
            _ => throw new InvalidOperationException("Unsupported demo window.")
        };
        if (!root.IsLoaded || !root.IsVisible) throw new InvalidOperationException("Demo control window is not displayed.");
        if (parts[0] == "demo-ui")
        {
            var button = Children(root).OfType<Button>().Single(control => Text(control.Content) == parts[2]);
            if (!button.IsEnabled || !button.IsVisible) throw new InvalidOperationException("Demo button is unavailable.");
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
        }
        else if (parts[0] == "demo-toggle")
        {
            var box = Children(root).OfType<CheckBox>().Single(control => Text(control.Content) == parts[2]);
            if (!box.IsEnabled || !box.IsVisible) throw new InvalidOperationException("Demo toggle is unavailable.");
            ((IToggleProvider)new CheckBoxAutomationPeer(box).GetPattern(PatternInterface.Toggle)).Toggle();
        }
        else if (parts[0] == "demo-select")
        {
            var selector = Children(root).OfType<ComboBox>().Single(control =>
                AutomationProperties.GetName(control) == (parts[1] == "tracking" ? "Recorded demonstration" : "Saved references"));
            if (!selector.IsEnabled || !int.TryParse(parts[2], out int index) || index < 0 || index >= selector.Items.Count)
                throw new InvalidOperationException("Demo selection is unavailable.");
            selector.SelectedIndex = index;
        }
        else throw new InvalidOperationException("Unsupported demo control operation.");
    }

    private static string Text(object value) => (value?.ToString() ?? "").Replace("_", "");
}
