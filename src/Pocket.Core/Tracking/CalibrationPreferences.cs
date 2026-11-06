using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pocket.Core.Tracking;

public sealed record CalibrationPreferences(
    [property: JsonRequired] int Version, [property: JsonRequired] CalibrationDisplay Display,
    [property: JsonRequired] int Mirror, [property: JsonRequired] int Hand,
    [property: JsonRequired] int InsetPercent, [property: JsonRequired] int SensitivityPercent)
{
    [JsonIgnore]
    public bool IsValid => Version == 1 && Display.IsValid && Mirror is 0 or 1 && Hand is >= 0 and <= 2 &&
        InsetPercent is >= 5 and <= 30 && SensitivityPercent is >= 50 and <= 200;
}

/// <summary>Only calibration numbers. An atomic same-directory replacement leaves the old file on failure.</summary>
public sealed class CalibrationPreferencesStore(string path)
{
    private const int Limit = 4096;
    private static readonly JsonSerializerOptions Schema = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false
    };

    public (CalibrationPreferences? Preferences, bool Unreadable) Load()
    {
        try
        {
            using var file = File.OpenRead(path);
            if (file.Length is <= 0 or > Limit) return (null, true);
            var bytes = new byte[(int)file.Length];
            file.ReadExactly(bytes);
            var value = JsonSerializer.Deserialize<CalibrationPreferences>(bytes, Schema);
            return value is { IsValid: true } ? (value, false) : (null, true);
        }
        catch (FileNotFoundException) { return (null, false); }
        catch (DirectoryNotFoundException) { return (null, false); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return (null, true); }
    }

    public void Save(CalibrationPreferences value)
    {
        if (!value.IsValid) throw new ArgumentException("Invalid calibration settings.", nameof(value));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, value, Schema);
                file.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }
}
