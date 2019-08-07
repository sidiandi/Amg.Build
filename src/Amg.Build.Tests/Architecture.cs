using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Amg.Build.Tests
{
    [TestFixture]
    public class Architecture
    {
        [Test]
        public void Api()
        {
            var assembly = typeof(Runner).Assembly;
            Console.WriteLine(PublicApi(assembly));
        }

        IWritable PublicApi(Assembly a) => TextFormatExtensions.GetWritable(w =>
        {
            foreach (var t in a.GetTypes()
                .Where(_ => _.IsPublic))
            {
                w.Write(PublicApi(t));
            }
        });

        IWritable PublicApi(Type t) => TextFormatExtensions.GetWritable(w =>
        {
            w.WriteLine(t.FullName);
            foreach (var i in t.GetMethods()
                .Where(_ => _.DeclaringType.Equals(t))
            )
            {
                w.WriteLine($"  {i.Name}");
            }
        });
    }
}
