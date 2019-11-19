using Amg.Extensions;
using Amg.FileSystem;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("amgbuild")]

namespace Amg.Build
{
    internal class SourceCodeLayout
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);
        public static string CmdExtension => ".cmd";

        public SourceCodeLayout(string cmdFile)
        {
            if (!cmdFile.HasExtension(CmdExtension))
            {
                Logger.Information(nameof(cmdFile), cmdFile, $"Must have extension {SourceCodeLayout.CmdExtension}");
            }
            this.CmdFile = cmdFile;
        }

        public string CmdFile {get;}
        public string RootDir => CmdFile.Parent();
        public string Name => CmdFile.FileNameWithoutExtension();
        public string Namespace => Name.ToCsharpIdentifier();
        public string SourceDir => CmdFile.Parent().Combine(Name);
        public string ProgramCs => SourceDir.Combine("Program.cs");
        public string CsprojFile => SourceDir.Combine(Name + ".csproj");
        public string DllFile => SourceDir.Combine("bin", Configuration, TargetFramework, Name + ".dll");
        public string Configuration => "Debug";
        public string TargetFramework => "netcoreapp3.0";

        static async Task Create(string path, string templateName, BackupDirectory? backup)
        {
            var text = ReadTemplate(templateName);
            await CreateFromText(path, text, backup);
        }

        static async Task CreateFromText(string path, string text, BackupDirectory? backup)
        {
            if (path.Exists())
            {
                if (backup == null)
                {
                    throw new System.IO.IOException($"File {path} exists");
                }
                else
                {
                    await backup.Move(path);
                }
            }
            Logger.Information("Write {path}", path);
            await path
                .EnsureParentDirectoryExists()
                .WriteAllTextIfChangedAsync(text);
        }

        public static async Task<SourceCodeLayout> Create(string cmdFilePath, bool overwrite = false)
        {
            var s = new SourceCodeLayout(cmdFilePath);
            var existing = new[] { s.CmdFile, s.SourceDir }.Where(_ => _.Exists());
            if (!overwrite && existing.Any())
            {
                throw new System.IO.IOException($"Cannot create because these files already exist: {existing.Join(", ")}");
            }

            var backup = overwrite
                ? new BackupDirectory(s.RootDir)
                : null;

            await Create(s.CmdFile, "name.cmd", backup);
            await Create(s.CsprojFile, "name.name.csproj", backup);
            await CreateFromText(s.ProgramCs, s.ProgramCsText, backup);
            await Create(s.SourceDir.Combine(".gitignore"), "name..gitignore", backup);
            var dotnet = (await Once.Create<Dotnet>().Tool())
                .WithWorkingDirectory(s.SourceDir);
            await dotnet.Run("add", "package", "Amg.Build");
            return s;
        }

        public async Task Check()
        {
            await CheckFileEnd(CmdFile, BuildCmdText);
        }

        async Task CheckFile(string file, string expected)
        {
            if (!string.Equals(await file.ReadAllTextAsync(), expected))
            {
                Logger.Warning(@"{cmdFile} does not have the expected contents
====
{expected}
====
", file, expected);
            }
        }

        async Task CheckFileEnd(string file, string expectedEnd)
        {
            var fileText = await file.ReadAllTextAsync();
            if (fileText == null)
            {
                fileText = String.Empty;
            }

            if (!fileText.EndsWith(expectedEnd))
            {
                Logger.Warning(@"{file} does not end with
====
{expectedEnd}
====
", file, expectedEnd);
            }
        }

        string NugetVersion => Assembly.GetExecutingAssembly().NugetVersion();

        string PropsText => ReadTemplate("name.Directory.Build.props")
            .Replace("{AmgBuildVersion}", NugetVersion);

        string ProgramCsText => ReadTemplate("name.Program.cs")
            .Replace("ReplaceWithName", Namespace);

        string BuildCmdText => ReadTemplate("name.cmd");

        static string ReadTemplate(string templateName)
        {
            return ReadStringFromEmbeddedResource("Amg.Build.template." + templateName);
        }

        static string ReadStringFromEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream(resourceFileName))
            {
                if (resource == null)
                {
                    var available = assembly.GetManifestResourceNames();
                    throw new ArgumentOutOfRangeException(
                        nameof(resourceFileName), 
                        resourceFileName,
                        $"available resources:\r\n{ available.Join()}");
                }
                return new StreamReader(resource).ReadToEnd();
            }
        }

        public static SourceCodeLayout? Get(object commandObject)
        {
            return GetFromType(commandObject.GetType());
        }

        public static SourceCodeLayout? GetFromType(Type type)
        {
            var assembly = type.Assembly;
            if (assembly.IsDynamic)
            {
                var interfaces = type.GetInterfaces();
                return GetFromType(interfaces.First(_ => !_.Assembly.IsDynamic));
            }
            else
            {
                return FromDll(assembly.Location);
            }
        }

        public async Task Fix()
        {
            var backup = new BackupDirectory(this.CmdFile.Parent());
            await FixFile(CmdFile, BuildCmdText, backup);

            // delete old Directory.props file
            SourceDir.Combine("Directory.props").EnsureFileNotExists();

            // delete old Amg.Build.props file
            SourceDir.Combine("Amg.Build.props").EnsureFileNotExists();
        }

        string BuildCsProjText => ReadTemplate("build.csproj.template");

        async Task FixFile(string file, string expected, BackupDirectory backup)
        {
            var actualText = await file.ReadAllTextAsync();
            if (!object.Equals(expected, actualText))
            {
                await backup.Move(file);
                Logger.Information("Writing {file}", file);
                await file
                    .EnsureParentDirectoryExists()
                    .WriteAllTextIfChangedAsync(expected);
            }
        }

        static string? GetCmdFile(string dllFile)
        {
            var cmd = dllFile.Parent().Parent().Parent().Parent().Parent()
                .Combine(dllFile.FileNameWithoutExtension() + CmdExtension);
            if (cmd.IsFile())
            {
                return cmd;
            }

            cmd = dllFile.Parent().Parent().Parent().Parent()
                .Combine(dllFile.FileNameWithoutExtension() + CmdExtension);
            if (cmd.IsFile())
            {
                return cmd;
            }
            return null;
        }

        /// <summary>
        /// Try to determine the source directory from which the assembly of targetType was built.
        /// </summary>
        /// <returns></returns>
        internal static SourceCodeLayout? FromDll(string dllFile)
        {
            try
            {
                Logger.Debug(new { dllFile });

                var cmdFile = GetCmdFile(dllFile);
                if (cmdFile == null)
                {
                    Logger.Debug("no cmd file found for {dllFile}", dllFile);
                    return null;
                }
                    
                var sourceCodeLayout = new SourceCodeLayout(cmdFile);

                var paths = new[] {
                    sourceCodeLayout.SourceDir,
                    sourceCodeLayout.CmdFile,
                }.Select(path => new { path, exists = path.Exists() })
                .ToList();

                Logger.Debug(paths);
                var hasSources = paths.All(_ => _.exists);
                if (hasSources)
                {
                    Logger.Debug("sources: {@sourceCodeLayout}", sourceCodeLayout);
                }
                else
                {
                    Logger.Warning("Files not found: {@filesNotFound}", paths.Where(_ => !_.exists));
                }
                return hasSources ? sourceCodeLayout : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
