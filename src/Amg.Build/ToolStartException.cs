using Amg.Extensions;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Amg.Build
{
    /// <summary>
    /// To be thrown by ITool.Run when the process cannot be started.
    /// </summary>
    [Serializable]
    public class ToolStartException : Exception
    {
        /// <summary>
        /// information that was used to start the process.
        /// </summary>
        public ProcessStartInfo StartInfo { get; }

        /// <summary />
        public ToolStartException(string message, ProcessStartInfo startInfo)
            : base(message)
        {
            StartInfo = startInfo;
        }

        /// <summary />
        protected ToolStartException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            StartInfo = null!;
        }

        static string GetPath(ProcessStartInfo startInfo)
        {
            return startInfo.EnvironmentVariables["PATH"]
                .Split(';')
                .Join();
        }

        /// <summary />
        public override string Message => $@"{base.Message}
{new
        {
            this.StartInfo.FileName,
            this.StartInfo.Arguments,
            StartInfo.WorkingDirectory,
            StartInfo.UserName,
            Path = GetPath(StartInfo),
            Environment = StartInfo.EnvironmentVariables.Cast<System.Collections.DictionaryEntry>()
                .Select(_ => $"set {_.Key}={_.Value}").Join(),
        }.Destructure()}";

        /// <summary />
        public string DiagnosticMessage => $@"{base.Message}
{new
        {
            this.StartInfo.FileName,
            this.StartInfo.Arguments,
            StartInfo.UserName,
            Path = GetPath(StartInfo),
            StartInfo.WorkingDirectory,
            Environment = StartInfo.EnvironmentVariables.Cast<System.Collections.DictionaryEntry>()
                .Select(_ => $"set {_.Key}={_.Value}").Join(),
        }.Destructure()}";
    }
}