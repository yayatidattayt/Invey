using System.IO.Pipes;
using System.Text;
using Pocket.Core.Tracking;
using Xunit;

namespace Pocket.Core.Tests;

public class TrackingProtocolTests
{
    private static string Header(Guid session, string kind) => "{\"v\":1,\"session\":\"" + session + "\",\"kind\":\"" + kind + "\"";
    private static string Observation(Guid id, string landmarks, string extra = "") =>
        Header(id, "observation") + $$""","seq":11,"captureMonoMs":100,"resultMonoMs":120,"tracked":true,"landmarks":{{landmarks}}{{extra}}}""";
    private static string Points(string point = "[0.25,0.75,-0.1]") =>
        "[" + string.Join(",", Enumerable.Repeat(point, 21)) + "]";
    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    [Fact]
    public async Task FragmentedNamedPipeStreamDeliversOnlyCompleteMessagesAndStopClearsHand()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var session = new TrackingSession();
        Guid id = session.Enable(TrackingSource.Simulation)!.Value;
        string name = "GhostTrackingTest-" + Guid.NewGuid();
        using var sender = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var receiver = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous);
        await Task.WhenAll(sender.WaitForConnectionAsync(deadline.Token), receiver.ConnectAsync(deadline.Token));
        var reader = new TrackingLineReader(receiver);
        string ready = Header(id, "ready") + "}\n";
        var pending = reader.ReadAsync(deadline.Token).AsTask();
        await sender.WriteAsync(Utf8(ready[..10]), deadline.Token);
        Assert.False(pending.IsCompleted);
        var write = sender.WriteAsync(Utf8(ready[10..] + Observation(id, Points(),
            ""","handedness":"right","quality":{"handednessScore":0.8}""") + "\n"), deadline.Token).AsTask();
        Assert.True(session.Accept(Assert.IsType<TrackerReady>(await pending)));
        var frame = Assert.IsType<HandObservation>(await reader.ReadAsync(deadline.Token));
        await write;
        Assert.True(session.Accept(frame));
        Assert.Equal(new ImageLandmark(.25, .75, -.1), frame.Landmarks[20]);
        Assert.Equal(HandSide.Right, frame.Handedness);
        Assert.Equal(.8, frame.HandednessScore);

        Assert.True(session.Stop());
        write = sender.WriteAsync(Utf8(Observation(id, Points()) + "\n" + Header(id, "stopped") + "}\n"), deadline.Token).AsTask();
        Assert.False(session.Accept(Assert.IsType<HandObservation>(await reader.ReadAsync(deadline.Token))));
        Assert.False(session.Accept(Assert.IsType<TrackerStopped>(await reader.ReadAsync(deadline.Token))));
        Assert.Null(session.Enable(TrackingSource.Simulation));
        await write;
        Assert.Null(session.Read().Observation);
        sender.Dispose();
        Assert.Null(await reader.ReadAsync(deadline.Token));
        Assert.True(session.ConfirmWorkerExit(id));
    }

    [Theory]
    [InlineData("[]", "")]
    [InlineData("[[0,0,0]]", "")]
    [InlineData("invalid-number", "")]
    [InlineData("outside-image", "")]
    [InlineData("wrong-dimension", "")]
    [InlineData("valid", ",\"pixels\":[0,0,0]")]
    [InlineData("valid", ",\"seq\":12")]
    [InlineData("valid", ",\"quality\":{\"handednessScore\":0.5}")]
    [InlineData("valid", ",\"partial\":true")]
    public void RejectsWrongShapeUntrustedPayloadsAndFabricatedQuality(string landmarks, string extra)
    {
        landmarks = landmarks switch
        {
            "valid" => Points(),
            "invalid-number" => Points("[0,1,1e309]"),
            "outside-image" => Points("[0,1.01,0]"),
            "wrong-dimension" => Points("[0,1]"),
            _ => landmarks
        };
        Assert.Throws<InvalidDataException>(() => TrackingProtocol.Parse(Utf8(Observation(Guid.NewGuid(), landmarks, extra))));
    }

    [Fact]
    public void PartialResultClearsPreviouslyUsableLandmarksWithoutClaimingNoHand()
    {
        var session = new TrackingSession();
        Guid id = session.Enable(TrackingSource.Camera)!.Value;
        Assert.True(session.Accept(new TrackerReady(id)));
        Assert.True(session.Accept(TrackingProtocol.Parse(Utf8(Observation(id, Points())))));
        string partial = Header(id, "observation") +
            ""","seq":12,"captureMonoMs":130,"resultMonoMs":150,"tracked":false,"landmarks":[],"partial":true}""";
        Assert.True(session.Accept(TrackingProtocol.Parse(Utf8(partial))));
        Assert.Equal(TrackingState.Partial, session.Read().State);
        Assert.Null(session.Read().Observation);
    }

    [Fact]
    public async Task RejectsOversizedUnterminatedAndInvalidUtf8Frames()
    {
        using var tooLong = new MemoryStream(new byte[TrackingProtocol.MaxLineBytes + 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => new TrackingLineReader(tooLong).ReadAsync().AsTask());
        using var partial = new MemoryStream(Utf8(Header(Guid.NewGuid(), "ready") + "}"));
        await Assert.ThrowsAsync<InvalidDataException>(() => new TrackingLineReader(partial).ReadAsync().AsTask());
        Assert.Throws<InvalidDataException>(() => TrackingProtocol.Parse(new byte[] { 0xc3, 0x28 }));
    }

    [Fact]
    public async Task CancellationOfIdlePipeReadDoesNotProduceAnObservation()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        string name = "GhostTrackingTest-" + Guid.NewGuid();
        using var sender = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var receiver = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous);
        await Task.WhenAll(sender.WaitForConnectionAsync(deadline.Token), receiver.ConnectAsync(deadline.Token));
        var reader = new TrackingLineReader(receiver);
        var pending = reader.ReadAsync(deadline.Token).AsTask();
        deadline.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }
}
