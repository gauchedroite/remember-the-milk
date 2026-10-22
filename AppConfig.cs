using System.Text.Json;

namespace RememberTheMilk;

public class AppConfig
{
    public string NtfyUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public int Port { get; set; } = 8080;

    public static AppConfig Load(string path = "config.json")
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}. Copy config.json to the working directory.");
        var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Could not parse {path}.");
        if (string.IsNullOrEmpty(config.NtfyUrl) || string.IsNullOrEmpty(config.Model))
            throw new InvalidOperationException($"{path} must set both NtfyUrl and Model.");
        return config;
    }
}
