using Amg.CommandLine;
using NUnit.Framework;

namespace Amg.Build
{
    [TestFixture]
    class GetOptParserTests
    {
        [Test]
        public void IgnoreUnknownOptions()
        {
            var options = new Options();
            Assert.Throws<CommandLineArgumentException>(() =>
            {
                GetOptParser.Parse(new[] { "--unknownOption" }, options);
            });

            GetOptParser.Parse(new[] { "--unknownOption" }, options, ignoreUnknownOptions: true);
            GetOptParser.Parse(new[] { "--known-option" }, options);
        }

        class Options
        { 
            [System.ComponentModel.Description("option that actually exists")]
            public bool KnownOption { get; set; }
        }
    }
}
