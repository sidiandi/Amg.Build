using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    [TestFixture]
    class RunContextTests
    {
        internal static (MethodInfo? method, object?[] parameters, ArraySegment<string> rest)
            ParseCommands(string[] arguments, object targets)
        {
            var rest = new ArraySegment<string>(arguments);
            var (method, parameters) = RunContext.ParseCommands(ref rest, targets);
            return (method, parameters, rest);
        }

        [Test]
        public void MissingArgument()
        {
            var co = new CombinedOptions(Once.Create<MyBuild>());
            Assert.Throws<CommandLine.CommandLineArgumentException>(() =>
            {
                ParseCommands(new[] { "say-hello" }, co);
            });
        }

        [Test]
        public void Rest()
        {
            var co = Once.Create<MyBuild>();
            var (m,p,r) = ParseCommands(new[] { "say-hello", "name", "additional" }, co);
            Assert.That(r.SequenceEqual(new[] { "additional" }));
        }

        [Test]
        public void OptionalParameter()
        {
            var co = Once.Create<MyBuild>();
            var (m, p, r) = ParseCommands(new[] { "say-something" }, co);
            Assert.That(r.SequenceEqual(new string[] { }));
        }
    }
}
