using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Pocket.Core.Tracking;

public readonly record struct ImageLandmark(double X, double Y, double Z);
public readonly record struct TrackingFrameSize(int Width, int Height)
{
    public bool IsValid => Width is > 0 and <= 4096 && Height is > 0 and <= 4096;
}
public enum HandSide { Left, Right }
public enum TrackingFailure
{
    NoDevice, PermissionDenied, DeviceBusy, DeviceLost, UnsupportedFormat,
    InvalidModel, RuntimeFailure, ProtocolFailure, Timeout, WorkerExited,
    DeviceActivationFailed, SourceReaderFailed, NoFrames, StreamInterrupted, FrameConversionFailed
}

public abstract record TrackerMessage(Guid SessionId);
public sealed record TrackerReady(Guid SessionId) : TrackerMessage(SessionId);
public sealed record TrackerHealth(Guid SessionId) : TrackerMessage(SessionId);
public sealed record TrackerStopped(Guid SessionId) : TrackerMessage(SessionId);
public sealed record TrackerError(Guid SessionId, TrackingFailure Failure) : TrackerMessage(SessionId);

/// <summary>Single selected hand in normalized image coordinates. Empty landmarks mean tracking loss.</summary>
public sealed record HandObservation(Guid SessionId, long Sequence, long CaptureMonoMs, long ResultMonoMs,
    ImmutableArray<ImageLandmark> Landmarks, HandSide? Handedness = null, double? HandednessScore = null,
    bool Partial = false, TrackingFrameSize? FrameSize = null, bool Ambiguous = false)
    : TrackerMessage(SessionId)
{
    public bool Tracked => !Landmarks.IsDefault && Landmarks.Length == 21;

    public bool IsValid =>
        SessionId != Guid.Empty && Sequence >= 0 && CaptureMonoMs >= 0 && ResultMonoMs >= CaptureMonoMs &&
        !Landmarks.IsDefault && (Landmarks.IsEmpty || Tracked) &&
        (!(Partial || Ambiguous) || Landmarks.IsEmpty) && !(Partial && Ambiguous) &&
        (FrameSize is null || FrameSize.Value.IsValid) &&
        Landmarks.All(p => p.X is >= 0 and <= 1 && p.Y is >= 0 and <= 1 && double.IsFinite(p.Z)) &&
        (Handedness is null || (Tracked && Enum.IsDefined(Handedness.Value))) &&
        (HandednessScore is null || (Handedness.HasValue && HandednessScore is >= 0 and <= 1));
}

/// <summary>Decodes the landmark-only v1 worker stream. Fields outside the schema are rejected.</summary>
public static class TrackingProtocol
{
    public const int MaxLineBytes = 16 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static TrackerMessage Parse(ReadOnlyMemory<byte> utf8)
    {
        if (utf8.Length is 0 or > MaxLineBytes)
            throw new InvalidDataException("Tracker message is outside the byte limit.");

        try
        {
            _ = Utf8.GetCharCount(utf8.Span);
            using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 6 });
            var fields = ReadFields(document.RootElement);
            if (fields["v"].GetInt32() != 1 || !fields["session"].TryGetGuid(out var session) || session == Guid.Empty)
                throw new InvalidDataException("Invalid tracker version or session.");

