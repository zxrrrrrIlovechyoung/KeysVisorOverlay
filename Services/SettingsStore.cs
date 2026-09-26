using System.IO;
using System.Text.Json;

namespace KeysVisorOverlay.Services;

public sealed record OverlaySettings(List<WatchedInput> Inputs);

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
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

    public static IReadOnlyList<WatchedInput> LoadInputs()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return DefaultInputs;
            }

            string json = File.ReadAllText(SettingsPath);
            OverlaySettings? settings = JsonSerializer.Deserialize<OverlaySettings>(json, JsonOptions);
            return Normalize(settings?.Inputs ?? DefaultInputs);
        }
        catch
        {
            return DefaultInputs;
        }
    }

    public static void SaveInputs(IEnumerable<WatchedInput> inputs)
    {
        Directory.CreateDirectory(SettingsDirectory);
        OverlaySettings settings = new(Normalize(inputs).ToList());
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
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
