namespace Server;

public class Logger
{
    public static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[SERVER][INFO] {message}");
    }
    
    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[SERVER][ERROR] {message}");
    }
}