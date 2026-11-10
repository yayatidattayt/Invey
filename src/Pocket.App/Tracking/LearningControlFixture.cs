using System.Windows;
using Pocket.Core;
using Pocket.Core.Tracking;
namespace Pocket.App.Tracking;

internal sealed partial class RecordedTrackingWindow
{
    private TrackingSnapshot? _learningFixture;
    private readonly Guid _learningFixtureSession = Guid.NewGuid();
    private long _learningFixtureSequence;
    internal void ConfigureBudgetPreviewFixture()
    {
        AcceptanceHost.ValidateDesktop();
        _devices.ItemsSource = new[] { new CameraDevice(new string('0', 64), "NON-CAMERA budget preview fixture") };
        _devices.SelectedIndex = 0;
        _live.IsChecked = true;
        Render();
    }
    internal void ChoosePerformanceFixture()
    {
        AcceptanceHost.ValidateDesktop();
        _performanceMode.SelectedIndex=1;Render(); // Selection only, never Enable.
    }

    internal void LearningFixture(string operation)
    {
        AcceptanceHost.ValidateDesktop();
        if (!_controller.CanStart) throw new InvalidOperationException("Stop real/recorded workers before synthetic learning fixtures.");
        if (operation == "loss")
        {
            if (_learningFixture is null) throw new InvalidOperationException("Start the synthetic source first.");
            _learningFixture = _learningFixture with { State = TrackingState.Lost, Observation = null,
                Continuity = _learningFixture.Continuity + 1, LastBreak = TrackingBreak.Loss };
            Render(); return;
        }
        if (operation == "end")
        {
            _learningFixture = null; _calibration.End(); Render(); return;
        }
        if (operation == "begin") _calibration.Begin(null);
        if (operation is not ("begin" or "observe")) throw new InvalidOperationException("Unknown fixture operation.");
        double bias = Environment.GetEnvironmentVariable("INVEY_LEARNING_SEED") == "99174" ? -.025 : .02;
        for (int i = 0; i < 8; i++)
        {
            var target = operation == "observe" ? _learning.FixtureTarget ?? new DipPoint(.5, .5) : new DipPoint(.5, .5);
            _learningFixture = _calibration.LearningFixtureObservation(_learningFixtureSession, ++_learningFixtureSequence,
                new(target.X + bias, target.Y - bias / 2));
            Render();
        }
    }
}
