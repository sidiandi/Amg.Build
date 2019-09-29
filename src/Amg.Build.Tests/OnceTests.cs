using NUnit.Framework;
using System.Net;

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
        public void OnlyExecutesOnce()
        {
            var once = new Once();
            var name = "Alice";
            var hello = once.Add<Hello>(name);
            hello.Greet();
            hello.Greet();

            var hello2 = once.Get<Hello>();
            hello2.Greet();
            hello2.Greet();

            Assert.That(hello.Count, Is.EqualTo(1));
            Assert.That(hello2.Name, Is.EqualTo(name));

            var hello3 = (Hello) once.GetService(typeof(Hello));
            Assert.That(hello3.Name, Is.EqualTo(name));
        }
    }
}
