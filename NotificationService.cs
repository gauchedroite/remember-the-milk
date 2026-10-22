using System.Text;

namespace RememberTheMilk;

public class NotificationService
{
    private const string NtfyUrl = "https://ntfy.sh/ctrep-gauchedroite-9341";
    private readonly HttpClient _http = new();

    public async Task SendAsync(ScheduledTask task)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, NtfyUrl)
        {
            Content = new StringContent($"Reminder: {task.Description}", Encoding.UTF8)
        };
        request.Headers.Add("Title", task.NtfyTitle ?? "Reminder");

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"ntfy.sh returned {(int)response.StatusCode}.");
    }
}
