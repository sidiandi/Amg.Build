using Amg.Extensions;

namespace Amg.Build
{
    public record InvocationId
    {
        public string InstanceId { get; }
        public string Method { get; }
        public object[] Arguments { get; }

        public InvocationId(
            string instanceId,
            string method,
            object[] arguments)
        {
            this.InstanceId = instanceId;
            this.Method = method;
            this.Arguments = arguments;
        }

        public override string ToString() => $"{Method}({Arguments.Join(", ")})";
    }
}
