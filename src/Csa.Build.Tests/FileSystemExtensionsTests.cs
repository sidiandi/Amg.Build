using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Csa.Build
{
    [TestFixture]
    public class FileSystemExtensionsTests : TestBase
    {
        [Test]
        public async Task EnsureDirectoryIsEmpty()
        {
            var testDir = CreateEmptyTestDirectory();
            var git = new Tool("git.exe");
            await git.Run("init", testDir);
            testDir.EnsureDirectoryIsEmpty();
        }

        [Test]
        public void ParentDirectory()
        {
            var testDir = CreateEmptyTestDirectory();
            var d = Path.Combine(testDir, "a", "b");
            var f = d.CatDir("c");
            f.EnsureParentDirectoryExists();
            Assert.That(Directory.Exists(d));
            Assert.That(f.GetParentDirectory(), Is.EqualTo(d));
        }
    }
}