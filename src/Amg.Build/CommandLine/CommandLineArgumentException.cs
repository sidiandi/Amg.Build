using Amg.Build;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amg.CommandLine
{
    internal class CommandLineArgumentException : Exception
    {
        private readonly IEnumerable<string> _args;
        private readonly int _markedPosition;

        public CommandLineArgumentException(IEnumerable<string> args, IEnumerator<string> currentPosition, Exception exception)
            : base("Error in command line arguments", exception)
        {
            _args = args;
            var p = args
                .Select((_, i) => new { _, i })
                .FirstOrDefault(_ => object.ReferenceEquals(_._, currentPosition.Current));
            _markedPosition = (p == null)
                ? -1
                : p.i;
        }

        public CommandLineArgumentException(
            IEnumerable<string> args, 
            int markedPosition, 
            Exception exception)
            : base("Error in command line arguments", exception)
        {
            _args = args;
            _markedPosition = markedPosition;
        }

        public override string Message => $@"Error in commmand line arguments:

{ArgList(_args, _markedPosition)}

{InnerException.Message}
";

        private static string ArgList(IEnumerable<string> args, int markedPosition)
        {
            return args.Select((_,i) => $"{Marker(i, _, markedPosition)}{_}").Join();
        }

        private static string Marker(int index, string s, int markedPosition)
        {
            return index == markedPosition
                ? "here => "
                : "        ";
        }
    }
}
