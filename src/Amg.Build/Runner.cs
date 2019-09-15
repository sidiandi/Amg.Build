using Castle.DynamicProxy;
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
        /// <summary>
        /// Creates an TargetsDerivedClass instance and runs the contained targets according to the passed commandLineArguments.
        /// </summary>
        /// Call this method directly from your Main()
        /// <typeparam name="TargetsDerivedClass"></typeparam>
        /// <param name="commandLineArguments"></param>
        /// <param name="callerFilePath"></param>
        /// <param name="rebuildCheck">Check if a rebuild of the DLL is required.</param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run<TargetsDerivedClass>(
            string[] commandLineArguments, 
            [CallerFilePath] string callerFilePath = null, 
            bool rebuildCheck = true) where TargetsDerivedClass : class
        {
            var runner = new RunContext(
                callerFilePath,
                Assembly.GetEntryAssembly().Location,
                typeof(TargetsDerivedClass),
                commandLineArguments,
                rebuildCheck
                );

            return (int) runner.Run();
        }

        /// <summary>
        /// Returns the directory where build.cmd resides.
        /// </summary>
        /// <param name="callerFilePath"></param>
        /// <returns></returns>
        public static string RootDirectory([CallerFilePath] string callerFilePath = null)
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
        /// <param name="callerFilePath"></param>
        /// <param name="rebuildCheck">Check if a rebuild of the DLL is required.</param>
        /// <returns>Exit code: 0 if success, unequal to 0 otherwise.</returns>
        public static int Run(
            string[] commandLineArguments, 
            [CallerFilePath] string callerFilePath = null,
            bool rebuildCheck = true)
        {
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            var runner = new RunContext(
                callerFilePath,
                Assembly.GetEntryAssembly().Location,
                type,
                commandLineArguments,
                rebuildCheck
                );

            return (int)runner.Run();
        }

        /// <summary>
        /// Creates an instance of T where all methods marked with [Once] are only executed once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Once<T>()
        {
            return Once<T>(_ => { });
        }

        /// <summary>
        /// Creates an instance of T where all methods marked with [Once] are only executed once.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Once<T>(Action<T> initializer)
        {
            var builder = new DefaultProxyBuilder();
            var generator = new ProxyGenerator(builder);
            var onceInterceptor = new OnceInterceptor();
            var onceProxy = generator.CreateClassProxy(typeof(T), new ProxyGenerationOptions
            {
                Hook = new OnceHook()
            },
            onceInterceptor);
            var proxy = (T) onceProxy;
            initializer(proxy);
            return proxy;
        }
    }
}
