using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Amg.Build
{
    internal static class InternalExtensions
    {
        internal static string FileAndLine(this Exception exception)
        {
            var files = FileAndLineFromStackTrace(exception.StackTrace);
            if (exception is InvocationFailed)
            {
                files = files.Skip(1);
            }
            return files.FirstOrDefault();
        }

        static internal IEnumerable<string> FileAndLineFromStackTrace(this string stackTrace)
        {
            return Regex.Matches(stackTrace, @"in (.*):line (\d+)").Cast<Match>()
                .Select(m => new { file = m.Groups[1].Value, line = m.Groups[2].Value })
                .Select(_ => $"{_.file}({_.line})");
        }
    }
}
