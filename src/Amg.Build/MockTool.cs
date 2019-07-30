using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// ITool implementation that only logs, but does not run the tool. For testing purposes.
    /// </summary>
    internal class MockTool : ITool
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string filename;

        public MockTool(string filename)
        {
            this.filename = filename;
        }

        public async Task<IToolResult> Run(params string[] args)
        {
            Logger.Information("Would run: {filename}: {args}", filename, args);
            await Task.CompletedTask;
            return MockResult;
        }

        /// <summary />
        public ITool WithArguments(params string[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <summary />
        public ITool WithWorkingDirectory(string workingDirectory)
        {
            throw new System.NotImplementedException();
        }

        /// <summary />
        public ITool WithExitCode(int expectedExitCode)
        {
            throw new System.NotImplementedException();
        }

        /// <summary />
        public ITool DoNotCheckExitCode()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Set the result to be returned from Run
        /// </summary>
        public Result MockResult { get; set; } = new Result();

        public class Result : IToolResult
        {
            public int ExitCode { get; set; } = 0;

            public string Output { get; set; } = string.Empty;

            public string Error { get; set; } = string.Empty;
        }
    }
}