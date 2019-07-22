using Amg.Build;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amg.CommandLine
{
    public class ParseException : Exception
    {
        private readonly IEnumerable<string> _args;
        private readonly IEnumerator<string> _currentPosition;

        public ParseException(IEnumerable<string> args, IEnumerator<string> currentPosition, Exception exception)
            : base("Error in command line arguments", exception)
        {
            _args = args;
            _currentPosition = currentPosition;
        }

        public override string Message => $@"Error in commmand line arguments:

{ArgList(_args, _currentPosition)}

{InnerException.Message}
";

        private static string ArgList(IEnumerable<string> args, IEnumerator<string> currentPosition)
        {
            return args.Select(_ => $"{Marker(_, currentPosition)}{_}").Join();
        }

        private static string Marker(string s, IEnumerator<string> currentPosition)
        {
            return ReferenceEquals(s, currentPosition.Current)
                ? "here => "
                : "        ";
        }
    }
}
