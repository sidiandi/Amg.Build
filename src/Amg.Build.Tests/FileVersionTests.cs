using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class FileVersionTests : TestBase
    {
        [Test]
        public async Task DllIsNewerThanSourceCode()
        {
            var dll = Assembly.GetExecutingAssembly().Location;
            var sourceDir = dll.Parent().Parent().Parent().Parent();
            var sourceDirVersion = FileVersion.Get(sourceDir);
            if (sourceDirVersion == null)
            {
                throw new Exception();
            }
            Assert.That(FileVersion.Get(dll)!.IsNewer(sourceDirVersion));

            var testDir = CreateEmptyTestDirectory();
            var jsonFile = testDir.Combine("version.json");
            Json.Write<FileVersion>(jsonFile, sourceDirVersion);
            Console.WriteLine(await jsonFile.ReadAllTextAsync());
            var restored = Json.Read<FileVersion>(jsonFile);
            Assert.That(restored, Is.EqualTo(sourceDirVersion));
        }
    }
}
