using Amg.GetOpt;

namespace hello;

public class Program
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    static int Main(string[] args) => Runner.Run(args);

    [Once, Description("Demo of the Cake adapter")]
    public virtual void WorkWithCakeAddins()
    {
        var cakeExample = Once.Create<CakeAddinExample>();
        cakeExample.ZipSomethingWithCake();
    }

    [Once, Description("Greet someone.")]
    public virtual async Task Greet(string name)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        Logger.Information($"Hello, {name}");
        Console.WriteLine($"Hello, {name}");
    }

    [Once, Description("Greet all.")]
    public virtual async Task GreetAll()
    {
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Greet($"Alice {_}")).ToArray());
        await Greet(Enumerable.Range(0, 100).Select(_ => "Very long name ").Join());
    }

    [Once, Description("Simulate a failing tool")]
    public virtual async Task FailTool()
    {
        await Tools.Cmd.Run("/c", "fasdfasdfasd");
    }

    [Once, Description("Runs forever")]
    public virtual async Task RunForever()
    {
        await Tools.Default.WithFileName("cmd")
            .Run();
    }

    [Once, Cached, Description("Long running operations can be cached.")]
    public virtual async Task<string> SlowGreet(string name)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return $"Hello, {name}. This took a while";
    }

    [Once, Description("Use failing tool")]
    public virtual async Task UseFailingTool()
    {
        await Task.WhenAll(FailTool(), RunForever());
    }

    [Once, Description("File dependencies")]
    public async virtual Task<string> GreetingFile()
        => (await Make.Rule(
            new[] { Runner.RootDirectory().Combine("greeting") },
            new[] { await NameFile() },
            async (outputs, inputs) =>
            {
                await inputs.First().CopyTree(outputs.First());
            }
        )).First();

    [Once, Description("File dependencies")]
    public async virtual Task<string> GreetingFile2()
        => await Make.Rule(
            Runner.RootDirectory().Combine("greeting"),
            await NameFile(),
            async (output, input) =>
            {
                await input.CopyTree(output);
            }
        );

    protected virtual async Task<string> NameFile()
    {
        var nameFile = Runner.RootDirectory().Combine("name");
        if (!nameFile.IsFile())
        {
            await nameFile.WriteAllTextAsync("hello");
        }
        return nameFile;
    }

    [Once, Default, Description("Greet Alice.")]
    public virtual async Task Default()
    {
        await Greet("Someone with a really long name. Someone with a really long name. Someone with a really long name. Someone with a really long name.");
        await Greet("Alice");
        foreach (var i in Enumerable.Range(0, 10))
        {
            await SlowGreet(i.ToString());
        }
    }
}
