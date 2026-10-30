using System.Text.Json;

namespace RememberTheMilk;

public class AppConfig
{
    public string NtfyUrl { get; set; } = "";
    public string EmailTo { get; set; } = "christian-9341@proton.me";
    public string[] Models { get; set; } = [];
    public string Model { get; set; } = ""; // Legacy single-model config.
    public int Port { get; set; } = 8080;

    public static AppConfig Load(string path = "config.json")
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}. Copy config.json to the working directory.");
        var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Could not parse {path}.");
        var models = config.Models ?? [];
        if (models.Length == 0 && !string.IsNullOrWhiteSpace(config.Model))
            models = [config.Model];
        if (string.IsNullOrWhiteSpace(config.NtfyUrl) || string.IsNullOrWhiteSpace(config.EmailTo) || models.Length == 0 || models.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{path} must set NtfyUrl, EmailTo, and a non-empty Models list.");
        config.Models = models;
        return config;
    }
}
