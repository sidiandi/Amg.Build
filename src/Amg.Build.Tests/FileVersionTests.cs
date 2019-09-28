using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Amg.Build
{
    [TestFixture]
    public class FileVersionTests
    {
        [Test]
        public void DllIsNewerThanSourceCode()
        {
            var dll = Assembly.GetExecutingAssembly().Location;
            var sourceDir = dll.Parent().Parent().Parent().Parent();
            Assert.That(FileVersion.Get(dll).IsNewer(FileVersion.Get(sourceDir)));
        }
    }
}
