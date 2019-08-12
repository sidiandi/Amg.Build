using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    internal class Summary
    {
        internal static IWritable Print(IEnumerable<InvocationInfo> invocations) => TextFormatExtensions.GetWritable(@out =>
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
            foreach (var failedTarget in invocations.OrderBy(_ => _.End)
                .Where(_ => _.State == InvocationInfo.States.Failed))
            {
                var r = GetRootCause(failedTarget.Exception);
                @out.WriteLine($"{failedTarget} failed because: {r.Message}");
                if (!(r is InvocationFailed))
                {
                    @out.WriteLine($@"
{r}
");
                }

            }
        });

        private static string RootCause(InvocationInfo i)
        {
            throw new NotImplementedException();
        }
    }
}