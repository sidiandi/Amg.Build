using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    internal class TargetFailed : Exception
    {
        private readonly Targets.TargetStateBase targetState;

        public TargetFailed(Targets.TargetStateBase targetState, Exception innerException)
            :base($"{targetState} failed.", innerException)
        {
            this.targetState = targetState;
        }

        protected TargetFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}