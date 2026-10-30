using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RememberTheMilk;

public class TaskParser
{
    private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private readonly string[] _models;

    public TaskParser(string[] models)
    {
        _models = models;
    }

    private static readonly string SystemPrompt = """
        You convert natural-language reminder requests into JSON. Output ONLY a JSON object, no markdown, no explanation.

        Schema:
        {"Description": "string", "ScheduleType": "Single" | "Daily" | "Weekly" | "Monthly", "Weekdays": ["Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday" | "Sunday"] | null, "NextOccurrence": "YYYY-MM-DDTHH:MM:SS" | null, "NtfyTitle": "string" | null}

        Rules:
        - "Description" is the reminder text with time/recurrence words removed (e.g. "remind me to check out the yukon lightning tomorrow at 2" -> "check out the yukon lightning").
        - "NtfyTitle" is a short 2-5 word title, or null if not clearly implied.
        - "NextOccurrence" uses the current local time: {NOW}.
        - If a date is given but no time of day, use 09:00:00.
        - If no date or time can be determined from the request at all, set "NextOccurrence" to null. Do not guess a date.
        - "ScheduleType" must be exactly one of: Single, Daily, Weekly, Monthly (capitalized). Use "Single" for one-off events ("tomorrow at 2", "next week", "in 30 minutes"), "Daily"/"Weekly"/"Monthly" for recurring ("every day", "every monday", "monthly").
        - "Weekdays": set only for Weekly tasks with specific days, e.g. "every monday and thursday" -> ["Monday", "Thursday"]. "weekdays"/"every weekday" -> ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]. "weekends" -> ["Saturday", "Sunday"]. Otherwise null.
        """;

    private readonly HttpClient _http = new();

    public async Task<ScheduledTask?> ParseQueryAsync(string query)
    {
        // Key lives in openrouter.key next to the app or in the project root; env var as fallback.
        var keyFile = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory }
            .Select(d => Path.Combine(d, "openrouter.key"))
            .FirstOrDefault(File.Exists);
        var apiKey = keyFile is not null ? File.ReadAllText(keyFile).Trim() : Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException($"Put your OpenRouter key in {keyFile} (or set OPENROUTER_API_KEY).");

        var now = DateTime.Now;
        var prompt = SystemPrompt.Replace("{NOW}", now.ToString("yyyy-MM-dd HH:mm:ss dddd"));

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = _models[0],
            ["messages"] = new object[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content = query }
            }
        };
        if (_models.Length > 1)
            requestBody["models"] = _models[1..];
        var payload = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenRouter returned {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidOperationException("Empty LLM response.");

        // Models sometimes wrap JSON in code fences; find the outermost braces.
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException($"LLM returned no JSON: {content}");

        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
        };
        ScheduledTask? task;
        try
        {
            task = JsonSerializer.Deserialize<ScheduledTask>(content[start..(end + 1)], jsonOpts);
        }
        catch (JsonException ex)
        {
            // Include the raw LLM output so a nonconforming value is diagnosable.
            throw new InvalidOperationException($"LLM returned invalid task JSON ({ex.Message}): {content}");
        }
        if (task is null)
            throw new InvalidOperationException($"LLM returned null task: {content}");

        task.RawQuery = query;
        return task;
    }
}
