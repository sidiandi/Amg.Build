using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
    class SourceCodeLayout
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string name;
        public string sourceFile;
        public string sourceDir;
        public string propsFile;
        public string csprojFile;
        public string cmdFile;
        public string dllFile;

        public async Task Check()
        {
            await CheckFileEnd(cmdFile, BootstrapperText);
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

        public string PropsText => @"<Project>
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
	<Configuration>Debug</Configuration>
	<AmgBuildVersion Condition=""$(AmgBuildVersion)==''"" >0.*</AmgBuildVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amg.Build"" Version=""$(AmgBuildVersion)"" />
  </ItemGroup>
</Project>
";

        public string BootstrapperText => Assembly.GetExecutingAssembly().Location.Parent().Combine("build.cmd.template").ReadAllTextAsync().Result;

        public static SourceCodeLayout Get(Type targetsType)
        {
            return Get(targetsType.Assembly.Location);
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
