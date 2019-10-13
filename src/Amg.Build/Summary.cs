using Amg.Build.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Amg.Build
{
    static class Summary
    {
        internal static IWritable PrintTimeline(IEnumerable<InvocationInfo> invocations) => TextFormatExtensions.GetWritable(@out =>
        {
            var begin = invocations
                .Select(_ => _.Begin.GetValueOrDefault(DateTime.MaxValue))
                .Min();

            var end = invocations
                .Select(_ => _.End.GetValueOrDefault(DateTime.MinValue))
                .Max();

            var success = invocations.All(_ => !_.Failed);

            @out.WriteLine("Summary");
            new
            {
                success,
                begin,
                end,
                duration = end - begin
            }.PropertiesTable().Write(@out);

            @out.WriteLine();

            invocations.OrderBy(_ => _.End)
                .Select(_ => new
                {
                    Name = _.Id.Truncate(32),
                    State = _.State,
                    Duration = _.Duration.HumanReadable(),
                    Timeline = TextFormatExtensions.TimeBar(80, begin, end, _.Begin, _.End)
                })
                .ToTable()
                .Write(@out);
        });

        public static Exception GetRootCause(Exception e)
        {
            if (e is InvocationFailedException)
            {
                return e;
            }
            else
            {
                return e.InnerException == null
                    ? e
                    : GetRootCause(e.InnerException);
            }
        }

        internal static IWritable Error(IEnumerable<InvocationInfo> invocations) => TextFormatExtensions.GetWritable(@out =>
        {
            @out.WriteLine();
            foreach (var failedTarget in invocations.OrderByDescending(_ => _.End)
                .Where(_ => _.State == InvocationInfo.States.Failed))
            {
                var exception = failedTarget.Exception!;
                var r = GetRootCause(exception);
                if (!(r is InvocationFailedException))
                {
                    @out.WriteLine($"{r.FileAndLine()}: target {failedTarget} failed. Reason: {r.Message}");
                }
            }
            @out.WriteLine("FAILED");
        });

        internal static IWritable ErrorDetails(InvocationInfo failed) => TextFormatExtensions.GetWritable(o =>
        {
            var ex = failed.Exception;
            if (ex != null)
            {
                if (ex is InvocationFailedException)
                {
                    o.Write(ex.Message);
                }
                else
                {
                    o.WriteLine($@"{ex.Message}

Exception:

{ex.GetType()}: {ex.Message}

");
                    o.Write(ex.StackTrace.SplitLines()
                        .Where(_ => !Regex.IsMatch(_, @"(at System.Threading.|--- End of stack trace from previous location where exception was thrown ---|Amg.Build.InvocationInfo.TaskHandler.GetReturnValue)"))
                        .Join());
                    o.WriteLine();
                }
            }
        });

        internal static IWritable ShortErrorDetails(InvocationInfo failed) => TextFormatExtensions.GetWritable(o =>
        {
            var ex = failed.Exception;
            if (ex != null)
            {
                o.Write(ex.Message);
            }
        });

        internal static IWritable ErrorMessage(InvocationInfo failed) => TextFormatExtensions.GetWritable(o =>
        {
            var ex = failed.Exception;
            if (ex != null)
            {
                foreach (var sl in ex.SourceLocations().Reverse().Skip(1).Take(1))
                {
                    o.WriteLine($@"{sl}: {failed} failed at {failed.End!:o}. Reason:
{ErrorDetails(failed).Indent("  ")}");
                }
            }
        });

        internal static void PrintSummary(IEnumerable<InvocationInfo> invocations)
        {
            if (invocations.Failed())
            {
                foreach (var fail in invocations.Where(_ => _.Failed))
                {
                    ErrorMessage(fail).Write(Console.Error);
                }
                /*
                             )
                            (
                              ,
                           ___)\
                          (_____)
                         (_______)
                */
            }
        }

        internal static void PrintAsciiArt(IEnumerable<InvocationInfo> invocations)
        {
            if (invocations.Failed())
            {
                Console.Error.WriteLine(@"

      )
     (
       ,
    ___)\
   (_____)
  (_______)
");
            }
            else
            {
                Console.Out.WriteLine(@"

           /(|
          (  :
         __\  \  _____
       (____)  `|
      (____)|   |
       (____).__|
        (___)__.|_____
");
            }

        }

        private static string RootCause(InvocationInfo i)
        {
            throw new NotImplementedException();
        }
    }
}