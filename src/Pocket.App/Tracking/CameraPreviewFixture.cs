using System.Buffers.Binary;

namespace Pocket.App.Tracking;

/// <summary>Private-desktop acceptance only. No camera or image persistence.</summary>
internal static class CameraPreviewFixture
{
    internal static async Task<object> ExerciseAsync()
    {
        AcceptanceHost.ValidateDesktop();
        var header = new byte[24];
        void Set(int magic = 0x49565031, long sequence = 1, int width = 640, int height = 360, int length = 691200)
        {
            BinaryPrimitives.WriteInt32LittleEndian(header, magic);
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(4), sequence);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), width);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), height);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(20), length);
        }
        Set();
        if (CameraPreviewChannel.ParseHeader(header, 0).Length != 691200)
            throw new InvalidDataException("Valid preview header rejected.");
        int rejected = 0;
        void Reject()
        {
            try { CameraPreviewChannel.ParseHeader(header, 0); }
            catch (InvalidDataException) { rejected++; return; }
            throw new InvalidDataException("Malformed preview header accepted.");
        }
        Set(magic: 0); Reject();
        Set(sequence: 0); Reject();
        Set(sequence: -1); Reject();
        Set(width: 641); Reject();
        Set(height: 361); Reject();
        Set(width: 0); Reject();
        Set(length: int.MaxValue); Reject();
        var waiting = new CameraPreviewChannel();
        waiting.Start(Environment.ProcessId);
        await waiting.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        if (waiting.Latest is not null) throw new InvalidOperationException("Stop retained a preview frame.");
        await using var channel = new CameraPreviewChannel();
        channel.Start(Environment.ProcessId);
        await using var client = new System.IO.Pipes.NamedPipeClientStream(".", channel.Name,
            System.IO.Pipes.PipeDirection.Out, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
        await client.ConnectAsync(2000);
        for (long sequence = 1; sequence <= 3; sequence++)
        {
            Set(sequence: sequence, width: 2, height: 2, length: 12);
            await client.WriteAsync(header);
            await client.WriteAsync(new byte[12]);
            await client.FlushAsync();
            var deadline = Environment.TickCount64 + 2000;
            while (channel.Latest?.Sequence != sequence && Environment.TickCount64 < deadline) await Task.Delay(5);
            if (channel.Latest?.Sequence != sequence) throw new InvalidDataException("Preview frame did not arrive.");
        }
        if (channel.Matching(1) is not null || channel.Matching(2)?.Sequence != 2 || channel.Matching(3)?.Sequence != 3)
            throw new InvalidDataException("Bounded preview frame correlation failed.");
        var view=new CameraPreviewView();
        var window=new System.Windows.Window{Content=view,Width=360,Height=300,ShowActivated=false};
        try
        {
            window.Show();view.Update(channel.Latest,null,false);window.UpdateLayout();
            var visible=System.Text.Json.JsonSerializer.SerializeToElement(view.TestState()).GetProperty("FrameVisible").GetBoolean();
            long updates=view.UpdateCount;
            for(int i=0;i<100;i++)view.Update(channel.Latest,null,false);
            if(!visible||view.UpdateCount!=updates)throw new InvalidOperationException("Unchanged camera image polls caused redraw or lost the WPF image.");
            view.Update(null,null,false);window.UpdateLayout();
            if(System.Text.Json.JsonSerializer.SerializeToElement(view.TestState()).GetProperty("FrameVisible").GetBoolean())throw new InvalidOperationException("Stop retained the camera image.");
        }
        finally {window.Close();}
        return new { RejectedHeaders = rejected, UnconnectedStop = true, BoundedFrameMatch = true,RepeatedImagePollsDeduplicated=true };
    }
}
