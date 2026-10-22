namespace RememberTheMilk;

public static class Log
{
    private const string LogFile = "app.log";

    public static void Info(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        Console.WriteLine(line);
        try { File.AppendAllText(LogFile, line + Environment.NewLine); }
        catch (IOException) { /* log file unavailable; console output still works */ }
    }
}