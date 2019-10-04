using NUnit.Framework;
using System;
using System.Diagnostics;
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

            s.TempAssemblyFile.Parent().Move(s.AssemblyFile.Parent().EnsureParentDirectoryExists());
            Assert.That(s.AssemblyFile.IsFile());
        }

        [Test]
        public void MoveToArgs()
        {
            var testDir = CreateEmptyTestDirectory();

            var move = new RebuildMyself.MoveTo
            {
                source = testDir.Combine("source"),
                dest = testDir.Combine("dest")
            };

            var si = new ProcessStartInfo();
            RebuildMyself.SetMoveToArgs(move, si);
            try
            {
                System.Environment.SetEnvironmentVariable(RebuildMyself.MoveToKey, si.Environment[RebuildMyself.MoveToKey]);
                var move1 = RebuildMyself.GetMoveToArgs()!;
                Assert.AreEqual(move.source, move1.source);
                Assert.AreEqual(move.dest, move1.dest);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(RebuildMyself.MoveToKey, null);
            }
        }
    }
}
