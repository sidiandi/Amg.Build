using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class FileSystemExtensionsTests : TestBase
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            var f = d.Combine("c");
            f.EnsureParentDirectoryExists();
            Assert.That(Directory.Exists(d));
            Assert.That(f.Parent(), Is.EqualTo(d));
        }

        [Test]
        public void LastModified()
        {
            Console.WriteLine(".".Glob().LastWriteTimeUtc());
        }

        static string GetThisSourceFile([CallerFilePath] string path = null) => path;

        [Test]
        public void OutOfDate()
        {
            var thisDll = Assembly.GetExecutingAssembly().Location;
            var sources = GetThisSourceFile().Parent()
                .Glob("**")
                .Exclude("obj")
                .Exclude("bin");

            Assert.That(thisDll.IsOutOfDate(sources), Is.Not.True);
        }

        bool IsValidFilename(string f)
        {
            try
            {
                var d = this.CreateEmptyTestDirectory();
                var p = d.Combine(f);
                p.WriteAllTextAsync("a").Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [Test]
        public void Combine()
        {
            var combined = @"C:\temp\a\b\c";
            Assert.That(@"C:\temp".Combine("a", "b", "c"), Is.EqualTo(combined));
            Assert.That(@"C:\temp".Combine(@"a\b", "c"), Is.EqualTo(combined));
            Assert.That(@"C:\temp".Combine(@"a\b\c"), Is.EqualTo(combined));
            Assert.That(@"C:\temp".Combine(@"a/b/c"), Is.EqualTo(combined));
        }

        [Test]
        public void CombineChecksForValidFileNames()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var c = @"C:\temp".Combine("a", "b", new string('c', 1024));
            });
            Logger.Information("{exception}", e);
        }

        [Test]
        public void MakeValidFilename()
        {
            Assert.That("x".MakeValidFileName(), Is.EqualTo("x"));

            var invalid = new string(Path.GetInvalidFileNameChars());
            Assert.That(invalid.MakeValidFileName(), Is.EqualTo(new string('_', invalid.Length)));

            var tooLong = new string('a', 1024) + ".ext";
            Assert.That(tooLong.IsValidFileName(), Is.False);
            var shortened = tooLong.MakeValidFileName();
            Assert.That(shortened.IsValidFileName(), Is.True);
            Logger.Information("{shortened}", shortened);
            Assert.That(IsValidFilename(shortened));
            Assert.That(shortened.Extension(), Is.EqualTo(tooLong.Extension()));
        }
    }
}