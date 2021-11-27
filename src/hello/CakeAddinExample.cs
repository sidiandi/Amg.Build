using Cake.Common.IO;

namespace hello;

internal class CakeAddinExample
{
    [Once]
    protected virtual Cake.Core.ICakeContext Cake => Amg.Build.Cake.Cake.CreateContext();

    [Once]
    public virtual void ZipSomethingWithCake()
    {
        Cake.Zip(
            Runner.RootDirectory().Combine("hello"),
            Runner.RootDirectory().Combine("out", "z.zip").EnsureParentDirectoryExists());
    }
}
