using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
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

        public static TimeSpan MeasureTime(Action a)
        {
            var stopwatch = Stopwatch.StartNew();
            a();
            return stopwatch.Elapsed;
        }

        public static TimeSpan MeasureTime(Func<Task> a)
        {
            var stopwatch = Stopwatch.StartNew();
            a().Wait();
            return stopwatch.Elapsed;
        }
    }
}
