namespace Amg.Build;

/// <summary>
/// Create objects that execute methods marked with [Once] only once
/// </summary>
public static class Once
{
    /// <summary>
    /// Get an instance of type that executes methods marked with [Once] only once and caches the result.
    /// </summary>
    /// <returns></returns>
    public static object Create(Type type, params object?[] ctorArguments)
    {
        return OnceContainer.Instance.Get(type, ctorArguments);
    }

    /// <summary>
    /// Get an instance of T that executes methods marked with [Once] only once and caches the result.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Create<T>(params object?[] ctorArguments) where T : class
    {
        return (T)Create(typeof(T), ctorArguments);
    }
}
