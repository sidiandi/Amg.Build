using NUnit.Framework;
using System.Linq;

namespace Amg.Build
{
    [TestFixture]
    class RunContextTests
    {
        [Test]
        public void MissingArgument()
        {
            var co = new CombinedOptions(Once.Create<MyBuild>());
            Assert.Throws<CommandLine.CommandLineArgumentException>(() =>
            {
                RunContext.ParseCommands(new[] { "say-hello" }, co);
            });
        }

        [Test]
        public void Rest()
        {
            var co = Once.Create<MyBuild>();
            var (m,p,r) = RunContext.ParseCommands(new[] { "say-hello", "name", "additional" }, co);
            Assert.That(r.SequenceEqual(new[] { "additional" }));
        }

        [Test]
        public void OptionalParameter()
        {
            var co = Once.Create<MyBuild>();
            var (m, p, r) = RunContext.ParseCommands(new[] { "say-something" }, co);
            Assert.That(r.SequenceEqual(new string[] { }));
        }
    }
}
