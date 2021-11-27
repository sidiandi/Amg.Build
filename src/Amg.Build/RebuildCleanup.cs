using Amg.FileSystem;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;

namespace Amg.Build
{
    class RebuildCleanup
    {
        private static Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static async Task Handle()
        {
            if (!HasArgs()) return;

            try
            {
                Logger.Information("Entering Cleanup handler");
                var args = GetArgs();
                Logger.Information("{@args}", args);
                var cleanup = new RebuildCleanup();
                await cleanup.CleanupInternal(args);
                Logger.Information("Cleanup handler success.");
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Logger.Error(e, "cleanup handler");
            }
        }

        async Task CleanupInternal(Args args)
        {
            try
            {
                var old = await args.Dest!.MoveToBackup();
                if (old != null)
                {
                    old = await old.EnsureNotExists();
                    Logger.Information("Deleted {old}", old);
                }
                await args.Source!.CopyTree(args.Dest!, useHardlinks: true);
            }
            catch (Exception exception)
            {
                Logger.Debug("Cannot cleanup {args} at the moment. {exception}", args, exception);
            }
        }

        internal static bool HasArgs()
        {
            var argsJson = System.Environment.GetEnvironmentVariable(ArgsKey);
            return !String.IsNullOrEmpty(argsJson);
        }

        internal static Args GetArgs()
        {
            var argsJson = System.Environment.GetEnvironmentVariable(ArgsKey);
            if (String.IsNullOrEmpty(argsJson))
            {
                throw new InvalidOperationException("no args");
            }

            var args = JsonConvert.DeserializeObject<Args>(argsJson);
            return args;
        }

        internal static void SetArgs(Args move, ProcessStartInfo processStartInfo)
        {
            var json = JsonConvert.SerializeObject(move);
            processStartInfo.EnvironmentVariables.Add(ArgsKey, json);
            Logger.Debug($"set {ArgsKey}={json}");
        }

        public static void Start(string fileName, Args move)
        {
            // before exiting, start a process that moves TempAssemblyFile to AssemblyFile
            var si = new ProcessStartInfo
            {
                FileName = fileName,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            SetArgs(move, si);
            Logger.Debug("Starting cleanup process: {@move}", move);
            Process.Start(si);
        }

        internal static string ArgsKey => nameof(RebuildCleanup) + "8ce0a148334b44e58b2cd832fdf935ea";

        public ILogger Logger1 => Logger;

        internal class Args
        {
            public string? Source { set; get; }
            public string? Dest { set; get; }
        }

    }
}