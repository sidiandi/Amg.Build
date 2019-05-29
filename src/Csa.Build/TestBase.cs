using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Csa.Build
{
    public class TestBase
    {
        static TestBase()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        protected string CreateEmptyTestDirectory([CallerMemberName] string name = null)
        {
            var a = this.GetType().Assembly;
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                a.GetCustomAttribute<AssemblyCompanyAttribute>().Company,
                a.GetCustomAttribute<AssemblyProductAttribute>().Product,
                "test",
                name);
            return dir.EnsureDirectoryIsEmpty();
        }
    }
}
