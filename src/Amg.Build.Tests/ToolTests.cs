using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class ToolTests
    {
        [Test]
        public async Task Run()
        {
            var echo = new Tool("cmd.exe")
                .WithArguments("/c", "echo");
            await echo.Run("Hello");
        }

        [Test]
        public async Task Environment()
        {
            var echo = new Tool("cmd.exe")
                .WithArguments("/c", "echo")
                .WithEnvironment(new Dictionary<string, string>{ { "NAME", "Alice" } });
            var r = await echo.Run("Hello", "%NAME%");
            Assert.That(r.Output, Is.EqualTo("Hello Alice\r\n"));
        }

        [Test]
        public async Task WorkingDirectory()
        {
            var workingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
            var dir = new Tool("cmd.exe")
                .WithArguments("/c", "dir")
                .WithWorkingDirectory(workingDirectory);
            var r = await dir.Run(".");
            Assert.That(r.Output, Does.Contain(workingDirectory));
        }

        [Test]
        public async Task RunError()
        {
            var tool = new Tool("cmd.exe")
                .WithEnvironment(new Dictionary<string, string> { { "Name", "Alice" } });
            try
            {
                await tool.Run("/c", "echo_Wrong_Command", "Hello");
                Assert.Fail("must throw");
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<ToolException>());
                var te = (ToolException)e;
                Console.WriteLine(te);
                Console.WriteLine(te.Result.Error);
            }
        }
    }
}