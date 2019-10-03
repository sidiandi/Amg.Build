using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace Amg.Build
{
    [TestFixture]
    public class RunnerTests
    {
        [Test]
        public void Run()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { });
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public void Ascii()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "--ascii-art" });
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
            var exitCode = Runner.Run<MyBuild>(new string[] { "always-fails", "-vq", "--ascii"});
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.TargetFailed));
        }

        [Test]
        public void ToolFail()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "tool-fails", "-vq" });
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
            var once = Amg.Build.Once.Create<MyBuild>("Test");
            await once.All();
            Assert.That(once.result, Is.EqualTo("TestCompileLinkPack"));
        }

        [Test]
        public void PrintReturnValue()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "get-info" });
            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}
