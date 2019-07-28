namespace Amg.Build
{
    /// <summary>
    /// Subtargets for the dotnet core framework
    /// </summary>
    public class Dotnet : Targets
    {
        /// <summary>
        /// Dotnet tool
        /// </summary>
        public Target<Tool> Tool => DefineTarget(() =>
        {
            return new Tool("dotnet");
        });

        /// <summary>
        /// dotnet version
        /// </summary>
        public Target<string> Version => DefineTarget(async () =>
        {
            var d = await Tool();
            return (await d.Run("--version")).Output.Trim();
        });
    }
}
