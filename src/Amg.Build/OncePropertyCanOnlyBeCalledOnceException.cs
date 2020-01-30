using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    public class OncePropertyCanOnlyBeSetBeforeFirstGetException : Exception
    {
        public MethodInfo Method { get; }

        public OncePropertyCanOnlyBeSetBeforeFirstGetException(MethodInfo method)
        : base($"Property {method} is decorated with [Once] and can only be set once.")
        {
            this.Method = method;
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        protected OncePropertyCanOnlyBeSetBeforeFirstGetException(SerializationInfo info, StreamingContext context) : base(info, context)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }
    }
}