using Amg.FileSystem;

namespace Amg.Build
{
    /// <summary>
    /// Targets for git repositories
    /// </summary>
    public class Git
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// Constructor. To be called via Once.Create
        /// </summary>
        protected Git(string rootDirectory)
        {
            this.RootDirectory = rootDirectory;
        }

        /// <summary>
        /// Create an instance where all methods marked with [Once] will only be called once.
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <returns></returns>
        public static Git Create(string rootDirectory)
        {
            return Once.Create<Git>(rootDirectory);
        }

        /// <summary>
        /// Git command line tool
        /// </summary>
        [Once]
        public virtual ITool GitTool => Tools.Default.WithFileName("git.exe").WithArguments("-C", RootDirectory);

        /// <summary>
        ///  Fails if the git repository contains uncommited changes.
        /// </summary>
        /// Use this target to prevent the release of binaries built with local, uncommitted changes.
        [Once]
        public virtual async Task EnsureNoPendingChanges()
        {
            var r = await GitTool.Run("ls-files", "--modified", "--others", "--exclude-standard");
            if (!String.IsNullOrEmpty(r.Output))
            {
                throw new InvalidOperationException($@"The build requires that all git has no uncommitted changes.
Commit following files:

{r.Output}
            ");
            }
        }

        /// <summary>
        /// Repository root directory
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Execute buildStep only if commit hash was changed since last execution
        /// </summary>
        /// <param name="buildStep">build step to execute</param>
        /// <param name="stateFile">path to the state file</param>
        /// <returns></returns>
        [Obsolete("use GitExtensions.IfTreeChanged")]
        public async Task RebuildIfCommitHashChanged(Func<Task> buildStep, string stateFile)
        {
            var resultCommitHash = await stateFile.ReadAllTextAsync();
            var sourceHash = (await GitTool.Run("log", "-1", "--pretty=format:%H")).Output.Trim();

            if (!string.Equals(resultCommitHash, sourceHash))
            {
                await buildStep();
                await stateFile.WriteAllTextAsync(sourceHash);
            }
        }
    }
}