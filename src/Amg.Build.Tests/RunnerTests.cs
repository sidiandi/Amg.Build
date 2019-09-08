using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection;
using NUnit.Framework;
using System;

namespace Amg.Build
{
    [TestFixture]
    public class RunnerTests : TestBase
    {
        [Test]
        public async Task Once()
        {
            var once = Runner.Once<MyBuild>();
            await once.All();
            Assert.That(once.result, Is.EqualTo("CompileLinkPack"));
        }

        [Test]
        public void Run()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { });
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public void SayHello()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "say-hello", "World" });
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public void Fail()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "always-fails", "-vq" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.TargetFailed));
        }

        [Test]
        public void CommandLineErrorWrongOption()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "--this-option-is-wrong" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.CommandLineError));
        }

        [Test]
        public void CommandLineErrorWrongTarget()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "this-target-is-wrong" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.CommandLineError));
        }

        public class MinimalTargets
        {
        }

        [Test]
        public void Help()
        {
            var o = TestUtil.CaptureOutput(() =>
            {
                var exitCode = Runner.Run<MyBuild>(new[] { "--help" });
                Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.HelpDisplayed));
            });
            /*
            Assert.AreEqual(@"Usage: build <targets> [options]

Targets:
  Link     Link object files      
  SayHello Say hello              
  Pack     Pack nuget package     
  Default  Compile, link, and pack

Options:
  --configuration=<string>                Release or Debug                                                
  -h | --help                             Show help and exit                                              
  -v<verbosity> | --verbosity=<verbosity> Set the verbosity level. verbosity=quiet|minimal|normal|detailed
", o.Out);

            */
            Assert.AreEqual(String.Empty, o.Error);
            Assert.That(o.Out, Does.Not.Contain("--ignore-clean"));
        }

        [Test]
        public void Minimal()
        {
            var exitCode = Runner.Run<MinimalTargets>(new[] { "--help" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.HelpDisplayed));
        }

        [Test]
        public void NestedOnce()
        {
            var exitCode = Runner.Run<MyBuild>(new[] { "Version" });
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public async Task RunProgrammatically()
        {
            var once = Runner.Once<MyBuild>(_ => _.result = "Test");
            await once.All();
            Assert.That(once.result, Is.EqualTo("TestCompileLinkPack"));
        }
    }
}
