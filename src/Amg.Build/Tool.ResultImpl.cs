namespace Amg.Build
{
    public partial class Tool
    {
        class ResultImpl : IToolResult
        {
            public ResultImpl(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }

            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }
}