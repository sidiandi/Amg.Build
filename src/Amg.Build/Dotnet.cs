using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Subtargets for the dotnet core framework
    /// </summary>
    public class Dotnet
    {
        /// <summary>
        /// Only construct via <![CDATA[Runner.Once<Dotnet>()]]> 
        /// </summary>
        protected Dotnet() { }

        /// <summary>
        /// Dotnet tool
        /// </summary>
        [Once]
        public virtual Task<ITool> Tool() => Task.FromResult(Tools.Default.WithFileName("dotnet.exe"));

        /// <summary>
        /// dotnet version
        /// </summary>
        [Once]
        public virtual async Task<string> Version()
        {
            var d = await Tool();
            return (await d.Run("--version")).Output.Trim();
        }
    }
}
