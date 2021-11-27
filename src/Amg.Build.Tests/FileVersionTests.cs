using Amg.Extensions;
using Amg.Test;
using NUnit.Framework;
using System.Reflection;

namespace Amg.FileSystem
{
    [TestFixture]
    public class FileVersionTests : TestBase
    {
        [Test]
        public async Task DllIsNewerThanSourceCode()
        {
            var dll = Assembly.GetExecutingAssembly().Location;
            var sourceDir = dll.Parent().Parent().Parent().Parent();
            var sourceDirVersion = await FileVersion.Get(sourceDir);
            if (sourceDirVersion == null)
            {
                throw new Exception();
            }
            Assert.That((await FileVersion.Get(dll))!.IsNewer(sourceDirVersion));

            sourceDirVersion.Dump().Write(Console.Out);

            var testDir = CreateEmptyTestDirectory();
            var jsonFile = testDir.Combine("version.json");
            await Json.Write<FileVersion>(jsonFile, sourceDirVersion);
            Console.WriteLine(await jsonFile.ReadAllTextAsync());
            var restored = await Json.Read<FileVersion>(jsonFile);
            Assert.That(restored, Is.EqualTo(sourceDirVersion));
        }
    }
}