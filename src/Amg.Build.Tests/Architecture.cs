using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using Amg.Build.Extensions;

namespace Amg.Build
{
    [TestFixture]
    public class Architecture
    {
        [Test]
        public void Api()
        {
            var assembly = typeof(Runner).Assembly;
            Console.WriteLine(PublicApi(assembly));
            Assert.That(true);
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
            if (!t.IsPublic) return;

            var publicMethods = t.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static|
                BindingFlags.DeclaredOnly
                );

            foreach (var i in publicMethods)
            {
                w.WriteLine(FullSignature(i));
            }
        });

        string FullSignature(MethodInfo i) => $"{i.DeclaringType!.Assembly.GetName().Name}:{i.DeclaringType.FullName}.{i.Name}({Parameters(i)}): {Nice(i.ReturnType)}";

        static string Nice(Type t)
        {
            return t.Name;
        }

        static string Parameters(MethodInfo m)
        {
            return m.GetParameters()
                .Select(_ => Nice(_.ParameterType))
                .Join(", ");
        }
    }
}
