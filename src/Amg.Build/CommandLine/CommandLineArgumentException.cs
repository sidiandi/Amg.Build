using Amg.Build.Extensions;
using System;
using System.Linq;

namespace Amg.CommandLine
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3871:Exception types should be \"public\"", Justification = "<Pending>")]
    internal class CommandLineArgumentException : Exception
    {
        private readonly ArraySegment<string> _args;

        public CommandLineArgumentException(
            ArraySegment<string> args,
            string message)
            : base(message)
        {
            _args = args;
        }

        public CommandLineArgumentException(
            ArraySegment<string> args,
            Exception exception)
            : base(exception.Message, exception)
        {
            _args = args;
        }

        public CommandLineArgumentException(
            ArraySegment<string> args,
            string message,
            Exception exception)
            : base(message, exception)
        {
            _args = args;
        }

        public override string Message => $@"Error in commmand line arguments:

{ArgList(_args)}

{(InnerException == null ? String.Empty :InnerException.Message)}
";

        private static string ArgList(ArraySegment<string> args)
        {
            return args.Array.Select((_,i) => $"{Marker(i, args.Offset)}{_}").Join();
        }

        private static string Marker(int index, int markedPosition)
        {
            return index == markedPosition
                ? "here => "
                : "        ";
        }
    }
}
