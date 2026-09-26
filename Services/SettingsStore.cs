using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeysVisorOverlay.Services;

public enum StatsPlacement
{
    Top,
    Bottom
}

public sealed class OverlaySettings
{
    public const string DefaultPressedColor = "#7DE2D1";

    public List<WatchedInput> Inputs { get; set; } = [];

    public StatsPlacement StatsPlacement { get; set; } = StatsPlacement.Top;

    public bool TrailsEnabled { get; set; } = true;

    public bool TransparentBackground { get; set; }

    public bool BlurBackground { get; set; }

    public string PressedColor { get; set; } = DefaultPressedColor;

    public double OverlayScale { get; set; } = 1.0D;

    public double KeyWidth { get; set; } = 76.0D;

    public double KeyHeight { get; set; } = 34.0D;

    public double TrailHeight { get; set; } = 128.0D;
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeysVisorOverlay");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    public static IReadOnlyList<WatchedInput> DefaultInputs { get; } =
    [
        new(InputCodes.VkD, "D"),
        new(InputCodes.VkF, "F"),
        new(InputCodes.VkJ, "J"),
        new(InputCodes.VkK, "K")
    ];

    public static OverlaySettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            OverlaySettings? settings = JsonSerializer.Deserialize<OverlaySettings>(json, JsonOptions);
            return Normalize(settings);
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public static void Save(OverlaySettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(Normalize(settings), JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static IReadOnlyList<WatchedInput> LoadInputs()
    {
        return Load().Inputs;
    }

    public static void SaveInputs(IEnumerable<WatchedInput> inputs)
    {
        OverlaySettings settings = Load();
        settings.Inputs = Normalize(inputs).ToList();
        Save(settings);
    }

    public static OverlaySettings CreateDefaultSettings()
    {
        return new OverlaySettings
        {
            Inputs = DefaultInputs.ToList(),
            StatsPlacement = StatsPlacement.Top,
            TrailsEnabled = true,
            TransparentBackground = false,
            BlurBackground = false,
            PressedColor = OverlaySettings.DefaultPressedColor,
            OverlayScale = 1.0D,
            KeyWidth = 76.0D,
            KeyHeight = 34.0D,
            TrailHeight = 128.0D
        };
    }

    public static OverlaySettings Normalize(OverlaySettings? settings)
    {
        settings ??= CreateDefaultSettings();
        settings.Inputs = Normalize(settings.Inputs.Count == 0 ? DefaultInputs : settings.Inputs).ToList();
        settings.PressedColor = NormalizeColor(settings.PressedColor);
        settings.OverlayScale = Math.Clamp(settings.OverlayScale, 0.7D, 1.6D);
        settings.KeyWidth = Math.Clamp(settings.KeyWidth, 48.0D, 180.0D);
        settings.KeyHeight = Math.Clamp(settings.KeyHeight, 28.0D, 96.0D);
        settings.TrailHeight = Math.Clamp(settings.TrailHeight, 96.0D, 720.0D);
        return settings;
    }

    public static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return OverlaySettings.DefaultPressedColor;
        }

        string value = color.Trim();
        if (!value.StartsWith('#'))
        {
            value = $"#{value}";
        }

        if (value.Length != 7 || value.Skip(1).Any(character => !Uri.IsHexDigit(character)))
        {
            return OverlaySettings.DefaultPressedColor;
        }

        return value.ToUpperInvariant();
    }

    public static IReadOnlyList<WatchedInput> Normalize(IEnumerable<WatchedInput> inputs)
    {
        List<WatchedInput> result = [];
        HashSet<int> seen = [];

        foreach (WatchedInput input in inputs)
        {
            if (!seen.Add(input.Code))
            {
                continue;
            }

            string label = string.IsNullOrWhiteSpace(input.Label)
                ? InputCodes.GetDisplayName(input.Code)
                : input.Label.Trim();

            result.Add(new WatchedInput(input.Code, label));
        }

        return result.Count == 0 ? DefaultInputs : result;
    }
}
