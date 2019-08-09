using System;

namespace Amg.Build
{
    internal class ProgressAction<T> : IProgress<T>
    {
        private Action<T> onProgress;

        public ProgressAction(Action<T> onProgress)
        {
            this.onProgress = onProgress;
        }

        public void Report(T value)
        {
            onProgress(value);
        }
    }
}