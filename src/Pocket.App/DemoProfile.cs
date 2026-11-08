using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pocket.Core.References;

namespace Pocket.App;

/// <summary>Explicit demo launch only. No migration or access to the normal profile.</summary>
internal static class DemoProfile
{
    internal static InveyProfile Prepare(string root)
    {
        root = Path.GetFullPath(root);
        for (var directory = new DirectoryInfo(root); directory is not null; directory = directory.Parent)
            if (directory.Exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Demo profile cannot use a linked directory.");
        Directory.CreateDirectory(root);
        var store = new PocketStore(root);
        if (!File.Exists(Path.Combine(root, "manifest.json")) && store.Load().Items.Count == 0)
        {
            foreach (var sample in new[] {
                (Guid.Parse("17000000-0000-4000-8000-000000000001"), "DEMO · Geometric reference", "Circle  /  square  /  alignment", false),
                (Guid.Parse("17000000-0000-4000-8000-000000000002"), "DEMO · Presentation checklist", "Capture → Pin → Compare → Tracking", true) })
            {
                var drawing = new DrawingVisual();
                using (var dc = drawing.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.WhiteSmoke, null, new Rect(0, 0, 800, 500));
                    void Label(string text, double y, double size) => dc.DrawText(new FormattedText(text,
                        CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), size,
                        Brushes.Black, 1), new Point(36, y));
                    Label(sample.Item2, 28, 28);
                    Label("Synthetic sample · no desktop pixels", 76, 18);
                    Label(sample.Item3, 420, 22);
                    if (sample.Item4)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            dc.DrawRectangle(null, new Pen(Brushes.SlateGray, 2), new Rect(40, 145 + i * 75, 24, 24));
                            Label(new[] { "Keep a useful reference", "Compare two independent targets", "Use recorded Ghost when remote" }[i], 138 + i * 75, 24);
                        }
                    }
                    else
                    {
                        dc.DrawEllipse(Brushes.SlateGray, null, new Point(205, 245), 100, 100);
                        dc.DrawRectangle(Brushes.SteelBlue, null, new Rect(425, 145, 200, 200));
                    }
                }
                var bitmap = new RenderTargetBitmap(800, 500, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawing);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var bytes = new MemoryStream();
                encoder.Save(bytes);
                store.Save(sample.Item1, bytes.ToArray());
                store.Rename(sample.Item1, sample.Item2);
            }
        }
        return new(root, "DEMO PROFILE · Synthetic samples. Your normal Invey library is separate.");
    }
}
