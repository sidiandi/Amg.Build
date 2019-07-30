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

            var files = testDir.Glob().Include("**").ToList();
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
    }
}