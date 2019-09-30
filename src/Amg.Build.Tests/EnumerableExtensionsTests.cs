using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Amg.Build
{
    [TestFixture]
    public class EnumerableExtensionsTests : TestBase
    {
        [Test]
        public void Progress()
        {
            Enumerable.Range(0, 100).Progress(
                metric: _ => 1000.0,
                metricUnit: "Bytes",
                description: "Testing...",
                updateInterval: TimeSpan.FromSeconds(0.01)
                )
                .Select(_ =>
                {
                    Thread.Sleep(10);
                    return _;
                }).ToList();
        }

        [Test]
        public void NotNull()
        {
            var e = new[] { "a", "b", "c", null };
            var nne = e.NotNull();
            Assert.That(nne.SequenceEqual(new string[] { "a", "b", "c" }));
        }

        [Test]
        public void Pad()
        {
            var originalCount = 3;
            var e = Enumerable.Range(0, originalCount).Select(_ => _.ToString());
            var padCount = 10;
            var padded = e.Pad(padCount).ToList();
            Assert.That(padded.Count, Is.EqualTo(padCount));
            Assert.That(padded.NotNull().SequenceEqual(e));
        }
    }
}
