namespace Server;

public class Logger
{
    public static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[SERVER][INFO] {message}");
        Console.ResetColor();
    }
    
    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[SERVER][ERROR] {message}");
        Console.ResetColor();
    }

    public static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[SERVER][WARNING] {message}");
        Console.ResetColor();
    }

    public static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SERVER][SUCCESS] {message}");
        Console.ResetColor();
    }

}