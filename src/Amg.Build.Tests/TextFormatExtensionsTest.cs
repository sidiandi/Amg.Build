using NUnit.Framework;
using System;
using System.Linq;

namespace Amg.Build
{
    [TestFixture]
    public class TextFormatExtensionsTest
    {
        [Test]
        public void ReduceLines()
        {
            var text = Enumerable.Range(0, 1000).Select(_ => $"Line {_}").Join();

            var shortened = text.ReduceLines(10,2).ToString();
            Console.WriteLine(shortened);
            Assert.That(shortened.SplitLines().Count(), Is.EqualTo(10));
        }

        [Test]
        public void Metric()
        {
            Console.WriteLine(
            Enumerable.Range(-16, 38).Select(_ => Math.Pow(10.0, _))
                .Select(_ => new
                {
                    Value = _,
                    Format = _.Metric()
                })
                .ToTable(header:true));

            Assert.AreEqual("100 kilo", 100e3.Metric());
            Assert.AreEqual("100 pico", 100e-12.Metric());
        }
    }
}
