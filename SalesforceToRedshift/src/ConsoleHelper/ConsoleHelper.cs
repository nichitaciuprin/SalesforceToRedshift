public static class ConsoleHelper
{
    public static void WriteLineWarning(string msg)
    {
        var rem = Console.ForegroundColor;
        WriteLine(msg,ConsoleColor.Yellow);
        Console.ForegroundColor = rem;
    }
    private static void WriteLine(string msg, ConsoleColor color)
    {
        var rem = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = rem;
    }
}