            return fields["kind"].GetString() switch
            {
                "ready" when fields.Count == 3 => new TrackerReady(session),
                "health" when fields.Count == 3 => new TrackerHealth(session),
                "stopped" when fields.Count == 3 => new TrackerStopped(session),
                "error" when fields.Count == 4 => new TrackerError(session, ReadFailure(fields["code"].GetString())),
                "observation" => ReadObservation(session, fields),
                _ => throw new InvalidDataException("Invalid tracker message kind or fields.")
            };
        }
        catch (Exception ex) when (ex is JsonException or DecoderFallbackException or InvalidOperationException or
                                   KeyNotFoundException or FormatException or OverflowException)
        {
            // A raw JSON exception can contain source text. Keep diagnostics free of payload data.
            throw new InvalidDataException("Malformed tracker message.");
        }
    }

    private static Dictionary<string, JsonElement> ReadFields(JsonElement element)
    {
        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!fields.TryAdd(property.Name, property.Value))
                throw new InvalidDataException("Duplicate tracker field.");
        return fields;
    }

    private static HandObservation ReadObservation(Guid session, Dictionary<string, JsonElement> fields)
    {
        if (fields.Keys.Any(name => name is not ("v" or "kind" or "session" or "seq" or "captureMonoMs" or
            "resultMonoMs" or "tracked" or "landmarks" or "handedness" or "quality" or "partial" or "ambiguous" or "frame")))
            throw new InvalidDataException("Unknown observation field.");

        bool tracked = fields["tracked"].GetBoolean();
        bool partial = fields.TryGetValue("partial", out var partialField) && partialField.GetBoolean();
        bool ambiguous = fields.TryGetValue("ambiguous", out var ambiguousField) && ambiguousField.GetBoolean();
        var points = fields["landmarks"];
        if (points.GetArrayLength() != (tracked ? 21 : 0))
            throw new InvalidDataException("Wrong landmark count.");
        var landmarks = ImmutableArray.CreateBuilder<ImageLandmark>(points.GetArrayLength());
        foreach (var point in points.EnumerateArray())
        {
            if (point.GetArrayLength() != 3) throw new InvalidDataException("Wrong coordinate count.");
            landmarks.Add(new(point[0].GetDouble(), point[1].GetDouble(), point[2].GetDouble()));
        }

        HandSide? side = fields.TryGetValue("handedness", out var handedness) ? handedness.GetString() switch
        {
            "left" => HandSide.Left,
            "right" => HandSide.Right,
            _ => throw new InvalidDataException("Invalid handedness.")
        } : null;
        double? score = null;
        if (fields.TryGetValue("quality", out var quality))
        {
            var qualityFields = ReadFields(quality);
            if (qualityFields.Count != 1) throw new InvalidDataException("Unknown quality field.");
            score = qualityFields["handednessScore"].GetDouble();
        }

        TrackingFrameSize? size = null;
        if (fields.TryGetValue("frame", out var frame))
        {
            var dimensions = ReadFields(frame);
            if (dimensions.Count != 2) throw new InvalidDataException("Invalid frame dimensions.");
            size = new(dimensions["width"].GetInt32(), dimensions["height"].GetInt32());
        }
        var observation = new HandObservation(session, fields["seq"].GetInt64(), fields["captureMonoMs"].GetInt64(),
            fields["resultMonoMs"].GetInt64(), landmarks.MoveToImmutable(), side, score, partial, size, ambiguous);
        if (!observation.IsValid) throw new InvalidDataException("Invalid hand observation.");
        return observation;
    }

    private static TrackingFailure ReadFailure(string? code) => code switch
    {
        "no-device" => TrackingFailure.NoDevice,
        "permission-denied" => TrackingFailure.PermissionDenied,
        "device-busy" => TrackingFailure.DeviceBusy,
        "device-lost" => TrackingFailure.DeviceLost,
        "unsupported-format" => TrackingFailure.UnsupportedFormat,
        "device-activation-failed" => TrackingFailure.DeviceActivationFailed,
        "source-reader-failed" => TrackingFailure.SourceReaderFailed,
        "no-frames" => TrackingFailure.NoFrames,
        "stream-interrupted" => TrackingFailure.StreamInterrupted,
        "frame-conversion-failed" => TrackingFailure.FrameConversionFailed,
        "invalid-model" => TrackingFailure.InvalidModel,
        "runtime-failure" => TrackingFailure.RuntimeFailure,
        _ => throw new InvalidDataException("Unknown tracker error code.")
    };
}

/// <summary>One asynchronous reader per connection. Discard the connection after a framing error or cancellation.</summary>
public sealed class TrackingLineReader(Stream stream)
{
    private readonly byte[] _line = new byte[TrackingProtocol.MaxLineBytes];
    private readonly byte[] _readBuffer = new byte[1024];
    private int _offset;
    private int _available;

    public async ValueTask<TrackerMessage?> ReadAsync(CancellationToken cancellationToken = default)
    {
        int length = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset == _available)
            {
                _available = await stream.ReadAsync(_readBuffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                _offset = 0;
                if (_available == 0)
                {
                    if (length != 0) throw new InvalidDataException("Truncated tracker message.");
                    return null;
                }
            }
            byte next = _readBuffer[_offset++];
            if (next == (byte)'\n') return TrackingProtocol.Parse(_line.AsMemory(0, length));
            if (length == _line.Length) throw new InvalidDataException("Tracker message exceeded the byte limit.");
            _line[length++] = next;
        }
    }
}
