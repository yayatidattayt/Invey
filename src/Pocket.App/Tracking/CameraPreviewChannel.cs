using System.Buffers.Binary;
using System.IO.Pipes;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pocket.App.Tracking;

internal sealed record CameraPreviewFrame(long Sequence, BitmapSource Image, long ReceivedMs);

/// <summary>A separate local-only bounded binary channel. Frames never enter protocol logs or files.</summary>
internal sealed class CameraPreviewChannel : IAsyncDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly CancellationTokenSource _stop = new();
    private Task _read = Task.CompletedTask;
    private sealed record FramePair(CameraPreviewFrame Current, CameraPreviewFrame? Previous);
    private FramePair? _frames;
    public string Name { get; } = "InveyPreview-" + Guid.NewGuid().ToString("N");
    public CameraPreviewFrame? Latest => Volatile.Read(ref _frames)?.Current;
    public CameraPreviewFrame? Matching(long sequence)
    {
        var frames = Volatile.Read(ref _frames);
        return frames?.Current.Sequence == sequence ? frames.Current :
            frames?.Previous?.Sequence == sequence ? frames.Previous : null;
    }
    private volatile bool _failed;
    public bool Failed => _failed;

    public CameraPreviewChannel()
    {
        _pipe = new NamedPipeServerStream(Name, PipeDirection.In, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 64 * 1024, 0);
    }

    public void Start(int workerPid) => _read = ReadAsync(workerPid);

    private async Task ReadAsync(int workerPid)
    {
        try
        {
            await _pipe.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
            if (!RecordedWorkerJob.IsPeer(_pipe, workerPid)) throw new InvalidDataException("Unexpected preview sender.");
            var header = new byte[24];
            long previous = -1;
            while (!_stop.IsCancellationRequested)
            {
                await _pipe.ReadExactlyAsync(header, _stop.Token).ConfigureAwait(false);
                var (sequence, width, height, length) = ParseHeader(header, previous);
                var pixels = new byte[length];
                await _pipe.ReadExactlyAsync(pixels, _stop.Token).ConfigureAwait(false);
                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, pixels, width * 3);
                bitmap.Freeze();
                previous = sequence;
                var frame = new CameraPreviewFrame(sequence, bitmap, Environment.TickCount64);
                Volatile.Write(ref _frames, new(frame, Volatile.Read(ref _frames)?.Current));
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (_stop.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (!_stop.IsCancellationRequested) _failed = true;
        }
        finally { Volatile.Write(ref _frames, null); }
    }

    internal static (long Sequence, int Width, int Height, int Length) ParseHeader(ReadOnlySpan<byte> header, long previous)
    {
        if (header.Length != 24) throw new InvalidDataException("Invalid preview header length.");
        int magic = BinaryPrimitives.ReadInt32LittleEndian(header);
        long sequence = BinaryPrimitives.ReadInt64LittleEndian(header[4..]);
        int width = BinaryPrimitives.ReadInt32LittleEndian(header[12..]);
        int height = BinaryPrimitives.ReadInt32LittleEndian(header[16..]);
        int length = BinaryPrimitives.ReadInt32LittleEndian(header[20..]);
        if (magic != 0x49565031 || sequence <= previous || width is <= 0 or > 640 || height is <= 0 or > 360 ||
            length != width * height * 3) throw new InvalidDataException("Invalid preview frame header.");
        return (sequence, width, height, length);
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _pipe.Dispose();
        await _read.ConfigureAwait(false);
        _stop.Dispose();
        Volatile.Write(ref _frames, null);
    }
}
