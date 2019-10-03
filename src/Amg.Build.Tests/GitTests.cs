using NUnit.Framework;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class GitTests : TestBase
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        [Test]
        public async Task WorkWithGit()
        {
            var testDir = CreateEmptyTestDirectory();
            Logger.Information(testDir);
            var git = Git.Create(testDir);
            var g = git.GitTool;
            await g.Run("init");
            await testDir.Combine("hello").WriteAllTextAsync("hello");
            await g.Run("add", ".");
            await g.Run("commit", "-mTest", "-a");
            int count = 0;
            for (int i = 0; i < 3; ++i)
            {
                await git.RebuildIfCommitHashChanged(async () =>
                {
                    await Task.CompletedTask;
                    ++count;
                }, testDir + ".state");
            }
            Assert.That(count, Is.EqualTo(1));
        }
    }
}
