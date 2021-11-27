using Amg.Extensions;
using Amg.FileSystem;
using System.Net;

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
            return Once.Create<Tools>();
        }

        [Once] protected virtual Nuget NugetHelper => Once.Create<Nuget>();

        /// <summary>
        /// nuget.exe
        /// </summary>
        [Once]
        protected virtual Task<ITool> NugetTool => NugetHelper.Tool();

        /// <summary>
        /// Download a tool
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        [Once]
        public virtual async Task<ITool> Get(Uri uri)
        {
            var dir = typeof(Tools).GetProgramDataDirectory()
                .Combine(uri.ToString().Md5Checksum());
            var fileName = uri.LocalPath.FileName();
            var file = dir.Combine(fileName);
            if (!file.IsFile())
            {
                await dir.EnsureNotExists();
                var tempDir = dir + "-download";
                var tempFile = tempDir.Combine(fileName);
                try
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.UseDefaultCredentials = true;
                        webClient.Proxy = WebRequest.GetSystemWebProxy();
                        await webClient.DownloadFileTaskAsync(
                            uri.ToString(),
                            tempFile.EnsureParentDirectoryExists());
                    }
                    await tempDir.Move(dir);
                }
                finally
                {
                    await tempDir.EnsureNotExists();
                }
            }

            return Default.WithFileName(file);
        }
    }
}