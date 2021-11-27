using Amg.FileSystem;

namespace Amg.Build;

/// <summary>
/// Utility methosds to be used with the Git class
/// </summary>
public static class GitExtensions
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    static string GetReference(this Git git, string path)
    {
        var relativeTreePath = path.Absolute().ChangeRoot(git.RootDirectory, String.Empty);

        if (relativeTreePath.StartsWith("\\"))
        {
            relativeTreePath = relativeTreePath.Substring(1);
        }

        relativeTreePath = relativeTreePath.Replace("\\", "/");

        return $"HEAD:{relativeTreePath}";
    }

    /// <summary>
    /// Returns the git hash of a file or directory in the working copy.
    /// </summary>
    /// <param name="git"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static async Task<string> GetHash(this Git git, string path)
    {
        var reference = git.GetReference(path);
        var sourceVersion = (await git.GitTool.Run("rev-parse", reference)).Output.Trim();
        return sourceVersion;
    }

    /// <summary>
    /// Execute buildAction if the existing outputPath was built with another tree hash of treePath.
    /// </summary>
    /// <param name="git"></param>
    /// <param name="outputPath">output file or directory. Will be deleted and overwritten.</param>
    /// <param name="sourcePath">source file or directory. Must be a git object.</param>
    /// <param name="buildAction"></param>
    /// <returns></returns>

    public static async Task<string> IfChanged(this Git git, string outputPath, string sourcePath, Func<Task> buildAction)
    {
        var sourceVersion = await git.GetHash(sourcePath);
        var versionFile = outputPath + ".source-version";
        var existingVersion = await versionFile.ReadAllTextAsync();
        if (string.Equals(sourceVersion, existingVersion))
        {
            Logger.Information("Skipping build action because output {output} was built with current content of tree {tree}. SHA1: {sha1}",
                outputPath,
                sourcePath,
                existingVersion
                );
        }
        else
        {
            await buildAction();
            await versionFile.WriteAllTextAsync(sourceVersion);
        }
        return outputPath;
    }
}
