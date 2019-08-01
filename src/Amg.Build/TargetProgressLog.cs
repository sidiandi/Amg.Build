using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Amg.Build
{
    class TargetLogger : TargetProgress
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TargetProgress next;

        public TargetLogger(TargetProgress next)
        {
            this.next = next;
        }

        public void Begin(JobId id)
        {
            Banner("start {id}", id);
            next.Begin(id);
        }

        public void End(JobId id, object output)
        {
            Banner("end {id}: {output}", id, output);
            next.End(id, output);
        }

        public void Fail(JobId id, Exception exception)
        {
            Banner(@"fail {id}

{output}", id, TargetProgressLog.GetRootCause(exception));
            next.Fail(id, exception);
        }

        static void Banner(string message, params object[] args)
        {
            var ruler = new string('#', 128);
            Logger.Information($@"
{ruler}
{message}
{ruler}", args);
        }
    }

    class TargetProgressLog : TargetProgress
    {
        IDictionary<JobId, TargetData> state = new Dictionary<JobId, TargetData>();

        public void Begin(JobId id)
        {
            var s = GetState(id);
            s.Begin = DateTime.UtcNow;
        }

        TargetData GetState(JobId id)
        {
            return state.GetOrAdd(id, () => new TargetData(id));
        }

        public void End(JobId id, object output)
        {
            var s = GetState(id);
            s.End = DateTime.UtcNow;
            s.Output = output;
        }

        public void Fail(JobId id, Exception exception)
        {
            var s = GetState(id);
            s.End = DateTime.UtcNow;
            s.Exception = exception;
        }

        public enum State
        {
            Running,
            Success,
            Failed
        }

        private class TargetData
        {
            public TargetData(JobId id)
            {
                Id = id;
            }

            public Exception Exception { get; internal set; }
            public DateTime? End { get; internal set; }
            public DateTime? Begin { get; internal set; }
            public object Input { get; internal set; }
            public object Output { get; internal set; }
            public bool Failed => Exception != null;

            public TimeSpan Duration => End.Value - Begin.Value;

            public State State
            {
                get
                {
                    if (Failed)
                    {
                        return State.Failed;
                    }
                    else if (Output != null)
                    {
                        return State.Success;
                    }
                    else
                    {
                        return State.Running;
                    }
                }
            }

            public JobId Id { get; }

            public override string ToString() => Id.ToString();
        }

        static DateTime? Max(DateTime? a, DateTime? b)
        {
            return (a == null)
                ? b
                : b == null
                    ? null
                    : a.Value > b.Value
                        ? a
                        : b;
        }

        static DateTime? Min(DateTime? a, DateTime? b)
        {
            return (a == null)
                ? b
                : b == null
                    ? null
                    : a.Value < b.Value
                        ? a
                        : b;
        }

        public static Exception GetRootCause(Exception e)
        {
            if (e is TargetFailed)
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

        public void PrintErrorSummary(TextWriter @out)
        {
            foreach (var failedTarget in state.Values
                .Where(_ => _.Failed)
                .OrderBy(_ => _.End))
            {
                var r = GetRootCause(failedTarget.Exception.InnerException);
                @out.WriteLine($"{failedTarget} failed because: {r.Message}");
                if (!(r is TargetFailed))
                {
                    @out.WriteLine($@"
{r}
");
                }
            }
        }

        public void PrintSummary(TextWriter @out)
        {
            var end = state.Values.Aggregate((DateTime?)null, (m, _) => Max(m, _.End));
            var begin = state.Values.Aggregate((DateTime?)null, (m, _) => Min(m, _.Begin));

            if (end == null || begin == null)
            {
                return;
            }

            new
            {
                Begin = begin,
                End = end,
                Duration = (end.Value - begin.Value).HumanReadable(),
            }.ToPropertiesTable().Write(@out);

            @out.WriteLine();

            state.Values.OrderBy(_ => _.End)
                .Select(_ => new
                {
                    _.Id,
                    Duration = _.Duration.HumanReadable(),
                    _.State,
                    Timeline = _.Begin.HasValue && _.End.HasValue
                        ? TextFormatExtensions.TimeBar(80, begin.Value, end.Value, _.Begin.Value, _.End.Value)
                        : String.Empty
                })
                .ToTable().Write(@out);
        }
    }
}
