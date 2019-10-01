using Castle.DynamicProxy;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Amg.Build
{
    /// <summary>
    /// Runs classes with [Once] 
    /// </summary>
    public class Runner
    {
        private static Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        /// <summary>
        /// Creates an TargetsDerivedClass instance and runs the contained targets according to the passed commandLineArguments.
        /// </summary>
        /// Call this method directly from your Main()
        /// <typeparam name="TargetsDerivedClass"></typeparam>
        /// <param name="commandLineArguments"></param>
        /// <param name="callerFilePath"></param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run<TargetsDerivedClass>(
            string[] commandLineArguments, 
            [CallerFilePath] string? callerFilePath = null)
            where TargetsDerivedClass : class
        {
            return Run(typeof(TargetsDerivedClass), commandLineArguments, callerFilePath!);
        }

        /// <summary>
        /// Returns the directory where build.cmd resides.
        /// </summary>
        /// <param name="callerFilePath"></param>
        /// <returns></returns>
        public static string RootDirectory([CallerFilePath] string callerFilePath = null!)
        {
            return callerFilePath.Parent().Parent();
        }

        /// <summary>
        /// Treats the caller's type as container of [Once] methods and runs it.
        /// </summary>
        /// Convenience method to embed the Main method directly in your Build class:
        /// ````
        /// 	static int Main(string[] args) => Runner.Run(args);
        /// ````
        /// See $/examples/hello/build/build.cs for an example.
        /// <param name="commandLineArguments"></param>
        /// <param name="sourceFile"></param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run(string[] commandLineArguments, [CallerFilePath] string? sourceFile = null)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            return Run(type, commandLineArguments, sourceFile!);
        }

        static int Run(Type type, string[] commandLineArguments, string sourceFile)
        {
            RebuildMyself.BuildIfOutOfDate(
                type.Assembly,
                sourceFile,
                commandLineArguments).Wait();

            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var runner = new RunContext(type, commandLineArguments);
            return (int)runner.Run().Result;
        }

        /// <summary>
        /// Creates an instance of T where all methods marked with [Once] are only executed once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Obsolete("use Once.Create")]
        public static T Once<T>(params object[] ctorArguments) where T: class
        {
            return Amg.Build.Once.Create<T>(ctorArguments);
        }
    }
}
