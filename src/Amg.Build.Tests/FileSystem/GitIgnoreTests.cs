using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Amg.FileSystem
{
    [TestFixture]
    public class GitIgnoreTests
    {
        [Test]
        public void Ignore()
        {
            var gitIgnore = GitIgnore.Create();

            var sourceDir = LibGit2Sharp.Repository.Discover(GetSourceFile())
                .Parent();

            Assert.That(gitIgnore.IsIgnored(sourceDir.Combine("bin")));
            Assert.That(!gitIgnore.IsIgnored(sourceDir.Combine("hello.cs")));
        }

        static string GetSourceFile([CallerFilePath] string? sourceFile = null)
            => sourceFile!;

        [Test]
        public void Lib2GitSharpBehaviour()
        {
            var r = new Repository(LibGit2Sharp.Repository.Discover(GetSourceFile()));
            Assert.That(r.Ignore.IsPathIgnored("out"));
            Assert.That(!r.Ignore.IsPathIgnored(@"out\Release\Version.props"));
            Assert.That(r.Ignore.IsPathIgnored(@"out/Release/Version.props"));
            Assert.That(!r.Ignore.IsPathIgnored("Readme.md"));
        }
    }
}
