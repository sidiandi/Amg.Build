using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
    [TestFixture]
    public class ToolTests : TestBase
    {
        [Test]
        public void SetFileName()
        {
            var aFileName = "a";
            var a = Tools.Default.WithFileName(aFileName);
            var bFileName = "b";
            var b = a.WithFileName(bFileName);

            string? GetFileNameField(ITool tool)
            {
                var field = tool
                    .GetType()
                    .GetField("fileName", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (field == null)
                {
                    throw new InvalidOperationException();
                }
                return (string?) field.GetValue(tool);
            }

            Assert.AreEqual(aFileName, GetFileNameField(a));
            Assert.AreEqual(bFileName, GetFileNameField(b));

        }

        [Test]
        public void Run()
        {
            var echo = Tools.Cmd.WithArguments("echo");
            Assert.DoesNotThrowAsync(async () =>
            {
                await echo.Run("Hello");
            });
        }

        [Test]
        public async Task FileNotFound()
        {
            var programThatDoesNotExist = "program-that-does-not-exist.exe";
            var echo = new Tool(programThatDoesNotExist);
            var e = Assert.Throws<AggregateException>(() => echo.Run("Hello").Wait());
            var ie = e.InnerException!;
            Console.WriteLine(ie);
            Assert.That(ie is ToolStartException);
            Assert.That(ie.Message.Contains(programThatDoesNotExist));
            await Task.CompletedTask;
        }

        [Test]
        public async Task Environment()
        {
            var echo = Tools.Cmd
                .WithArguments("echo")
                .WithEnvironment(new Dictionary<string, string> { { "NAME", "Alice" } });
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

        [Test, Ignore("requires a local user test")]
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
            Assert.That(ex.InnerException!.Message, Is.EqualTo("The user name or password is incorrect"));
        }
    }
}