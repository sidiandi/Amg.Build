using NUnit.Framework;
using System;
using System.Linq;

namespace Amg.Build
{
    [TestFixture]
    class RunContextTests
    {
        internal static (RunContext.CommandInvocation c, ArraySegment<string> rest)
            ParseCommand(string[] arguments, object targets)
        {
            var rest = new ArraySegment<string>(arguments);
            var c = RunContext.ParseCommand(ref rest, targets);
            return (c, rest);
        }

        [Test]
        public void MissingArgument()
        {
            var co = new CombinedOptions(Once.Create<MyBuild>());
            Assert.Throws<CommandLine.CommandLineArgumentException>(() =>
            {
                ParseCommand(new[] { "say-hello" }, co);
            });
        }

        [Test]
        public void Rest()
        {
            var co = Once.Create<MyBuild>();
            var (c,r) = ParseCommand(new[] { "say-hello", "name", "additional" }, co);
            Assert.That(r.SequenceEqual(new[] { "additional" }));
        }

        [Test]
        public void OptionalParameter()
        {
            var co = Once.Create<MyBuild>();
            var (c, r) = ParseCommand(new[] { "say-something" }, co);
            Assert.That(r.SequenceEqual(new string[] { }));
        }
    }
}
