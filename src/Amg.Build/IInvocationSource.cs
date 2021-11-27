using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Amg.Build
{
    /// <summary>
    /// Creates proxies for classes that execute methods marked with [Once] only once.
    /// </summary>

    internal interface IInvocationSource
    {
        IEnumerable<IInvocation> Invocations { get; }
    }
}