using NUnit.Framework;

namespace Amg.Build
{
    [TestFixture]
    public class FunctionUtilsTests
    {
        [Test]
        public void Once()
        {
            var count = 0;

            int f(int a)
            {
                ++count;
                return a * a;
            }

            var fo = FunctionUtils.Once((int i) => f(i));

            fo(1);
            Assert.AreEqual(1, count);
            fo(1);
            Assert.AreEqual(1, count);
            fo(2);
            Assert.AreEqual(2, count);
        }
    }
}
