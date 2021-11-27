using Amg.Build;

namespace Amg.Build;

internal class OnceExample
{
    [Once]
    public virtual async Task Compile()
    {
        await Task.CompletedTask;
    }

    [Once]
    public virtual async Task Test()
    {
        await Compile();

        // ... testing done here ...
    }

    [Once]
    public virtual async Task Package()
    {
        await Compile();
        // ... packaging the compiled binaries here ...
    }

    [Once]
    public virtual async Task Release()
    {
        await Task.WhenAll(Test(), Package());
    }
}
