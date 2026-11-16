using System.Text.Json;
using Serilog;

namespace OwlScripts.Common;

public sealed class Config
{
    private static readonly string _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oot2", "Config.json");

    public static Config Shared { get; } = FromLocalAppData();

    public string? GameDumpFolderPath { get; set; }

    private static Config FromLocalAppData()
    {
        if (File.Exists(_configFilePath)) {
            using var fs = File.OpenRead(_configFilePath);
            return JsonSerializer.Deserialize<Config>(fs) ?? new Config();
        }

        return new Config();
    }

    public static void Save()
    {
        byte[]? last = File.Exists(_configFilePath) ? File.ReadAllBytes(_configFilePath) : null;

        try {
            if (Path.GetDirectoryName(_configFilePath) is { } dir) {
                Directory.CreateDirectory(dir);
            }
            
            using var fs = File.Create(_configFilePath);
            JsonSerializer.Serialize(fs, Shared);
        }
        catch (Exception ex) {
            Log.Error(ex, "Failed to save config, rolling back...");

            if (last is not null) {
                File.WriteAllBytes(_configFilePath, last);
            }
        }
    }
}