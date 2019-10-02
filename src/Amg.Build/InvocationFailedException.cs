using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
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