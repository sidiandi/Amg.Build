using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Targets for git repositories
    /// </summary>
    public class Git : Targets
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Use GitVersion to extract the application version
        /// </summary>
        public Target<GitVersion.VersionVariables> GetVersion => DefineTarget(() =>
        {
            return Task.Factory.StartNew(() =>
            {
                var gitVersionExecuteCore = new GitVersion.ExecuteCore(new GitVersion.Helpers.FileSystem());
                string templateString = @"GitVersion: {message}";
                GitVersion.Logger.SetLoggers(
                _ => Logger.Debug(templateString, _),
                _ => Logger.Information(templateString, _),
                _ => Logger.Warning(templateString, _),
                _ => Logger.Error(templateString, _));
                if (!gitVersionExecuteCore.TryGetVersion(".", out var versionVariables, true, null))
                {
                    throw new System.Exception("Cannot read version");
                }
                return versionVariables;
            }, TaskCreationOptions.LongRunning);
        });

        /// <summary>
        ///  Fails if the git repository contains uncommited changes.
        /// </summary>
        /// Use this target to prevent the release of binaries built with local, uncommitted changes.
        public Target EnsureNoPendingChanges => DefineTarget(async () =>
        {
            var git = new Tool("git");
            var r = await git.Run("ls-files", "--modified", "--others", "--exclude-standard");
            if (!String.IsNullOrEmpty(r.Output))
            {
                throw new Exception($@"The build requires that all git has no uncommitted changes.
Commit following files:

{r.Output}
            ");
            }
        });
    }
}
