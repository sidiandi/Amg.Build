using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Amg.Build
{
    /// <summary>
    /// To be thrown by ITool.Run when a call to a tool fails.
    /// </summary>
    /// This is most often thrown when the exit code was not as expected.
    [Serializable]
    public class ToolException : Exception
    {
        /// <summary>
        /// Result of the tool run
        /// </summary>
        public IToolResult Result { get; private set; }
        /// <summary>
        /// information that was used to start the process.
        /// </summary>
        public ProcessStartInfo StartInfo { get; }

        /// <summary />
        public ToolException(string message, IToolResult result, ProcessStartInfo startInfo)
            : base(message)
        {
            this.Result = result;
            StartInfo = startInfo;
        }

        /// <summary />
        protected ToolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Result = null!;
            StartInfo = null!;
        }

        /// <summary />
        public override string Message => $@"{base.Message}
{new
        {
            this.StartInfo.FileName,
            this.StartInfo.Arguments,
            Result.ExitCode,
            Error = Result.Error.ReduceLines(16, 4)
        }.Dump()}";

        /// <summary />
        public string DiagnosticMessage => $@"{base.Message}
{new
        {
            this.StartInfo.FileName,
            this.StartInfo.Arguments,
            Result.ExitCode,
            Error = Result.Error.ReduceLines(200, 20),
            Output = Result.Output.ReduceLines(200, 20),
            StartInfo.WorkingDirectory,
            Environment = StartInfo.EnvironmentVariables.Cast<System.Collections.DictionaryEntry>()
                .Select(_ => $"set {_.Key}={_.Value}").Join(),
        }.Dump()}";
    }
}
