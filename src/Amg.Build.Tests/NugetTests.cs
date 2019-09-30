using NUnit.Framework;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class NugetTests
    { 
        [Test]
        public async Task GetPackageContent()
        {
            var nuget = Nuget.Create();
            var sevenZip = await nuget.Get(
                "7-Zip.CommandLine",
                version: "18.1.0");
            Assert.That(sevenZip.IsDirectory());
        }

        [Test]
        public async Task DownloadTool()
        {
            var nuget = Runner.Once<Nuget>();
            var sevenZip = await nuget.GetTool(
                "7-Zip.CommandLine",
                version: "18.1.0",
                executable: "tools/x64/7za.exe");
            await sevenZip.Run();
        }

        [Test]
        public async Task DownloadToolDefaultExe()
        {
            var nuget = Nuget.Create();
            var sevenZip = await nuget.GetTool("7-Zip.CommandLine");
            await sevenZip.Run();
        }

        [Test]
        public async Task DownloadToolFromChocolatey()
        {
            var choco = Nuget.Create().Chocolatey;
            var busyBox = await choco.GetTool("busybox");
            await busyBox.Run();
        }
    }
}