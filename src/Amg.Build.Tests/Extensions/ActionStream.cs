using Amg.Test;
using NUnit.Framework;

namespace Amg.Extensions;

[TestFixture]
internal class ActionStreamTests : TestBase
{
    [Test]
    public void ActionIsCalledForEveryLine()
    {
        var lines = new List<String>();
        var s = Amg.Extensions.Utils.AsTextWriter(_ => lines.Add(_));

        s.WriteLine("Hello");
        s.Write("W");
        s.WriteLine("orld");
        s.Write("!");
        s.Flush();
        Assert.That(lines.SequenceEqual(new[]
        {
                "Hello",
                "World",
                "!"
            }));

    }
}
