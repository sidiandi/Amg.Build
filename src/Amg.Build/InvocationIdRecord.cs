using Amg.Extensions;
using System;
using System.Linq;

namespace Amg.Build
{
    public sealed partial class InvocationId : IEquatable<InvocationId>
    {
        public InvocationId(
            string instanceId,
            string method,
            object[] arguments)
        {
            this.InstanceId = instanceId;
            this.Method = method;
            this.Arguments = arguments;
        }

        public bool Equals(InvocationId other)
        {
            var r = InstanceId.Equals(other.InstanceId)
                && Method.Equals(other.Method)
                && Arguments.SequenceEqual(other.Arguments);
            return r;
        }

        public override bool Equals(object obj) => obj is InvocationId r && Equals(r);

        public override int GetHashCode()
        {
            var hc = InstanceId.GetHashCode();
            hc += 23 * Method.GetHashCode();
            return hc;
        }

        public override string ToString() => $"{Method}({Arguments.Join(", ")})";
    }
}
