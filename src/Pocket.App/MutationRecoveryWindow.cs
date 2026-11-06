using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Pocket.Automation.Client;
using Pocket.Automation.Contracts;

namespace Pocket.App;

/// <summary>Recovery controls for the explicitly launched private-desktop fixture.</summary>
internal sealed class MutationRecoveryWindow : Window
{
    private readonly WorkerClient _client;
    private readonly Func<long> _targetWindow;
    private readonly CancellationTokenSource _closing = new();
    private readonly TextBlock _status = new() { Text = "Ready", TextWrapping = TextWrapping.Wrap };
    private readonly Button _apply = new() { Content = "_Set fixture value to 42" };
    private readonly Button _check = new() { Content = "_Check current state", IsEnabled = false };
    private readonly Button _dismiss = new() { Content = "_Dismiss", IsEnabled = false };
    private Task _operation = Task.CompletedTask;
    private string? _unresolved;
    private int _checks;
    private bool _shuttingDown;
    private Reply? _result;

    public MutationRecoveryWindow(Func<long> targetWindow)
    {
        AcceptanceHost.ValidateDesktop();
        _targetWindow = targetWindow;
        string dotnet = Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_DOTNET") ?? throw new InvalidOperationException("Missing acceptance runtime.");
        string worker = Environment.GetEnvironmentVariable("GHOST_ACCEPTANCE_WORKER") ?? throw new InvalidOperationException("Missing acceptance worker.");
        _client = new WorkerClient(dotnet, worker, Environment.ProcessId);

        Title = "GHOST — Fixture action recovery";
        Width = 370;
        Left = 580;
        Top = 120;
        SizeToContent = SizeToContent.Height;
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        _apply.Click += async (_, _) => await ApplyAsync();
        _check.Click += async (_, _) => await CheckAsync();
        _dismiss.Click += async (_, _) => await DismissAsync();
        var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        buttons.Children.Add(_apply);
        buttons.Children.Add(_check);
        buttons.Children.Add(_dismiss);
        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Children = { new TextBlock { Text = "Controlled fixture only", Margin = new Thickness(0, 0, 0, 8) }, _status, buttons }
        };
        Closing += (_, e) =>
        {
            if (_shuttingDown) return;
            e.Cancel = true;
            Hide();
        };
    }

    public Task ApplyAsync() => RunAsync(_apply, async () =>
    {
        var probe = new Request(1, Guid.NewGuid().ToString(), "Probe", _targetWindow(), Environment.ProcessId, "GhostFixture.Range");
        var capability = await _client.RequestAsync(probe, _closing.Token);
        if (capability.Token is null)
        {
            Announce("The fixture target is unavailable.");
            return;
        }
        _result = await _client.RequestAsync(probe with
        {
            Id = Guid.NewGuid().ToString(), Operation = "Execute", Token = capability.Token, Value = 42
        }, _closing.Token);
        if (_result.Outcome == MutationOutcome.Unknown)
        {
            _unresolved = _result.Id;
            _checks = 0;
        }
        Announce(_result.Outcome switch
        {
            MutationOutcome.Applied => "The requested fixture value was verified.",
            MutationOutcome.Conflict => "The fixture changed before dispatch. Select the action again to check the new target.",
            MutationOutcome.Unknown => "Action could not be confirmed. The fixture may have completed it. GHOST will wait for your decision before another action.",
            _ => "The action was rejected before dispatch."
        });
    });

    public Task CheckAsync() => RunAsync(_check, async () =>
    {
        _result = await _client.ReconcileAsync(_unresolved!, _closing.Token);
        _checks++;
        Announce(_result.Observation switch
        {
            Observation.ExpectedObserved => "The requested value is visible now. The earlier action's outcome remains uncertain.",
            Observation.PriorStateObserved => "The earlier value is still visible. The pending action may still take effect.",
            Observation.StateConflict => "The current state differs from the expected and earlier values.",
            Observation.TargetChanged => "The target identity changed. The earlier action remains unresolved.",
            Observation.TargetUnavailable => "The original target is unavailable.",
            _ => "GHOST could not establish the current state."
        });
    });

    public Task DismissAsync() => RunAsync(_dismiss, async () =>
    {
        if (!await _client.DismissAsync(_unresolved!)) return;
        _unresolved = null;
        Announce("Uncertainty dismissed. Check the fixture before choosing another action. A new action could duplicate the earlier effect.");
    });

    private Task RunAsync(Button button, Func<Task> action)
    {
        if (!button.IsEnabled || _shuttingDown) return Task.CompletedTask;
        _apply.IsEnabled = _check.IsEnabled = _dismiss.IsEnabled = false;
        return _operation = RunCoreAsync(action);
    }

    private async Task RunCoreAsync(Func<Task> action)
    {
        try { await action(); }
        finally
        {
            _apply.IsEnabled = _unresolved is null;
            _check.IsEnabled = _unresolved is not null && _checks < 3;
            _dismiss.IsEnabled = _unresolved is not null;
        }
    }

    private void Announce(string text)
    {
        _status.Text = text;
        UIElementAutomationPeer.CreatePeerForElement(_status)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    public object Snapshot() => new { Text = _status.Text, Outcome = _result?.Outcome, Observation = _result?.Observation, CanApply = _apply.IsEnabled, CanCheck = _check.IsEnabled };

    public async Task ShutdownAsync()
    {
        _shuttingDown = true;
        _closing.Cancel();
        try { await _operation; }
        finally
        {
            try { await _client.DisposeAsync(); }
            finally { _closing.Dispose(); Close(); }
        }
    }
}
