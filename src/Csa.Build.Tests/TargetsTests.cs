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
    public class TargetsTests : TestBase
    {
        class MyTargets : Targets
        {
            public string result = String.Empty;

            Target Compile => DefineTarget(() =>
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

            public Target<int, int> Times2 => DefineTarget((int a) =>
            {
                args.Add(a);
                return a * 2;
            });

            public Target<int, int> Div2 => DefineTarget(async (int a) =>
            {
                await Task.Delay(100);
                return await Times2(a) / 4;
            });

            public IList<int> args = new List<int>();
        }

        [Test]
        public async Task TargetsAreRunOnlyOnceAndInCorrectOrder()
        {
            var t = new MyTargets();
            await t.Run(new[] { "Pack" });
            Assert.AreEqual("CompileLinkPack", t.result);
        }

        [Test]
        public async Task TargetsAreRunOnlyOncePerInputArgument()
        {
            var t = new MyTargets();
            const int aCount = 10;
            foreach (var i in Enumerable.Range(0,3))
            {
                foreach (var a in Enumerable.Range(0, aCount))
                {
                    await t.Div2(a);
                }
            }
            Assert.AreEqual(aCount, t.args.Count);
        }
    }
}
