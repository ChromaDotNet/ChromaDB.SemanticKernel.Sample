namespace ChromaDB.SemanticKernel.Sample.Tests;

/// <summary>
/// The output of a sample: records each line and passes it on to the output of the test.
/// </summary>
public sealed class RecordingOutput(ITestOutputHelper output) : ITestOutputHelper
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void WriteLine(string message)
    {
        _lines.Add(message);
        output.WriteLine(message);
    }

    public void WriteLine(string format, params object[] args) => WriteLine(string.Format(format, args));
}
