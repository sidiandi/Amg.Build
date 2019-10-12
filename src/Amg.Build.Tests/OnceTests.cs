using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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
            Count.Enqueue(0);
        }

        [Once]
        public virtual string Greeting => $"Hello, {Name}";

        [Once]
        public virtual WebClient Web => new WebClient();

        public Queue<int> Count { get; } = new Queue<int>();
    }

#pragma warning disable CS0414
    public class AClassThatHasMutableFields
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2933:Fields that are only assigned in the constructor should be \"readonly\"", Justification = "<Pending>")]
        int i = 0;

        [Once]
        public virtual void Hello()
        {
            Console.WriteLine("hello");
        }
    }
#pragma warning restore CS0414

    public class AClassWithOnceProperty
    {
        [Once]
        public virtual string? Name { get; set; } = null;
    }

    [TestFixture]
    public class OnceTests
    {
        [Test]
        public async Task RunOnce()
        {
            var once = Amg.Build.Once.Create<MyBuild>();
            await once.All();
            Assert.That(once.result.SequenceEqual(new[] { "Compile", "Link", "Pack" }));
        }

        [Test]
        public void OnceCannotBeAppliedWhenClassHasMutableFields()
        {
            Assert.Throws<OnceException>(() =>
            {
                Amg.Build.Once.Create<AClassThatHasMutableFields>();
            });
        }

        [Test]
        public void OncePropertiesWithSettersCanOnlyBeSetOnce()
        {
            var once = Once.Create<AClassWithOnceProperty>();
            once.Name = "Alice";
            Assert.Throws<OncePropertyCanOnlyBeCalledOnceException>(() =>
            {
                once.Name = "Bob";
            });
        }

       [Test]
        public void OnlyExecutesOnce()
        {
            var name = "Alice";
            var hello = Once.Create<Hello>(name);
            hello.Greet();
            hello.Greet();

            var hello2 = Once.Create<Hello>(name);
            hello2.Greet();
            hello2.Greet();

            Assert.That(hello.Count.Count, Is.EqualTo(1));
            Assert.That(hello2.Name, Is.EqualTo(name));
        }
    }
}
