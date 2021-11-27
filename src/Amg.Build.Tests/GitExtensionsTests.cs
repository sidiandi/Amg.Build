using NUnit.Framework;
using Amg.FileSystem;

namespace Amg.Build
{
    [TestFixture]
    public class GitExtensionsTests : TestBase
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        [Test, TestCase(""), TestCase("src")]
        public async Task Rebuild(string relativeSourceDir)
        {
            var testDir = this.CreateEmptyTestDirectory();
            var gitHelper = Git.Create(testDir);
            var git = gitHelper.GitTool;
            await git.Run("init");
            var sourceDir = testDir.Combine(relativeSourceDir);
            var sourceFile = sourceDir.Combine("hello.txt");
            await sourceFile.WriteAllTextAsync("world");
            await git.Run("add", ".");
            await git.Run("commit", "-a", "-m", "first commit");

            var output = testDir.Combine("out", "greeting");
            int count = 0;
            for (int i = 0; i < 3; ++i)
            {
                output = await gitHelper.IfChanged(output, sourceDir, async () =>
                {
                    ++count;
                    var text = await sourceFile.ReadAllTextAsync();
                    await output.WriteAllTextAsync(text!);
                });
            }

            Assert.AreEqual(1, count);

            await sourceFile.WriteAllTextAsync("hello world");
            await git.Run("add", ".");
            await git.Run("commit", "-a", "-m", "now greeting world");

            for (int i = 0; i < 3; ++i)
            {
                output = await gitHelper.IfChanged(output, sourceDir, async () =>
                {
                    ++count;
                    await output.WriteAllTextAsync((await sourceFile.ReadAllTextAsync())!);
                });
            }
            Assert.AreEqual(2, count);

        }
    }
}