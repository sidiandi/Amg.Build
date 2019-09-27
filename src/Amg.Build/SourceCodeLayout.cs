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
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string name;
        public string sourceFile;
        public string sourceDir;
        public string propsFile;

        public static async Task<SourceCodeLayout> Create(string cmdFilePath)
        {
            var s = new SourceCodeLayout
            {
                cmdFile = cmdFilePath,
                name = cmdFilePath.FileName()
            };

            s.sourceDir = cmdFilePath.Parent().Combine(s.name);
            s.sourceFile = s.sourceDir.Combine($"{s.name}.cs");
            s.csprojFile = s.sourceDir.Combine($"{s.name}.csproj");
            s.propsFile = s.sourceDir.Combine("Amg.Build.props");

            Logger.Information("{@sourceLayout}", s); ;
            await s.Fix();

            return s;
        }

        public string csprojFile;
        public string cmdFile;
        public string dllFile;

        public async Task Check()
        {
            await CheckFileEnd(cmdFile, BuildCmdText);
            await CheckFile(propsFile, PropsText);
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
            using (var resource = assembly.GetManifestResourceStream($"Amg.Build.{resourceFileName}"))
            {
                return new StreamReader(resource).ReadToEnd();
            }
        }

        public static SourceCodeLayout Get(Type targetsType)
        {
            return Get(targetsType.Assembly.Location);
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
        /// build.cmd
        /// build\build.cs
        /// build\build.csproj
        /// build\bin\Debug\netcoreapp2.2\build.dll
        /// <returns></returns>
        internal static SourceCodeLayout Get(string dllFile)
        {
            try
            {
                var sourceCodeLayout = new SourceCodeLayout
                {
                    dllFile = dllFile
                };
                sourceCodeLayout.name = sourceCodeLayout.dllFile.FileNameWithoutExtension();
                sourceCodeLayout.sourceDir = sourceCodeLayout.dllFile.Parent().Parent().Parent().Parent();
                sourceCodeLayout.sourceFile = sourceCodeLayout.sourceDir.Combine($"{sourceCodeLayout.name}.cs");
                sourceCodeLayout.csprojFile = sourceCodeLayout.sourceDir.Combine($"{sourceCodeLayout.name}.csproj");
                sourceCodeLayout.propsFile = sourceCodeLayout.sourceDir.Combine("Amg.Build.props");
                sourceCodeLayout.cmdFile = sourceCodeLayout.sourceDir.Parent().Combine($"{sourceCodeLayout.name}.cmd");

                var paths = new[] {
                    sourceCodeLayout.sourceDir,
                    sourceCodeLayout.sourceFile,
                    sourceCodeLayout.cmdFile,
                    sourceCodeLayout.csprojFile
                }.Select(path => new { path, exists = path.Exists() })
                .ToList();

                Logger.Debug("{@paths}", paths);
                var hasSources = paths.All(_ => _.exists);
                if (hasSources)
                {
                    Logger.Debug("sources: {@sourceCodeLayout}", sourceCodeLayout);
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
