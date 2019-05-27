using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Csa.Build
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
        public async Task WorkingDirectory()
        {
            var workingDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
            var dir = new Tool("cmd.exe")
                .WithArguments("/c", "dir")
                .WithWorkingDirectory(workingDirectory);
            var r = await dir.Run(".");
            Assert.That(r.Output, Does.Contain(workingDirectory));
        }

        [Test]
        public async Task RunError()
        {
            var tool = new Tool("cmd.exe");
            try
            {
                await tool.Run("/c", "echo_Wrong_Command", "Hello");
                Assert.Fail("must throw");
            }
            catch (Exception)
            {
            }
        }
    }
}