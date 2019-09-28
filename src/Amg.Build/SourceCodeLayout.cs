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
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SourceCodeLayout(string cmdFile)
        {
            this.cmdFile = cmdFile;
        }

        public string cmdFile {get;}
        public string rootDir => cmdFile.Parent();
        public string name => cmdFile.FileNameWithoutExtension();
        public string SourceDir => cmdFile.Parent().Combine(name);
        public string sourceFile => SourceDir.Combine(name + ".cs");
        public string csprojFile => SourceDir.Combine(name + ".csproj");
        public string propsFile => SourceDir.Combine("Amg.Build.props");
        public string dllFile => SourceDir.Parent().Parent().Combine("out", "Debug", "bin", name + ".dll");

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
            var existing = new[] { s.cmdFile, s.SourceDir }.Where(_ => _.Exists());
            if (existing.Any())
            {
                throw new Exception($"Already exists: {existing.Join(", ")}");
            }
            await Create(s.cmdFile, "name.cmd");
            await Create(s.csprojFile, "name.name.csproj");
            await Create(s.sourceFile, "name.name.cs");
            await Create(s.propsFile, "name.Amg.Build.props");
            return s;
        }

        public async Task Check()
        {
            await CheckFileEnd(cmdFile, BuildCmdText);
            await CheckFile(propsFile, ReadStringFromEmbeddedResource("Amg.Build.props"));
            var csproj = await csprojFile.ReadAllTextAsync();
            var propsLine = @"<Import Project=""Amg.Build.props"" />";
            if (!csproj.Contains(propsLine))
            {
                Logger.Warning("{csprojFile} must contain {propsLine}", csprojFile, propsLine);
            }
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

        public string PropsText => ReadStringFromEmbeddedResource("Amg.Build.props");

        public string BuildCmdText => ReadStringFromEmbeddedResource("build.cmd");

        static string ReadStringFromEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream(resourceFileName))
            {
                if (resource == null)
                {
                    throw new Exception(assembly.GetManifestResourceNames().Join());
                }
                return new StreamReader(resource).ReadToEnd();
            }
        }

        public static SourceCodeLayout Get(Type targetsType)
        {
            return FromDll(targetsType.Assembly.Location);
        }

        public async Task Fix()
        {
            await FixFile(cmdFile, BuildCmdText);
            await FixFile(propsFile, PropsText);
            await FixFile(csprojFile, BuildCsProjText);
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
        internal static SourceCodeLayout FromDll(string dllFile)
        {
            try
            {
                Logger.Dump(new { dllFile });

                var cmdFile = dllFile.Parent().Parent().Combine(dllFile.FileNameWithoutExtension() + ".cmd");
                var sourceCodeLayout = new SourceCodeLayout(cmdFile);

                var paths = new[] {
                    sourceCodeLayout.SourceDir,
                    sourceCodeLayout.cmdFile,
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

    internal class SourceCodeLayoutOutDir
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SourceCodeLayoutOutDir(string cmdFile)
        {
            this.cmdFile = cmdFile;
        }

        public string cmdFile { get; }
        public string rootDir => cmdFile.Parent();
        public string name => cmdFile.FileNameWithoutExtension();
        public string sourceDir => cmdFile.Parent().Combine(name);
        public string sourceFile => sourceDir.Combine(name + ".cs");
        public string csprojFile => sourceDir.Combine(name + ".csproj");
        public string propsFile => sourceDir.Combine("Amg.Build.props");
        public string dllFile => sourceDir.Combine("out", "bin", name + ".dll");

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
            var existing = new[] { s.cmdFile, s.SourceDir }.Where(_ => _.Exists());
            if (existing.Any())
            {
                throw new Exception($"Already exists: {existing.Join(", ")}");
            }
            await Create(s.cmdFile, "name.cmd");
            await Create(s.csprojFile, "name.name.csproj");
            await Create(s.sourceFile, "name.name.cs");
            await Create(s.propsFile, "name.Amg.Build.props");
            await Create(s.rootDir.Combine("out", "fastrun.cmd"), "name.out.fastrun.cmd");
            return s;
        }

        public async Task Check()
        {
            await CheckFileEnd(cmdFile, BuildCmdText);
            await CheckFile(propsFile, ReadStringFromEmbeddedResource("Amg.Build.props"));
            var csproj = await csprojFile.ReadAllTextAsync();
            var propsLine = @"<Import Project=""Amg.Build.props"" />";
            if (!csproj.Contains(propsLine))
            {
                Logger.Warning("{csprojFile} must contain {propsLine}", csprojFile, propsLine);
            }
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

        public string PropsText => ReadStringFromEmbeddedResource("Amg.Build.props");

        public string BuildCmdText => ReadStringFromEmbeddedResource("build.cmd");

        static string ReadStringFromEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream(resourceFileName))
            {
                if (resource == null)
                {
                    throw new Exception(assembly.GetManifestResourceNames().Join());
                }
                return new StreamReader(resource).ReadToEnd();
            }
        }

        public static SourceCodeLayout Get(Type targetsType)
        {
            return FromDll(targetsType.Assembly.Location);
        }

        public async Task Fix()
        {
            await FixFile(cmdFile, BuildCmdText);
            await FixFile(propsFile, PropsText);
            await FixFile(csprojFile, BuildCsProjText);
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
        internal static SourceCodeLayout FromDll(string dllFile)
        {
            try
            {
                Logger.Dump(new { dllFile });

                var cmdFile = dllFile.Parent().Parent()
                    .Parent().Parent()
                    .Combine("src", dllFile.FileNameWithoutExtension() + ".cmd");
                var sourceCodeLayout = new SourceCodeLayout(cmdFile);

                var paths = new[] {
                    sourceCodeLayout.SourceDir,
                    sourceCodeLayout.cmdFile,
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
