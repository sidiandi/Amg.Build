using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amg.Build
{
    [TestFixture]
    public class GlobTests
    {
        [Test]
        public void Glob()
        {
            var temp = System.IO.Path.GetTempPath();
            var glob = temp.Glob().Include("**");
            Console.WriteLine(glob.Join());
        }

        [Test]
        public void Glob2()
        {
            var temp = System.IO.Path.GetTempPath();
            var glob = @"C:\src\Amg.Build\examples\hello\build".Glob()
                .Include("**")
                .Exclude("obj").Exclude("bin").Exclude(".vs");

            Console.WriteLine(glob.Join());
        }
    }
}