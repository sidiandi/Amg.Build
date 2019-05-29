using System;
using System.Runtime.Serialization;

namespace Csa.Build
{
    [Serializable]
    public class ToolException : Exception
    {
        public IToolResult Result { get; private set; }

        public ToolException()
        {
        }

        public ToolException(string message, IToolResult result)
            : base(message)
        {
            this.Result = result;
        }

        protected ToolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override string Message => $@"{base.Message}
{Result.Dump()}";
    }
}