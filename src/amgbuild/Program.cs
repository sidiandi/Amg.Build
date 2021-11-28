using Amg.Build;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Amg.Extensions;
using Amg.FileSystem;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace amgbuild;

internal class Program
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    static int Main(string[] args)
    {
        return Runner.Run(args);
    }

    static string ResolveNewCmdFile(string? name)
    {
        var cmdFile = name == null
            ? defaultCmdFileName
            : name;

        if (!cmdFile.HasExtension(SourceCodeLayout.CmdExtension))
        {
            cmdFile = cmdFile + SourceCodeLayout.CmdExtension;
        }

        cmdFile = cmdFile.Absolute();

        return cmdFile;
    }

    [Once, Description("Create a new script")]
    public virtual async Task<string> New(string? scriptName = null)
    {
        var resolvedCmdFile = ResolveNewCmdFile(scriptName);
        var sourceLayout = await Amg.Build.SourceCodeLayout.Create(resolvedCmdFile, overwrite: Overwrite);
        Logger.Information("Amg.Build script {cmdFile} created.", sourceLayout.CmdFile);
        return sourceLayout.CmdFile;
    }

    const string defaultCmdFileName = "build.cmd";

    static string FindDefaultCmdFile(string startDirectory)
    {
        var c = startDirectory.Combine(defaultCmdFileName);
        if (c.IsFile())
        {
            return c;
        }

        foreach (var d in startDirectory.Up())
        {
            var p = d.ParentOrNull();
            if (p != null)
            {
                c = p.Combine(d.FileName() + SourceCodeLayout.CmdExtension);
                if (c.IsFile())
                {
                    return c;
                }
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(startDirectory),
            startDirectory,
            $"No {SourceCodeLayout.CmdExtension} file found in {startDirectory}");
    }

    static string FindExistingCmdFile(string? cmdFileSpec, string startDirectory)
    {
        return FindExistingCmdFileInternal(cmdFileSpec, startDirectory).Absolute();
    }

    static string FindExistingCmdFileInternal(string? cmdFileSpec, string startDirectory)
    {
        return cmdFileSpec.Map(spec =>
        {
            var cmdFile = spec.IsDirectory()
                ? spec.WithExtension(SourceCodeLayout.CmdExtension)
                : spec;

            if (!cmdFile.HasExtension(SourceCodeLayout.CmdExtension))
            {
                cmdFile = cmdFile + SourceCodeLayout.CmdExtension;
            }

            if (!cmdFile.IsFile())
            {
                throw new ArgumentOutOfRangeException(nameof(spec), spec, "File not found.");
            }

            return cmdFile;
        },
        () =>
        {
            return FindDefaultCmdFile(startDirectory);
        });
    }

    [Once, Description("overwrite existing files")]
    public virtual bool Overwrite { get; set; }

    [Once, Description("Fix a script")]
    public virtual Task Fix(string? script = null)
    {
        var source = SourceCode(script);
        Logger.Information("Fixing {CmdFile}", source.CmdFile);
        return FixInternal(source);
    }

    async Task FixInternal(SourceCodeLayout sourceLayout)
    {
        await sourceLayout.Fix();
    }

    [Once]
    protected virtual SourceCodeLayout SourceCode(string? spec)
        => ScriptSpecResolve.Resolve(spec, System.Environment.CurrentDirectory);

    [Once, Description("Print version")]
    public virtual async Task<string> Version()
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        return await Task.FromResult(version);
    }

    [Once, Description("Open in Visual Studio")]
    public virtual async Task Open(string? script = null)
    {
        var source = SourceCode(script);
        await Tools.Cmd.Run("start", source.CsprojFile);
    }

    [Once]
    protected virtual ITool DotnetTool(SourceCodeLayout sourceCode) => Tools.Default
        .WithFileName("dotnet.exe")
        .WithWorkingDirectory(sourceCode.SourceDir);

    [Once]
    protected virtual async Task<IEnumerable<string>> Pack(ITool dotnet)
    {
        var r = await dotnet.Run("pack");
        return r.Output.SplitLines().WhereMatch(new Regex(@"Successfully created package '([^']+)'."));
    }

    [Once, Description("Pack as dotnet tool")]
    public virtual async Task<string> Pack(string? script = null)
    {
        var source = SourceCode(script);
        return (await Pack(DotnetTool(source))).First();
    }

    [Once, Description("Install as global dotnet tool")]
    public virtual async Task Install(string? script = null)
    {
        var source = SourceCode(script);

        var nupkgFile = await Pack();
        var dotnet = DotnetTool(source);

        await dotnet.DoNotCheckExitCode()
            .Run("tool", "uninstall", "--global", source.Name);

        await dotnet.Run(
            "tool", "install",
            "--global",
            "--add-source", nupkgFile.Parent(),
            source.Name
            );
    }

    [Once, Description("Adds the script to the users PATH")]
    public virtual async Task<string> AddToPath(string? script = null)
    {
        var source = SourceCode(script);

        var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            .Combine(".dotnet", "tools");

        var shim = dir.Combine(source.CmdFile.FileName());

        return await shim
            .WriteAllTextAsync($@"@call {source.CmdFile.Quote()} %*");
    }
}
