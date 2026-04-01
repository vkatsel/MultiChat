namespace Client;

public class Logger
{
    public static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[MultiChat][INFO] {message}");
    }
    
    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[MultiChat][ERROR] {message}");
    }
    
    public static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[MultiChat][WARNING] {message}");
    }
    
    public static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[MultiChat][SUCCESS] {message}");
    }
    
    public static void LogIncomingMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[MultiChat][INCOMING] {message}");
    }
}