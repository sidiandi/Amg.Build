using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    internal class TargetFailed : Exception
    {
        public TargetFailed(JobId id, Exception innerException)
            :base($"{id} failed.", innerException)
        {
        }

        protected TargetFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}