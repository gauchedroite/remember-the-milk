using System.Text;

namespace RememberTheMilk;

public class NotificationService(string ntfyUrl)
{
    private readonly string _ntfyUrl = ntfyUrl;
    private readonly HttpClient _http = new();

    public async Task SendAsync(ScheduledTask task)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _ntfyUrl)
        {
            Content = new StringContent(task.Description, Encoding.UTF8)
        };
        request.Headers.Add("Title", task.NtfyTitle ?? "Reminder");

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"ntfy.sh returned {(int)response.StatusCode}.");
    }
}
