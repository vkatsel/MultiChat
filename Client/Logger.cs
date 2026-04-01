namespace Client;

public class Logger
{
    public static void LogInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"[MultiChat][INFO] {message}");
        Console.ResetColor();
    }
    
    public static void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[MultiChat][ERROR] {message}");
        Console.ResetColor();
    }
    
    public static void LogWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[MultiChat][WARNING] {message}");
        Console.ResetColor();
    }
    
    public static void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[MultiChat][SUCCESS] {message}");
        Console.ResetColor();
    }
    
    public static void LogIncomingMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[MultiChat][INCOMING] {message}");
        Console.ResetColor();
    }
}