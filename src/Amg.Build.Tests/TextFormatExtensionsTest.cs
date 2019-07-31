using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amg.Build
{
    [TestFixture]
    public class TextFormatExtensionsTest
    {
        [Test]
        public void ReduceLines()
        {
            var text = Enumerable.Range(0, 1000).Select(_ => $"Line {_}").Join();

            var shortened = text.ReduceLines(10,2);

            Console.WriteLine(shortened);

            Assert.That(shortened.ToString().SplitLines().Count(), Is.EqualTo(10));
        }
    }
}
