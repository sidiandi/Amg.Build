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
        public async Task Create()
        {
            var testDir = CreateEmptyTestDirectory();

            var name = "hello";
            var cmdFile = testDir.Combine(name + ".cmd");
            var s = await SourceCodeLayout.Create(cmdFile);
            Assert.That(s != null);
            await s.Check();
        }
    }
}
