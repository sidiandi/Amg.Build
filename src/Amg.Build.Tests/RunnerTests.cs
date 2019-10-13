using System.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Linq;
using Amg.Build.Extensions;

namespace Amg.Build
{
    [TestFixture]
    public class RunnerTests
    {
        [Test]
        public void Run()
        {
            var c = Once.Create<MyBuild>();
            var exitCode = Runner.Run(c, new string[] { });
            Console.WriteLine(c.result.Join());
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.Success));
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
        public void OnceFail()
        {
            var exitCode = Runner.Run<AClassThatHasMutableFields >(new string[] { });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.TargetFailed));
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

        [Test]
        public void CommandLineErrorMissingArguments()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "say-hello" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.CommandLineError));
        }

        [Test]
        public void CommandLineDefaultParameterMissing()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "say-something" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.Success));
        }

        [Test]
        public void CommandLineDefaultParameterPresent()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "say-something", "hello" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.Success));
        }

        [Test]
        public void CommandLineDefaultParameterParams()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "use-params", "1", "2", "3" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.Success));
        }

        [Test]
        public void MultipleCommands()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "say-something", "hello", "say-something" });
            Assert.That(exitCode, Is.EqualTo((int)RunContext.ExitCode.Success));
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
            Assert.That(o.Out, Does.Contain("use-params [items]..."));
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
            Assert.That(once.result.SequenceEqual(new[] { "Test", "Compile", "Link", "Pack" }));
        }

        [Test]
        public void PrintReturnValue()
        {
            var exitCode = Runner.Run<MyBuild>(new string[] { "get-info" });
            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}
