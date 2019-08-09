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
    }
}
