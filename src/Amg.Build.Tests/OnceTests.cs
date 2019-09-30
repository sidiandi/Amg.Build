using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace Amg.Build
{
    public class Hello
    {
        public string Name { get; }

        protected Hello(string name)
        {
            Name = name;
        }

        [Once]
        public virtual void Greet()
        {

            ++Count;
        }

        [Once]
        public virtual string Greeting => $"Hello, {Name}";

        [Once]
        public virtual WebClient Web => new WebClient();

        public int Count { get; private set; }
    }

    [TestFixture]
    public class OnceTests
    {
        [Test]
        public async Task Once()
        {
            var once = Amg.Build.Once.Create<MyBuild>();
            await once.All();
            Assert.That(once.result, Is.EqualTo("CompileLinkPack"));
        }

        [Test]
        public void OnlyExecutesOnce()
        {
            var once = new Once();
            var name = "Alice";
            var hello = once.Get<Hello>(name);
            hello.Greet();
            hello.Greet();

            var hello2 = once.Get<Hello>(name);
            hello2.Greet();
            hello2.Greet();

            Assert.That(hello.Count, Is.EqualTo(1));
            Assert.That(hello2.Name, Is.EqualTo(name));
        }
    }
}
