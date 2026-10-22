namespace RememberTheMilk;

public static class Log
{
    public static void Info(string message) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
}