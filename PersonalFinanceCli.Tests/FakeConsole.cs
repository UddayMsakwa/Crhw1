using PersonalFinanceCli.Presentation.Rendering;

namespace PersonalFinanceCli.Tests;

internal sealed class FakeConsole : IConsole
{
    private readonly Queue<string?> _input;
    private readonly StringWriter _writer;

    public FakeConsole(IEnumerable<string?> inputLines)
    {
        _input = new Queue<string?>(inputLines);
        _writer = new StringWriter();
    }

    public string? ReadLine()
    {
        return _input.Count > 0 ? _input.Dequeue() : null;
    }

    public void Write(string text)
    {
        _writer.Write(text);
    }

    public void WriteLine(string text)
    {
        _writer.WriteLine(text);
    }

    public void WriteLine()
    {
        _writer.WriteLine();
    }

    public TextWriter Out => _writer;

    public string Output => _writer.ToString();

    public void ClearOutput()
    {
        _writer.GetStringBuilder().Clear();
    }
}
