using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Use nuget
    /// </summary>
    public class Nuget
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string source = null;

        /// <summary>
        /// ITool wrapper for nuget.exe
        /// </summary>
        [Once]
        public virtual Task<ITool> Tool => ProvideNugetTool();

        /// <summary>
        /// Access the Chocolatey repository
        /// </summary>
        public Nuget Chocolatey => GetChocolatey();

        /// <summary />
        [Once]
        protected virtual Nuget GetChocolatey()
        {
            return Runner.Once(new Nuget(){source = "https://chocolatey.org/api/v2/" });
        }

        /// <summary />
        [Once]
        protected virtual async Task<ITool> ProvideNugetTool()
        {
            return await Runner.Once<Tools>().Get(new Uri("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"));
        }

        /// <summary>
        /// Downloads a tool with nuget
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <param name="executable">file name of the executable relative to the package root.
        /// If omitted, a single *.exe file in the tools dir of the package will be used as the default executable</param>
        /// <returns></returns>
        [Once]
        public virtual async Task<ITool> GetTool(
            string packageId,
            string version = null,
            string executable = null
            )
        {
            var installDir = await Get(packageId, version);

            var path = executable == null
                ? GetDefaultExecutable(installDir)
                : installDir.Combine(executable);

            Logger.Information("nuget tool {packageId} uses {path}", packageId, path);
            return Tools.Default.WithFileName(path);
        }

        /// <summary>
        /// Downloads a nupkg and returns the root directory
        /// </summary>
        /// <param name="packageId"></param>
        /// <param name="version"></param>
        /// <returns>root directory of the package</returns>
        [Once]
        public virtual async Task<string> Get(
            string packageId,
            string version = null
            )
        {
            var install = (await Tool)
                .WithArguments(
                    "install",
                    "-ForceEnglishOutput",
                    "-PackageSaveMode", "nuspec"
                    );

            if (version != null)
            {
                install = install.WithArguments("-Version", version);
            }

            if (source != null)
            {
                install = install.WithArguments("-Source", source);
            }

            var r = await install.Run(packageId);

            var m = Regex.Match(r.Output, @"Installing package '([^']+)' to '([^']+)'.");
            var actualPackageId = m.Groups[1].Value;
            var dir = m.Groups[2].Value;

            m = Regex.Match(r.Output, @"Successfully installed '([^ ]+) ([^ ]+)' to");
            if (m.Success)
            {
                version = m.Groups[2].Value;
            }
            else
            {
                m = Regex.Match(r.Output, $@"Package ""{packageId.ToLower()}\.([^""]+)"" is already installed.", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    version = m.Groups[1].Value;
                }
            }

            var installDir =
                version.Lineage(".").Reverse()
                .Select(versionDir => dir.Combine(new[] { packageId, versionDir }.Join(".")))
                .First(_ => _.IsDirectory());

            return installDir;
        }

        static string GetDefaultExecutable(string nugetPackageInstallDir)
        {
            try
            {
                return nugetPackageInstallDir.Combine("tools")
                    .Glob("*.exe").EnumerateFiles().Single();
            }
            catch (Exception e)
            {
                throw new Exception($@"Cannot determine default executable in {nugetPackageInstallDir}.

Files:
{nugetPackageInstallDir.Glob("**").Join()}", e);
            }
        }

        /// <summary />
        protected Nuget()
        {
        }

        /// <summary>
        /// Create an instance where every method marked with [Once] is only executed once and its results are cached.
        /// </summary>
        /// <param name="nuget"></param>
        /// <returns></returns>
        public static Nuget Create(ITool nuget = null)
        {
            return Runner.Once<Nuget>();
        }
    }
}
