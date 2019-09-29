using System;
using System.Runtime.Serialization;

namespace Amg.Build
{
    [Serializable]
    internal class InvocationFailed : Exception
    {
        private Exception? exception;
        private InvocationInfo? invocationInfo;

        public InvocationFailed()
        {
        }

        public InvocationFailed(Exception exception, InvocationInfo invocationInfo)
           : base($"{invocationInfo} failed.", exception)
        {
            this.exception = exception;
            this.invocationInfo = invocationInfo;
        }

        protected InvocationFailed(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static IWritable ShortMessage(Exception ex) => TextFormatExtensions.GetWritable(w =>
        {
            if (ex is InvocationFailed i)
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