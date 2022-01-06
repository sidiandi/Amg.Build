namespace Amg.Build;

public enum InvocationState
{
    Pending,
    InProgress,
    Done,
    Failed
}

public interface IInvocation
{
    InvocationId Id { get; }
    InvocationState State { get; }
    DateTime? Begin { get; }
    DateTime? End { get; }
    object? ReturnValue { get; }
    Exception? Exception { get; }
}

public static class InvocationExtensions
{
    public static TimeSpan? Duration(this IInvocation invocation)
    {
        return invocation.Begin is { } && invocation.End is { }
            ? invocation.End - invocation.Begin
            : null;
    }

    public static bool Failed(this IInvocation invocation)
        => invocation.Exception is { };

    public static bool Failed(this IEnumerable<IInvocation> invocations)
        => invocations.Any(_ => _.Failed());
}
