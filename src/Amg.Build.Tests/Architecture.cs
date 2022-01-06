using NUnit.Framework;
using System.Reflection;
using Amg.Extensions;

namespace Amg.Build;

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
        w.WriteLine(a.GetName().Name);
        foreach (var t in a.GetTypes()
            .Where(_ => _.IsPublic))
        {
            w.Indent("  ").Write(PublicApi(t));
        }
    });

    IWritable PublicApi(Type t) => TextFormatExtensions.GetWritable(w =>
    {
        if (!t.IsPublic) return;

        w.WriteLine(t.FullName);

        var publicMethods = t.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly
            );

        var iw = w.Indent("  ");
        foreach (var i in publicMethods)
        {
            iw.WriteLine($"{i.Name}({Parameters(i)}): {Nice(i.ReturnType)}");
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
