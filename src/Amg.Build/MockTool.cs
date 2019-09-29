using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// ITool implementation that only logs, but does not run the tool. For testing purposes.
    /// </summary>
    public class MockTool : ITool
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private string fileName;

        /// <summary />
        public MockTool(string filename)
        {
            this.fileName = filename;
        }

        /// <summary />
        public async Task<IToolResult> Run(params string[] args)
        {
            Logger.Information("Would run: {filename}: {args}", fileName, args);
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

        /// <summary />
        public ITool WithEnvironment(IDictionary<string, string> environmentVariables)
        {
            return this;
        }

        /// <summary />
        public ITool WithArguments(IEnumerable<string> args)
        {
            return this;
        }

        /// <summary />
        public ITool RunAs(string user, string password)
        {
            return this;
        }

        /// <summary />
        public ITool WithOnError(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler)
        {
            return this;
        }

        /// <summary />
        public ITool WithOnOutput(Func<Action<IRunning, string>, Action<IRunning, string>> getLineHandler)
        {
            return this;
        }

        /// <summary />
        public ITool WithFileName(string fileName)
        {
            this.fileName = fileName;
            return this;
        }

        /// <summary>
        /// Set the result to be returned from Run
        /// </summary>
        public Result MockResult { get; set; } = new Result();

        /// <summary />
        public class Result : IToolResult
        {
            /// <summary />
            public int ExitCode { get; set; } = 0;

            /// <summary />
            public string Output { get; set; } = string.Empty;

            /// <summary />
            public string Error { get; set; } = string.Empty;
        }
    }
}