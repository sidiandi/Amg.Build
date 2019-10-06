using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class RebuildMyselfTests : TestBase
    {
        [Test]
        public async Task Rebuild()
        {
            var testDir = CreateEmptyTestDirectory();
            var cmd = testDir.Combine("hello.cmd");
            var layout = await SourceCodeLayout.Create(cmd);
            var s = RebuildMyself.GetSourceInfo(layout.DllFile)!;

            var version = (await FileVersion.Get(s.SourceDir))!;
            Assert.That(!s.TempAssemblyFile.IsFile());
            await RebuildMyself.Build(
                s.CsprojFile,
                version,
                s.Configuration,
                s.TargetFramework,
                s.TempAssemblyFile.Parent()
                );
            Console.WriteLine(s.SourceDir.Glob("**/*").EnumerateFiles().Join());
            Assert.That(s.TempAssemblyFile.IsFile(), () => s.Dump().ToString());

            await s.TempAssemblyFile.Parent().Move(s.AssemblyFile.Parent().EnsureParentDirectoryExists());
            Assert.That(s.AssemblyFile.IsFile());
        }
    }
}
