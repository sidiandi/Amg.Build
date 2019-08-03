using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    internal class Summary
    {
        internal static IWritable Print(IEnumerable<OnceInterceptor.Invocation> invocations) => TextFormatExtensions.GetWritable(@out =>
        {
            var begin = invocations
                .Select(_ => _.Begin.GetValueOrDefault(DateTime.MaxValue))
                .Min();

            var end = invocations
                .Select(_ => _.End.GetValueOrDefault(DateTime.MinValue))
                .Max();

            invocations.OrderBy(_ => _.End)
                .Select(_ => new
                {
                    Name = _.Id,
                    State = _.State,
                    Duration = _.Duration.HumanReadable(),
                    Timeline = TextFormatExtensions.TimeBar(80, begin, end, _.Begin.Value, _.End.Value)
                })
                .ToTable()
                .Write(@out);
        });
    }
}