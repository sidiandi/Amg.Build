using Amg.Extensions;
using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3871:Exception types should be \"public\"", Justification = "<Pending>")]
    internal class InvocationFailedException : Exception
    {
        public InvocationInfo Invocation { get; }

        public InvocationFailedException(InvocationInfo invocationInfo)
           : base($"{invocationInfo} failed.", invocationInfo.Exception)
        {
            this.Invocation = invocationInfo;
        }

        protected InvocationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Invocation = null!;
        }

        public static IWritable ShortMessage(Exception ex) => TextFormatExtensions.GetWritable(w =>
        {
            if (ex is InvocationFailedException i)
            {
                w.Write(i.Message);
            }
            else
            {
                w.Write(ex);
            }
        });
    }
}