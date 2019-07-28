using System;

namespace Amg.Build
{
    internal class PrefixProgress : TargetProgress
    {
        private TargetProgress progress;
        private readonly string prefix;

        public PrefixProgress(TargetProgress progress, string prefix)
        {
            this.progress = progress;
            this.prefix = prefix;
        }

        JobId T(JobId id)
        {
            return id.Prefix(prefix);
        }

        public void Begin(JobId id)
        {
            progress.Begin(T(id));
        }

        public void End(JobId id, object output)
        {
            progress.End(T(id), output);
        }

        public void Fail(JobId id, Exception exception)
        {
            progress.Fail(T(id), exception);
        }
    }
}