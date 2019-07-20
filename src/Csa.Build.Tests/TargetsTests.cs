using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System;

namespace Csa.Build
{
    [TestFixture]
    public class TargetsTests : TestBase
    {
        [Test]
        public async Task TargetsAreRunOnlyOnceAndInCorrectOrder()
        {
            var t = new MyTargets();
            await t.RunTargets(new[] { "Pack" });
            Assert.AreEqual("CompileLinkPack", t.result);
        }

        [Test]
        public async Task WhenNoTargetIsSpecifiedDefaultTargetIsRun()
        {
            var t = new MyTargets();
            await t.RunTargets(new string[] { });
            Assert.AreEqual("CompileLinkPack", t.result);
        }

        [Test]
        public async Task TargetsCanBeAbbreviated()
        {
            var t = new MyTargets();
            await t.RunTargets(new string[] { "Co" });
            Assert.AreEqual("Compile", t.result);
        }

        [Test]
        public async Task TargetsAreRunOnlyOncePerInputArgument()
        {
            var t = new MyTargets();
            const int aCount = 10;
            foreach (var i in Enumerable.Range(0,3))
            {
                foreach (var a in Enumerable.Range(0, aCount))
                {
                    await t.Div2(a);
                }
            }
            Assert.AreEqual(aCount, t.args.Count);
        }

        [Test]
        public void TargetsAreRunWithCommandLineArgs()
        {
            var exitCode = Targets.Run<MyTargets>(new[] { "Pack", "-vvvv", "--configuration", "MyConfiguration" });
        }

        [Test]
        public void Help()
        {
            var o = TestUtil.CaptureOutput(() =>
            {
                var exitCode = Targets.Run<MyTargets>(new[] { "--help" });
                Assert.That(exitCode, Is.EqualTo(1));
            });
            Assert.AreEqual(@"Usage: build <targets> [options]

Targets:
  Compile Compile source code    
  Link    Link object files      
  Pack    Pack nuget package     
  Default Compile, link, and pack

Options:
  --configuration=<string> Release or Debug  
  -h | --help              Show help and exit
  -v | --verbose           Increase verbosity
", o.Out);
            Assert.AreEqual(String.Empty, o.Error);
        }
    }
}
