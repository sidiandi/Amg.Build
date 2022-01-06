using NUnit.Framework;

namespace Amg.Build;

[TestFixture]
class InvocationInfoTests
{
    [Test]
    public void GetResultType()
    {
        Assert.That(!InvocationInfo.TryGetResultType(Task.CompletedTask, out var resultType));
    }

    [Test]
    public void GetResultType2()
    {
        Assert.That(InvocationInfo.TryGetResultType(Task.FromResult(String.Empty), out var resultType));
        Assert.That(resultType, Is.EqualTo(typeof(string)));
    }
}
