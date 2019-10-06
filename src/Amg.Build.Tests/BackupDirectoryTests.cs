using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    class BackupDirectoryTests : TestBase
    {
        [Test]
        public async Task Backup()
        {
            var testDir = CreateEmptyTestDirectory();
            var d = testDir.Combine("project");
            var p = await d.Combine("a", "b", "c").Touch();
            var b = new BackupDirectory(d);
            var backupLocation = b.Move(p);
            Assert.That(!p.Exists());
            Assert.That(backupLocation.Exists());
        }
    }
}
