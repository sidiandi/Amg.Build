﻿using NUnit.Framework;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Amg.Build
{
    [TestFixture]
    public class EnumerableExtensionsTests : TestBase
    {
        [Test]
        public void Progress()
        {
            var text = new StringWriter();
            var logger = new Serilog.LoggerConfiguration()
                .WriteTo.TextWriter(text)
                .CreateLogger();

            Enumerable.Range(0, 100).Progress(
                metric: _ => 1000.0,
                metricUnit: "Bytes",
                description: "Testing...",
                updateInterval: TimeSpan.FromSeconds(0.01),
                logger: logger
                )
                .Select(_ =>
                {
                    Thread.Sleep(10);
                    return _;
                }).ToList();

            var logOutput = text.ToString();
            Assert.That(logOutput.SplitLines().Count() > 50);
            Assert.That(logOutput.Contains("Testing..."));
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
