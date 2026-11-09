using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;

namespace Pocket.App;

internal sealed class FeedbackWindow : Window
{
    private readonly Func<string> _diagnostics;
    private readonly ComboBox _category = new() { MinWidth = 330 };
    private readonly TextBox _description = Field("What happened?", 3);
    private readonly TextBox _steps = Field("What did you do just before it happened?", 4);
    private readonly TextBox _expected = Field("What did you expect?", 2);
    private readonly TextBox _actual = Field("What happened instead?", 2);
    private readonly CheckBox _includeDiagnostics = new() { Content = "Include a small local performance and Ghost-state summary" };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 0) };

    public FeedbackWindow(Func<string> diagnostics)
    {
        _diagnostics = diagnostics;
        Title = "Invey · Report a problem";
        Width = 500;
        Height = 650;
        MinWidth = 380;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var body = new StackPanel { Margin = new(20) };
        body.Children.Add(new TextBlock { Text = "Tell us what went wrong", FontSize = 20, FontWeight = FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text = "Nothing is sent or saved automatically. Copying creates a text report you can choose to share.", TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 10) });
        foreach (var label in new[] { "Ghost is shaky", "Ghost feels delayed", "Ghost points the wrong way", "Ghost disappears", "Ghost misses a card", "Ghost grabs the wrong card", "Ghost cannot release", "Pocket shelf did not open", "Mouse or keyboard focus changed", "Card position did not persist", "App froze or closed", "Other" }) _category.Items.Add(label);
        _category.SelectedIndex = 0;
        Add(body, "Issue", _category);
        Add(body, "Description", _description);
        Add(body, "Steps to reproduce", _steps);
        Add(body, "Expected", _expected);
        Add(body, "Actual", _actual);
        body.Children.Add(_includeDiagnostics);
        body.Children.Add(new TextBlock { Text = "The optional summary contains only mode, source and timing counters. It excludes images, camera frames, screen content, target names, desktop coordinates and personal identifiers.", TextWrapping = TextWrapping.Wrap, Margin = new(0, 4, 0, 8) });
        var copy = new Button { Content = "Copy feedback report", HorizontalAlignment = HorizontalAlignment.Left, Padding = new(10, 6, 10, 6) };
        copy.Click += (_, _) => Copy();
        body.Children.Add(copy);
        body.Children.Add(_status);
        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = body };
        AutomationProperties.SetName(_category, "Feedback issue category");
    }

    private static TextBox Field(string watermark, int rows)
    {
        var field=new TextBox { MinHeight=rows*24,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
        AutomationProperties.SetName(field,watermark);
        return field;
    }

    private static void Add(Panel panel, string label, Control field)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new(0, 4, 0, 2), FontWeight = FontWeights.SemiBold });
        panel.Children.Add(field);
    }

    private void Copy()
    {
        try
        {
            var report = $"Invey Beta feedback\nIssue: {_category.SelectedItem}\nDescription: {_description.Text.Trim()}\nSteps: {_steps.Text.Trim()}\nExpected: {_expected.Text.Trim()}\nActual: {_actual.Text.Trim()}";
            if (_includeDiagnostics.IsChecked == true) report += "\n\n" + _diagnostics();
            Clipboard.SetText(report);
            _status.Text = "Copied. Review the text before sharing it.";
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            _status.Text = "Could not access the clipboard. Your entries are still here; try again.";
        }
    }
}
