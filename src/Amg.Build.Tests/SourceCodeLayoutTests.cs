using NUnit.Framework;
using Amg.FileSystem;

namespace Amg.Build;

[TestFixture]
class SourceCodeLayoutTests : TestBase
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    [Test]
    public async Task Create()
    {
        var testDir = CreateEmptyTestDirectory();
        var name = "hello";
        var cmdFile = testDir.Combine(name + SourceCodeLayout.CmdExtension);
        var s = await SourceCodeLayout.Create(cmdFile);
        Assert.That(s.CsprojFile.IsFile());

        // cannot create again, because files exist.
        Assert.Throws<AggregateException>(() => SourceCodeLayout.Create(cmdFile).Wait());
    }
}
