using System;
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

        /// <summary />
        public ToolException()
        {
        }

        /// <summary />
        public ToolException(string message, IToolResult result)
            : base(message)
        {
            this.Result = result;
        }

        /// <summary />
        protected ToolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary />
        public override string Message => $@"{base.Message}
{Result.Dump()}";
    }
}
