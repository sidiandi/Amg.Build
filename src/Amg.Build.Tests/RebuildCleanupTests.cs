using NUnit.Framework;
using System.Diagnostics;

namespace Amg.Build.Tests
{
    [TestFixture]
    class RebuildCleanupTests : TestBase
    {
        [Test]
        public void MoveToArgs()
        {
            var testDir = CreateEmptyTestDirectory();

            var move = new RebuildCleanup.Args
            {
                Source = testDir.Combine("source"),
                Dest = testDir.Combine("dest")
            };

            var si = new ProcessStartInfo();
            RebuildCleanup.SetArgs(move, si);
            try
            {
                System.Environment.SetEnvironmentVariable(RebuildCleanup.ArgsKey, si.Environment[RebuildCleanup.ArgsKey]);
                var move1 = RebuildCleanup.GetArgs()!;
                Assert.AreEqual(move.Source, move1.Source);
                Assert.AreEqual(move.Dest, move1.Dest);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(RebuildCleanup.ArgsKey, null);
            }
        }
    }
}
