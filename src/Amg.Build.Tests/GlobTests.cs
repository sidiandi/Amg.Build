using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class GlobTests : TestBase
    {
        string TestDir => _testDir!;

        string? _testDir;

        [OneTimeSetUp]
        public async Task CreateTestDir()
        {
            _testDir = CreateEmptyTestDirectory();
            await TestDir.Combine("hello.txt").WriteAllTextAsync("hello");
            await TestDir.Combine("a", "b", "c").WriteAllTextAsync("hello");
        }

        [Test]
        public void EmptyGlobReturnsEmpty()
        {
            Assert.That(!TestDir.Glob().Any());
        }

        /*
         globstar
    If set, the pattern ** used in a pathname expansion context will
    match all files and zero or more directories and subdirectories.
    If the pattern is followed by a /, only directories and
    subdirectories match.
         */

        [Test]
        public void Globstar()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("**"),
                new[]
                {
                    TestDir.Combine("a"),
                    TestDir.Combine("hello.txt"),
                    TestDir.Combine(@"a\b"),
                    TestDir.Combine(@"a\b\c")
                });
        }

        [Test]
        public void GlobstarExclude()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("**").Exclude("b"),
                new[]
                {
                    TestDir.Combine("a"),
                    TestDir.Combine("hello.txt")
                });
        }

        [Test]
        public void ExcludeSubPaths()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("**").Exclude("a/b"),
                new[]
                {
                    TestDir.Combine("a"),
                    TestDir.Combine("hello.txt")
                });
        }

        [Test]
        public void Match()
        {
            var e = Enumerable.Range(0, 100);
            Assert.That(Glob.Match(e, Enumerable.Range(0, 3).Select(c => new Func<int, bool>(_ => c == _))));
            Assert.That(Glob.Match(e, Enumerable.Range(99, 1).Select(c => new Func<int, bool>(_ => c == _))));
            Assert.That(Glob.Match(e, Enumerable.Range(50, 3).Select(c => new Func<int, bool>(_ => c == _))));
            Assert.That(!Glob.Match(e, Enumerable.Range(0, 3).Select(c => new Func<int, bool>(_ => 2*c == _))));
        }

        [Test]
        public void ExcludeFunc()
        {
            var g = new Glob(@"C:\temp");
            var f = g.ExcludeFuncFromWildcard("a/b");
            Assert.That(f(new FileInfo(@"C:\temp\a\b")));
        }

        [Test]
        public void FindFileRecursive()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("**/c"),
                new[]
                {
                    TestDir.Combine("a\\b\\c")
                });
        }

        [Test]
        public void FindFileRecursive2()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("**/b/**/c"),
                new[]
                {
                    TestDir.Combine("a/b/c")
                });
        }

        [Test]
        public void Wildcard()
        {
            AssertSequencesAreEqual(
                TestDir.Glob("*.txt"),
                new[]
                {
                    TestDir.Combine("hello.txt")
                });
        }

        [Test]
        public void RootDoesNotExistsReturnsEmpty()
        {
            AssertSequencesAreEqual(
                TestDir.Combine("not_exists").Glob(),
                new string[]
                {
                });
        }

        private static void AssertSequencesAreEqual<T>(IEnumerable<T> actual, IEnumerable<T> expected) where T: class
        {
            Assert.That(actual.SequenceEqual(expected), () => $@"Sequences do not match.

{actual.ZipOrDefault(expected, (a, e) => new { actual = a, expected = e}).ToTable()}
");
        }

        [Test]
        public void RegexFromWildcard()
        {
            var re = Amg.Build.Glob.RegexFromWildcard("he*");
            Assert.AreEqual(re.ToString(), "^he.*$");
            Assert.That(re.IsMatch("hello"));
            Assert.That(re.IsMatch("sayhello"), Is.Not.True);

        }
    }
}
 