using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("amgbuild")]

namespace Amg.Build
{
    internal class SourceCodeLayout
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        public SourceCodeLayout(string cmdFile)
        {
            this.CmdFile = cmdFile;
        }

        public string CmdFile {get;}
        public string RootDir => CmdFile.Parent();
        public string Name => CmdFile.FileNameWithoutExtension();
        public string SourceDir => CmdFile.Parent().Combine(Name);
        public string SourceFile => SourceDir.Combine(Name + ".cs");
        public string CsprojFile => SourceDir.Combine(Name + ".csproj");
        public string PropsFile => SourceDir.Combine("Directory.Build.props");
        public string DllFile => SourceDir.Parent().Parent().Combine("out", "Debug", "bin", Name + ".dll");

        static async Task Create(string path, string templateName)
        {
            var text = ReadStringFromEmbeddedResource("Amg.Build.template." + templateName);
            if (path.Exists())
            {
                throw new Exception($"File {path} exists");
            }
            await path
                .EnsureParentDirectoryExists()
                .WriteAllTextIfChangedAsync(text);
        }

        public static async Task<SourceCodeLayout> Create(string cmdFilePath)
        {
            var s = new SourceCodeLayout(cmdFilePath);
            var existing = new[] { s.CmdFile, s.SourceDir }.Where(_ => _.Exists());
            if (existing.Any())
            {
                throw new Exception($"Already exists: {existing.Join(", ")}");
            }
            await Create(s.CmdFile, "name.cmd");
            await Create(s.CsprojFile, "name.name.csproj");
            await Create(s.SourceFile, "name.name.cs");
            await Create(s.PropsFile, "name.Directory.Build.props");
            return s;
        }

        public async Task Check()
        {
            await CheckFileEnd(CmdFile, BuildCmdText);
            await CheckFile(PropsFile, PropsText);
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

        public string PropsText => ReadStringFromEmbeddedResource("Directory.Build.props");

        public string BuildCmdText => ReadStringFromEmbeddedResource("build.cmd");

        static string ReadStringFromEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream(resourceFileName))
            {
                if (resource == null)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(resourceFileName), 
                        resourceFileName,
                        $"available resources:\r\n{ assembly.GetManifestResourceNames().Join()}");
                }
                return new StreamReader(resource).ReadToEnd();
            }
        }

        public static SourceCodeLayout? Get(Type targetsType)
        {
            return FromDll(targetsType.Assembly.Location);
        }

        public async Task Fix()
        {
            await FixFile(CmdFile, BuildCmdText);
            await FixFile(PropsFile, PropsText);
        }

        string BuildCsProjText => ReadStringFromEmbeddedResource("build.csproj.template");

        async Task FixFile(string file, string expected)
        {
            Logger.Information("Write {file}", file);
            await file
                .EnsureParentDirectoryExists()
                .WriteAllTextIfChangedAsync(expected);
        }

        /// <summary>
        /// Try to determine the source directory from which the assembly of targetType was built.
        /// </summary>
        /// <returns></returns>
        internal static SourceCodeLayout? FromDll(string dllFile)
        {
            try
            {
                Logger.Dump(new { dllFile });

                var cmdFile = dllFile.Parent().Parent().Combine(dllFile.FileNameWithoutExtension() + ".cmd");
                var sourceCodeLayout = new SourceCodeLayout(cmdFile);

                var paths = new[] {
                    sourceCodeLayout.SourceDir,
                    sourceCodeLayout.CmdFile,
                }.Select(path => new { path, exists = path.Exists() })
                .ToList();

                Logger.Information("{@paths}", paths);
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
