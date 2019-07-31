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
        [Test]
        public async Task Glob()
        {
            var testDir = CreateEmptyTestDirectory();
            await testDir.Combine("hello").WriteAllTextAsync("hello");
            await testDir.Combine("a", "b", "c").WriteAllTextAsync("hello");

            IList<string> files;

            files = testDir.Glob().Include("**")
                .Exclude("he*o")
                .EnumerateFiles()
                .ToList();
            Console.WriteLine(files.Join());
            Assert.AreEqual(files.Count, 1);

            files = testDir.Glob().Include("**").ToList();
            Console.WriteLine(files.Join());
            Assert.AreEqual(files.Count, 5);

            files = testDir.Glob().Include("**")
                .Exclude("b")
                .ToList();
            Console.WriteLine(files.Join());
            Assert.AreEqual(files.Count, 3);

            files = testDir.Glob().Include("**")
                .EnumerateFiles()
                .ToList();
            Console.WriteLine(files.Join());
            Assert.AreEqual(files.Count, 2);
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