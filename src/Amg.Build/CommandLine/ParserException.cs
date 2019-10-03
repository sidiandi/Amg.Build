using System;
using System.Runtime.Serialization;

namespace Amg.CommandLine
{
    [Serializable]
#pragma warning disable S3871 // Exception types should be "public"
    internal class ParserException : Exception
#pragma warning restore S3871 // Exception types should be "public"
    {
        public ParserException()
        {
        }

        public ParserException(string message) : base(message)
        {
        }

        public ParserException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}