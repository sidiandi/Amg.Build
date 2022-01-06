using Amg.Extensions;
using Amg.FileSystem;
using System.Text.RegularExpressions;

namespace Amg.Build;

/// <summary>
/// Use nuget
/// </summary>
public class Nuget
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    readonly string? source = null;

    /// <summary>
    /// ITool wrapper for nuget.exe
    /// </summary>
    [Once]
    public virtual async Task<ITool> Tool() => tool == null
            ? await new Tools().Get(DownloadUri)
            : tool;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>")]
    Uri DownloadUri => new Uri(@"https://dist.nuget.org/win-x86-commandline/latest/nuget.exe");

    /// <summary>
    /// Access the Chocolatey repository
    /// </summary>
    public Nuget Chocolatey => GetChocolatey();

    /// <summary />
    [Once]
    protected virtual Nuget GetChocolatey()
    {
        return Create(null, "https://chocolatey.org/api/v2/");
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
        string? version = null,
        string? executable = null
        )
    {
        var installDir = await Get(packageId, version);

        var path = executable == null
            ? GetDefaultExecutable(installDir, packageId)
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
        string? version = null
        )
    {
        var install = (await Tool())
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
        var dir = m.Groups[2].Value;

        string GetActualVersion()
        {
            m = Regex.Match(r.Output, @"Successfully installed '([^ ]+) ([^ ]+)' to");
            if (m.Success)
            {
                return m.Groups[2].Value;
            }
            else
            {
                m = Regex.Match(r.Output, $@"Package ""{packageId.ToLower()}\.([^""]+)"" is already installed.", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
            throw new InvalidOperationException($"Cannot determine version from\r\n{r.Output}");
        }

        var actualVersion = GetActualVersion();

        var installDir =
            actualVersion.Lineage(".").Reverse()
            .Select(versionDir => dir.Combine(new[] { packageId, versionDir }.Join(".")))
            .First(_ => _.IsDirectory());

        return installDir;
    }

    static string GetDefaultExecutable(string nugetPackageInstallDir, string packageId)
    {
        try
        {
            var candidates = nugetPackageInstallDir.Combine("tools")
                .Glob("*.exe").ToList();

            if (candidates.Count == 1)
            {
                return candidates.Single();
            }

            return candidates.Single(_ => _.FileNameWithoutExtension().Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e)
        {
            throw new System.IO.FileNotFoundException($@"Cannot determine default executable in {nugetPackageInstallDir}.

Files:
{nugetPackageInstallDir.Glob("**").Join()}", e);
        }
    }

    /// <summary />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S3427:Method overloads with default parameter values should not overlap ", Justification = "<Pending>")]
    protected Nuget(ITool? nuget = null, string? source = null)
    {
        tool = nuget;
        this.source = source;
    }

    /// <summary />
    protected Nuget()
    {
        // parameterless constructor is required for Castle.DynamicProxy
    }

    readonly ITool? tool;

    /// <summary>
    /// Create an instance where every method marked with [Once] is only executed once and its results are cached.
    /// </summary>
    /// <param name="nuget"></param>
    /// <returns></returns>
    public static Nuget Create(ITool? nuget = null, string? source = null)
    {
        return Once.Create<Nuget>(nuget, source);
    }
}
