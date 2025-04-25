namespace Serilogs.ConsoleSample;

using Serilogs.Main;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var logger = SerilogHelper.GetLogger();
        logger.Information("First Message");
    }
}
