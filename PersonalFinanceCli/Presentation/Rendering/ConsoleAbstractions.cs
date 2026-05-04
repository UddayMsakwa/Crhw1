namespace PersonalFinanceCli.Presentation.Rendering;

public interface IConsole
{
    string? ReadLine();

    void Write(string text);

    void WriteLine(string text);

    void WriteLine();

    TextWriter Out { get; }
}

public sealed class SystemConsole : IConsole
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void Write(string text)
    {
        Console.Write(text);
    }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }

    public TextWriter Out => Console.Out;
}
