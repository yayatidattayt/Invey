using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Pocket.App;

internal enum ShortcutAction { Capture, Pocket, PauseGhost, Workspace }

internal readonly record struct ShortcutChord(uint Modifiers, uint Key)
{
    public static bool TryParse(string text, out ShortcutChord chord)
    {
        chord = default;
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length is < 3 or > 4 || !parts[0].Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            return false;
        uint modifiers = string.Join('+', parts[1..^1]).ToUpperInvariant() switch
        {
            "ALT" => 3u, "SHIFT" => 6u, "ALT+SHIFT" => 7u, _ => 0u
        };
        string key = parts[^1].ToUpperInvariant();
        uint code = key switch
        {
            "SPACE" => 0x20,
            [>= 'A' and <= 'Z' or >= '0' and <= '9'] => key[0],
            _ => 0u
        };
        if (modifiers == 0 || code == 0) return false;
        chord = new(modifiers, code);
        return true;
    }

    public override string ToString() =>
        $"Ctrl+{(Modifiers == 3 ? "Alt" : Modifiers == 6 ? "Shift" : "Alt+Shift")}+{(Key == 0x20 ? "Space" : ((char)Key).ToString())}";
}

internal sealed class Shortcuts : IDisposable
{
    private static readonly string[] Defaults = ["Ctrl+Alt+G", "Ctrl+Alt+P", "Ctrl+Alt+Space", "Ctrl+Alt+W"];
    private readonly HwndSource _window;
    private readonly string? _path;
    private readonly bool _writable;
    private readonly Action<ShortcutAction> _dispatch;
    private readonly Dictionary<ShortcutChord, int> _active = [];
    private ShortcutChord?[] _mapping = new ShortcutChord?[Defaults.Length];
    private int _nextId;
    private bool _disposed;
    public string? LoadNotice { get; private set; }

    public Shortcuts(string? path, bool writable, Action<ShortcutAction> dispatch)
    {
        _path = path;
        _writable = writable && path is not null;
        _dispatch = dispatch;
        _window = new HwndSource(new HwndSourceParameters("Invey shortcuts") { ParentWindow = new nint(-3) });
        _window.AddHook(OnMessage);
        _mapping = ReadMapping();
        foreach (var chord in _mapping.OfType<ShortcutChord>()) _ = Register(chord);
    }

    public bool CanEdit => _writable;
    public string Binding(ShortcutAction action) => _mapping[(int)action]?.ToString() ?? "";
    public string Status(ShortcutAction action) => _mapping[(int)action] is not { } chord ? "Disabled" :
        _active.ContainsKey(chord) ? "Registered" : "Unavailable. Use the tray or choose another shortcut.";

    // Retain all previous registrations until every new chord is reserved and the settings commit succeeds.
    public string? Apply(string[] entries)
    {
        if (!_writable) return "Shortcut settings are read-only.";
        if (!TryParse(entries, out var next))
            return "Use unique Ctrl+Alt, Ctrl+Shift or Ctrl+Alt+Shift combinations with a letter, digit or Space. Leave blank to disable.";
        var newlyRegistered = new List<ShortcutChord>();
        foreach (var chord in next.OfType<ShortcutChord>())
        {
            if (_active.ContainsKey(chord)) continue;
            if (Register(chord)) newlyRegistered.Add(chord);
            else
            {
                foreach (var added in newlyRegistered) Release(added);
                return $"{chord} could not be registered. Previous shortcuts were kept.";
            }
        }
        try { Save(next); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            foreach (var added in newlyRegistered) Release(added);
            return "Could not save shortcuts. Previous shortcuts were kept.";
        }
        _mapping = next;
        foreach (var chord in _active.Keys.Where(chord => !_mapping.Contains(chord)).ToArray()) Release(chord);
        LoadNotice = null;
        return null;
    }

    private ShortcutChord?[] ReadMapping()
    {
        _ = TryParse(Defaults, out var defaults);
        if (_path is null) return defaults;
        try
        {
            ValidatePath(_path);
            using var file = File.OpenRead(_path);
            if (file.Length is <= 0 or > 2048) throw new InvalidDataException("Invalid shortcut file size.");
            var entries = JsonSerializer.Deserialize<string[]>(file);
            return TryParse(entries, out var mapping) ? mapping : throw new InvalidDataException("Invalid shortcut mapping.");
        }
        catch (FileNotFoundException) { return defaults; }
        catch (DirectoryNotFoundException) { return defaults; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            LoadNotice = "Shortcut settings could not be read. Defaults were tried. The saved file was preserved.";
            return defaults;
        }
    }

    private static bool TryParse(string[]? entries, out ShortcutChord?[] mapping)
    {
        mapping = new ShortcutChord?[Defaults.Length];
        if (entries?.Length != mapping.Length) return false;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] is null || entries[i].Length > 40) return false;
            if (string.IsNullOrWhiteSpace(entries[i])) continue;
            if (!ShortcutChord.TryParse(entries[i], out var chord) || mapping.Contains(chord)) return false;
            mapping[i] = chord;
        }
        return true;
    }

    private void Save(ShortcutChord?[] mapping)
    {
        string path = _path!;
        ValidatePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string stage = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, mapping.Select(chord => chord?.ToString() ?? "").ToArray());
                stream.Flush(flushToDisk: true);
            }
            File.Move(stage, path, overwrite: true);
        }
        finally { File.Delete(stage); }
    }

    private static void ValidatePath(string path)
    {
        foreach (string candidate in new[] { path, Path.GetDirectoryName(path)! })
        {
            try
            {
                if ((File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0)
                    throw new UnauthorizedAccessException("Linked shortcut paths are unsupported.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    private bool Register(ShortcutChord chord)
    {
        // IDs remain distinct across updates. Queue entries from removed IDs cannot trigger a new action.
        if (_nextId == 0xBFFF) return false;
        int id = ++_nextId;
        if (!RegisterHotKey(_window.Handle, id, chord.Modifiers | 0x4000, chord.Key)) return false;
        _active.Add(chord, id);
        return true;
    }

    private void Release(ShortcutChord chord)
    {
        if (_active.Remove(chord, out int id) && !UnregisterHotKey(_window.Handle, id))
            System.Diagnostics.Trace.TraceWarning("Could not unregister an Invey shortcut.");
    }

    private nint OnMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != 0x0312) return 0;
        int id = wParam.ToInt32();
        handled = true;
        _ = _window.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            if (_disposed || NativeDragInProgress) return;
            for (int i = 0; i < _mapping.Length; i++)
                if (_mapping[i] is { } chord && _active.GetValueOrDefault(chord) == id)
                {
                    _dispatch((ShortcutAction)i);
                    break;
                }
        });
        return 0;
    }

    // A global shortcut must leave an ongoing native mouse gesture alone.
    internal static bool NativeDragInProgress =>
        GetAsyncKeyState(0x01) < 0 || GetAsyncKeyState(0x02) < 0 || GetAsyncKeyState(0x04) < 0 ||
        GetAsyncKeyState(0x05) < 0 || GetAsyncKeyState(0x06) < 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var chord in _active.Keys.ToArray()) Release(chord);
        _window.Dispose();
    }

    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
