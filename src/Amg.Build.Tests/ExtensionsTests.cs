using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void Quote()
        {
            Assert.AreEqual(@"""Hello, \""World\""""", @"Hello, ""World""".Quote());
        }

        [Test]
        public void Md5Checksum()
        {
            var c = "hello".Md5Checksum();
            Assert.AreEqual(c, "5d41402abc4b2a76b9719d911017c592");
        }

        [Test]
        public void Dump()
        {
            var o = new { A = 1, B = "2", C = 3.0 };
            Assert.AreEqual(@"A: 1
B: 2
C: 3
", o.Dump().ToString());
        }

        [Test]
        public void PrintTable()
        {
            var cells = new[]
            {
            new[]{"Name", "Number"},
            new[]{"Hello", "1"},
            new[]{"H", "1234234"}
        };

            var table = cells.Table();

            Assert.AreEqual(
@"Name  Number 
Hello 1      
H     1234234
", table.ToString());
        }

        [Test]
        public void IsAbbreviation()
        {
            Assert.IsTrue("BMW".IsAbbreviation("Bayerische Motorenwerke"));
            Assert.IsTrue("ga".IsAbbreviation("greet-all"));
            Assert.IsTrue("g-a".IsAbbreviation("greet-all"));
            Assert.IsTrue("p".IsAbbreviation("print"));
            Assert.IsFalse("r".IsAbbreviation("print"));
        }
    }
}