using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    internal class TargetFailed : Exception
    {
        private readonly Targets.TargetStateBase targetState;

        public TargetFailed(string name, object input, Exception innerException)
            :base($"{name}({input}) failed.", innerException)
        {
            this.targetState = targetState;
        }

        protected TargetFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}