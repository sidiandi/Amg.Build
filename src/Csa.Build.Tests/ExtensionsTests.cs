using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Csa.Build
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

            var table = Extensions.Table(cells);

            Assert.AreEqual(
@"Name  Number 
Hello 1      
H     1234234
", table.ToString());
        }
    }
}