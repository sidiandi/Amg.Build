namespace Build
{
    public partial class Program
    {
        string Name => "Amg.Build";
        string Company => "Amg";
        string[] NugetPushSource => new[]
        {
            null,
            "https://api.nuget.org/v3/index.json"
        };
        string[] NugetPushSymbolSource => NugetPushSource;
    }
}