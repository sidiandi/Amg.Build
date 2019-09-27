using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    internal class Summary
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

            @out.WriteLine();
            new
            {
                success,
                begin,
                end,
                duration = end - begin
            }.ToPropertiesTable().Write(@out);

            @out.WriteLine();

            invocations.OrderBy(_ => _.End)
                .Select(_ => new
                {
                    Name = _.Id.Truncate(32),
                    State = _.State,
                    Duration = _.Duration.HumanReadable(),
                    Timeline = TextFormatExtensions.TimeBar(80, begin, end, _.Begin.Value, _.End.Value)
                })
                .ToTable()
                .Write(@out);
        });

        public static Exception GetRootCause(Exception e)
        {
            if (e is InvocationFailed)
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
                var r = GetRootCause(failedTarget.Exception);
                if (!(r is InvocationFailed))
                {
                    @out.WriteLine($"{r.FileAndLine()}: target {failedTarget} failed. Reason: {r.Message}");
                }
            }
            @out.WriteLine("FAILED");
        });

        internal static void PrintSummary(IEnumerable<InvocationInfo> invocations) => TextFormatExtensions.GetWritable(@out =>
        {
            if (invocations.Any(_ => _.Failed))
            {
                Console.Error.WriteLine("FAILED");
            }
            else
            {
                Console.WriteLine("success");
            }
        });

        private static string RootCause(InvocationInfo i)
        {
            throw new NotImplementedException();
        }
    }
}