using Amg.Test;
using NUnit.Framework;
using Serilog;

namespace Amg.Extensions;

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
        var padded = e.Pad(padCount, String.Empty).ToList();
        Assert.That(padded.Count, Is.EqualTo(padCount));
        Assert.That(padded.Where(_ => !String.IsNullOrEmpty(_)).SequenceEqual(e));
    }

    [Test]
    public void TakeAllBut()
    {
        var e = Enumerable.Range(0, 3);
        var eb1 = e.TakeAllBut(1).ToList();
        Assert.That(eb1.SequenceEqual(Enumerable.Range(0, 2)));
        Assert.That(e.TakeAllBut(0).SequenceEqual(Enumerable.Range(0, 3)));
        Assert.That(e.TakeAllBut(3).SequenceEqual(Enumerable.Range(0, 0)));
    }

    [Test]
    public void StartsWith()
    {
        var e = Enumerable.Range(0, 100);
        Assert.That(e.StartsWith(Enumerable.Range(0, 100)));
        Assert.That(e.StartsWith(Enumerable.Range(0, 1)));
        Assert.That(e.StartsWith(Enumerable.Range(0, 0)));
        Assert.That(!e.StartsWith(Enumerable.Range(0, 101)));
    }
}
