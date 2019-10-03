﻿using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

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
            foreach (var i in t.GetMethods()
                .Where(_ => object.Equals(_.DeclaringType, t))
            )
            {
                if (i.DeclaringType != null)
                {
                    w.WriteLine($"{i.DeclaringType.Assembly.GetName().Name}:{i.DeclaringType.FullName}.{i.Name}({Parameters(i)}): {Nice(i.ReturnType)}");
                }
            }
        });

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
