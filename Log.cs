namespace RememberTheMilk;

public static class Log
{
    // %LOCALAPPDATA% so the log survives redeploys and stays out of the exe folder.
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GaucheDroite", "RememberTheMilk", "app.log");

    public static void Info(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        Console.WriteLine(line);
        try { Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!); File.AppendAllText(LogFile, line + Environment.NewLine); }
        catch (IOException) { /* log file unavailable; console output still works */ }
    }
}