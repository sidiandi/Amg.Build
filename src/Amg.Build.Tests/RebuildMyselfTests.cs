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
            var assembly = await RebuildMyself.ProvideAssembly(s, version);
            Console.WriteLine(s.SourceDir.Glob("**/*").EnumerateFiles().Join());
            Assert.That(assembly.IsFile(), () => s.Dump().ToString());
        }
    }
}
