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
            var echo = Tools.Cmd.WithArguments("echo");
            await echo.Run("Hello");
        }

        [Test]
        public async Task FileNotFound()
        {
            var programThatDoesNotExist = "program-that-does-not-exist.exe";
            var echo = new Tool(programThatDoesNotExist);
            var e = Assert.Throws<AggregateException>(() => echo.Run("Hello").Wait());
            Console.WriteLine(e.InnerException);
            Assert.That(e.InnerException is ToolStartFailed);
            Assert.That(e.InnerException.Message.Contains(programThatDoesNotExist));
            await Task.CompletedTask;
        }

        [Test]
        public async Task Environment()
        {
            var echo = Tools.Cmd
                .WithArguments("echo")
                .WithEnvironment(new Dictionary<string, string>{ { "NAME", "Alice" } });
            var r = await echo.Run("Hello", "%NAME%");
            Assert.That(r.Output, Is.EqualTo("Hello Alice\r\n"));
        }

        [Test]
        public async Task WorkingDirectory()
        {
            var workingDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
            var dir = Tools.Cmd
                .WithArguments("dir")
                .WithWorkingDirectory(workingDirectory);
            var r = await dir.Run(".");
            Assert.That(r.Output, Does.Contain(workingDirectory));
        }

        [Test]
        public async Task RunError()
        {
            var tool = Tools.Cmd
                .WithEnvironment(new Dictionary<string, string> { { "Name", "Alice" } });
            try
            {
                await tool.Run("echo_Wrong_Command", "Hello");
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

        [Test][Ignore("requires a local user test")]
        public async Task RunAs()
        {
            var user = "test";
            var password = @"adm$pwd$4$med$";
            var tool = Tools.Cmd.RunAs(user, password);
            await tool.Run("echo hello");

            var wrongPassword = @"adm$pwd$4$med";
            var wrongPasswordTool = tool.RunAs(user, wrongPassword);
            var ex = Assert.Throws<System.AggregateException>(() =>
            {
                wrongPasswordTool.Run("echo hello").Wait();
            });
            Assert.That(ex.InnerException.Message, Is.EqualTo("The user name or password is incorrect"));
        }
    }
}