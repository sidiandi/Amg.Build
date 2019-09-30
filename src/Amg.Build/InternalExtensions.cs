using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

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

        internal static IEnumerable<string> SourceLocations(this Exception exception)
        {
            return exception.StackTrace.FileAndLineFromStackTrace();
        }

        static IEnumerable<string> FileAndLineFromStackTrace(this string stackTrace)
        {
            return Regex.Matches(stackTrace, @"in (.*):line (\d+)").Cast<Match>()
                .Select(m => new { file = m.Groups[1].Value, line = m.Groups[2].Value })
                .Select(_ => $"{_.file}({_.line})");
        }

        static internal string Fullname(this MethodInfo method)
        {
            return method.DeclaringType.FullName + "." + method.Name;
        }
    }
}
