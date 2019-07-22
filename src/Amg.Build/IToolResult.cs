namespace Amg.Build
{
    public interface IToolResult
    {
        int ExitCode { get; }
        string Output { get; }
        string Error { get; }
    }
}