using System.Windows;
using Pocket.Core;
using Pocket.Core.Tracking;
namespace Pocket.App.Tracking;

internal sealed partial class RecordedTrackingWindow
{
    private TrackingSnapshot? _learningFixture;
    private readonly Guid _learningFixtureSession = Guid.NewGuid();
    private long _learningFixtureSequence;
    internal void LearningFixture(string operation)
    {
        AcceptanceHost.ValidateDesktop();
        if (!_controller.CanStart) throw new InvalidOperationException("Stop real/recorded workers before synthetic learning fixtures.");
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
