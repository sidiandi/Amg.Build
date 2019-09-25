using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Amg.Build
{
    /// <summary>
    /// To be thrown by ITool.Run when the process cannot be started.
    /// </summary>
    [Serializable]
    public class ToolStartFailed : Exception
    {
        /// <summary>
        /// information that was used to start the process.
        /// </summary>
        public ProcessStartInfo StartInfo { get; }

        /// <summary />
        public ToolStartFailed()
        {
        }

        /// <summary />
        public ToolStartFailed(string message, ProcessStartInfo startInfo)
            : base(message)
        {
            StartInfo = startInfo;
        }

        /// <summary />
        protected ToolStartFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
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
        }.Dump()}";

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
        }.Dump()}";
    }
}
