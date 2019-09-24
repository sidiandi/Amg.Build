using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    class SourceCodeLayoutTests : TestBase
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Test]
        public async Task Get()
        {
            var testDir = CreateEmptyTestDirectory();
            var name = "hello";
            await testDir.Combine(name + ".cmd").WriteAllTextAsync(String.Empty);
            var srcDir = testDir.Combine(name);
            await srcDir.Combine(name + ".csproj").WriteAllTextAsync(String.Empty);
            await srcDir.Combine(name + ".cs").WriteAllTextAsync(String.Empty);
            var dll = await srcDir.Combine("bin", "debug", "netcoreapp2.1", name + ".dll").WriteAllTextAsync(String.Empty);

            var s = SourceCodeLayout.Get(dll);
            Assert.That(s != null);

            await s.Check();
        }
    }
}
