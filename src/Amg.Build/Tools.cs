using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Frequently used tools
    /// </summary>
    public class Tools
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// Default tool settings
        /// </summary>
        public static ITool Default { get; set; } = new Tool()
            .WithOnOutput(old => (r, line) => Console.Out.WriteLine($"{r}:{line}"))
            .WithOnError(old => (r, line) => Console.Error.WriteLine($"{r}:{line}"));

        /// <summary>
        /// cmd.exe /c
        /// </summary>
        public static ITool Cmd => Default.WithFileName("cmd.exe").WithArguments("/c");

        /// <summary>
        /// Create a [Once] result cache of Tools
        /// </summary>
        /// Add to your class like this:
        /// 
        /// <returns></returns>
        public static Tools Create()
        {
            return Runner.Once<Tools>();
        }

        /// <summary>
        /// nuget.exe
        /// </summary>
        /// Downloads nuget.exe if not found.
        [Once]
        public virtual Task<ITool> Nuget => Get(new Uri(@"https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"));

        /// <summary>
        /// Download a tool
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        [Once]
        public virtual async Task<ITool> Get(Uri uri)
        {
            var dir = FileSystemExtensions.GetProgramDataDirectory(typeof(Tools))
                .Combine(uri.ToString().Md5Checksum());
            var file = dir.Combine(uri.LocalPath.FileName());
            if (!file.IsFile())
            {
                using (var webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(
                        uri.ToString(),
                        file.EnsureParentDirectoryExists());
                }
            }

            return Default.WithFileName(file);
        }
    }
}