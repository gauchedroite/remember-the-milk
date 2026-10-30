using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace RememberTheMilk;

public class NotificationService(string ntfyUrl, string emailTo)
{
    private readonly string _ntfyUrl = ntfyUrl;
    private readonly string _emailTo = emailTo;
    private readonly HttpClient _http = new();

    public async Task SendAsync(ScheduledTask task)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _ntfyUrl)
        {
            Content = new StringContent(task.Description, Encoding.UTF8)
        };
        var title = $"Remember the milk ({task.ScheduleType})";
        var repeat = task.ScheduleType switch
        {
            ScheduleType.Single => "once",
            ScheduleType.Daily => "daily",
            ScheduleType.Weekly when task.Weekdays is { Count: > 0 } => string.Join(", ", task.Weekdays),
            ScheduleType.Weekly => "weekly",
            _ => "monthly"
        };
        request.Headers.Add("Title", title);

        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"ntfy.sh returned {(int)response.StatusCode}.");

        try
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Outlook email requires Windows.");
            SendOutlookEmail(_emailTo, $"{task.Description} ({repeat})", task.Description);
        }
        catch (Exception ex)
        {
            Log.Info($"[notification] Outlook email failed for \"{task.Description}\": {ex.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void SendOutlookEmail(string recipient, string subject, string body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            object? outlook = null;
            object? mail = null;
            object? recipients = null;
            try
            {
                var outlookType = Type.GetTypeFromProgID("Outlook.Application")
                    ?? throw new InvalidOperationException("Classic Outlook is not installed.");
                outlook = Activator.CreateInstance(outlookType)
                    ?? throw new InvalidOperationException("Could not start Outlook.");
                dynamic item = outlook;
                mail = item.CreateItem(0);
                dynamic message = mail;
                message.To = recipient;
                message.Subject = subject;
                message.Body = body;
                recipients = message.Recipients;
                if (!((dynamic)recipients).ResolveAll())
                    throw new InvalidOperationException("Outlook could not resolve the email recipient.");
                message.Send();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (recipients is not null) Marshal.ReleaseComObject(recipients);
                if (mail is not null) Marshal.ReleaseComObject(mail);
                if (outlook is not null) Marshal.ReleaseComObject(outlook);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null)
            throw new InvalidOperationException("Outlook could not send the reminder email.", error);
    }
}
