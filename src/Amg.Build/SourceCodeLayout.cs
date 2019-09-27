using System;
using System.Collections.Generic;
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

        public string cmdFile;
        public string name => cmdFile.FileNameWithoutExtension();
        public string sourceFile => sourceDir.Combine(name + ".cs");
        public string sourceDir => cmdFile.Parent().Combine(name);
        public string propsFile => sourceDir.Combine("Amg.Build.props");
        public string csprojFile => sourceDir.Combine(name + ".csproj");
        public string dllFile => sourceDir.Combine("out", name + ".dll");
        public string fastDotnetRunFile => sourceDir.Combine("fast-dotnet-run.cmd");

        public SourceCodeLayout(string cmdFile)
        {
            this.cmdFile = cmdFile;
        }

        public static async Task<SourceCodeLayout> Create(string cmdFilePath)
        {
            var s = new SourceCodeLayout(cmdFilePath);

            var existing = new[]
            {
                s.sourceDir,
                s.cmdFile,
            }.Where(_ => _.Exists());

            if (existing.Any())
            {
                throw new Exception($"Cannot create script because these files already exist: {existing.Join(", ")}");
            }

            await s.Fix();
            await s.FixFile(s.sourceFile, ReadStringFromEmbeddedResource("name_cs"));

            return s;
        }

        string CmdFileText => ReadStringFromEmbeddedResource("name.cmd");
        string PropsFileText => ReadStringFromEmbeddedResource("Amg.Build.props");

        public async Task Check()
        {
            await CheckFileEnd(cmdFile, CmdFileText);
            await CheckFile(propsFile, PropsFileText);
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

        static string ReadStringFromEmbeddedResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream($"Amg.Build.template.{resourceFileName}"))
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
            await FixFile(cmdFile, CmdFileText);
            await FixFile(propsFile, PropsFileText);
            await FixFile(csprojFile, ReadStringFromEmbeddedResource("name.csproj"));
            await FixFile(fastDotnetRunFile, FastDotnetRunFileText);
        }

        string FastDotnetRunFileText => ReadStringFromEmbeddedResource("fast-dotnet-run.cmd");

        string BuildCsProjText => ReadStringFromEmbeddedResource("name.csproj");

        async Task FixFile(string file, string expected)
        {
            Logger.Information("Write {file}", file);
            await file
                .EnsureParentDirectoryExists()
                .WriteAllTextIfChangedAsync(expected);
        }

        IEnumerable<string> Files => new[] {
            cmdFile,
            csprojFile,
            propsFile,
            fastDotnetRunFile
        };

        /// <summary>
        /// Try to determine the source directory from which the assembly of targetType was built.
        /// </summary>
        /// build.cmd
        /// build\build.cs
        /// build\build.csproj
        /// build\bin\Debug\netcoreapp2.1\build.dll
        /// <returns></returns>
        internal static SourceCodeLayout FromDll(string dllFile)
        {
            try
            {
                var cmdFile = dllFile.Absolute().Parent().Parent().Parent().Combine(dllFile.FileNameWithoutExtension() + ".cmd");
                var sourceCodeLayout = new SourceCodeLayout(cmdFile);

                var paths = sourceCodeLayout.Files
                    .Select(path => new { path, exists = path.Exists() })
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
