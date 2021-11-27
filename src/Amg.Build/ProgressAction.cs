namespace Amg.Extensions;

internal class ProgressAction<T> : IProgress<T>
{
    readonly private Action<T> onProgress;

    public ProgressAction(Action<T> onProgress)
    {
        this.onProgress = onProgress;
    }

    public void Report(T value)
    {
        onProgress(value);
    }
}
