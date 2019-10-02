using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
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
            await RebuildMyself.Build(layout.CsprojFile, layout.Configuration, layout.TargetFramework);

            var sourceInfo = RebuildMyself.GetSourceInfo(layout.DllFile);
            if (sourceInfo == null)
            {
                throw new NullReferenceException();
            }
            var sourceVersion = await RebuildMyself.GetCurrentSourceVersion(sourceInfo);
            if (sourceVersion == null) throw new NullReferenceException();

            Assert.That(await RebuildMyself.SourcesChanged(sourceInfo, sourceVersion), Is.False);
            await RebuildMyself.WriteSourceVersion(sourceInfo, sourceVersion);
            
            sourceVersion = await RebuildMyself.GetCurrentSourceVersion(sourceInfo);
            if (sourceVersion == null) throw new NullReferenceException();
            Assert.That(await RebuildMyself.SourcesChanged(sourceInfo, sourceVersion), Is.False);

            // add a file
            await sourceInfo.SourceDir.Combine("some.cs").WriteAllTextAsync("class Some{}");
            sourceVersion = await RebuildMyself.GetCurrentSourceVersion(sourceInfo);
            if (sourceVersion == null) throw new NullReferenceException();
            Assert.That(await RebuildMyself.SourcesChanged(sourceInfo, sourceVersion), Is.True);
        }
    }
}
