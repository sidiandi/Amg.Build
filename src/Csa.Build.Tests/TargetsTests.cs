using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Csa.Build
{
    [TestFixture]
    public class TargetsTests
    {
        class MyTargets : Targets
        {
            public string result = String.Empty;

            Target Compile => DefineTarget(async () =>
            {
                result += "Compile";
            });

            Target Link => DefineTarget(async () =>
            {
                await Compile();
                result += "Link";
            });

            public Target Pack => DefineTarget(async () =>
            {
                await Compile();
                await Link();
                result += "Pack";
            });
        }

        [Test]
        public async Task TargetsAreRunOnlyOnceAndInCorrectOrder()
        {
            var t = new MyTargets();
            await t.Pack();
            Assert.AreEqual("CompileLinkPack", t.result);
        }

    }
}
