using Amg.CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Runs classes with [Once] 
    /// </summary>
    internal class RunContext
    {
        private static Serilog.ILogger Logger => Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly Type targetsType;
        private readonly string[] commandLineArguments;

        public enum ExitCode
        {
            Success = 0,
            UnknownError = 1,
            RebuildRequired = 2,
            HelpDisplayed = 3,
            CommandLineError = 4,
            TargetFailed = 5
        }

        public RunContext(
            Type targetsType,
            string[] commandLineArguments
            )
        {
            this.targetsType = targetsType;
            this.commandLineArguments = commandLineArguments;
        }

        static ExitCode RequireRebuild()
        {
            Console.Out.WriteLine("Build script requires rebuild.");
            return ExitCode.RebuildRequired;
        }

        async Task<ExitCode?> Watch()
        {
            if (!String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(nowatchEnvironmentVariable)))
            {
                return null;
            }

            var sourceCodeLayout = SourceCodeLayout.Get(targetsType);
            if (sourceCodeLayout != null)
            {
                await WatchInternal(sourceCodeLayout, this.commandLineArguments);
                return ExitCode.Success;
            }
            else
            {
                return ExitCode.TargetFailed;
            }
        }

        const string nowatchEnvironmentVariable = "Amg.Build_nowatch";

        async Task WatchInternal(SourceCodeLayout source, string[] commandLineArgs)
        {

            var watchedDir = source.CmdFile.Parent();
            using (var fsw = new FileSystemWatcher
            {
                Path = watchedDir,
                IncludeSubdirectories = true,
            })
            {
                var tool = Tools.Cmd.WithArguments(source.CmdFile)
                    .WithEnvironment(nowatchEnvironmentVariable, true.ToString())
                    .Passthrough();

                Task run = Task.CompletedTask;

                void Changed(object sender, FileSystemEventArgs e)
                {
                    if (run.IsCompleted)
                    {
                        run = tool.Run(commandLineArgs);
                    }
                }

                fsw.Changed += Changed;
                fsw.Created += Changed;
                fsw.Deleted += Changed;
                fsw.Renamed += Changed;

                fsw.EnableRaisingEvents = true;

                Console.Write($"Watching {watchedDir}...");
                await Task.Delay(-1);

                fsw.EnableRaisingEvents = false;

                fsw.Changed -= Changed;
                fsw.Created -= Changed;
                fsw.Deleted -= Changed;
                fsw.Renamed -= Changed;
            }
        }

        public async Task<ExitCode> Run()
        {
            try
            {
                RecordStartupTime();

                var minimalOptions = new Options();
                var rest = new ArraySegment<string>(commandLineArguments);
                GetOptParser.Parse(ref rest, minimalOptions, ignoreUnknownOptions: true);

                bool needConfigureLogger = Log.Logger.GetType().Name.Equals("SilentLogger");
                if (needConfigureLogger)
                {
                    var levelSwitch = new LoggingLevelSwitch(SerilogLogEventLevel(minimalOptions.Verbosity));
                    if (minimalOptions.Verbosity == Verbosity.Quiet)
                    {
                        Tools.Default = Tools.Default.Silent();
                    }

                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(levelSwitch)
                        .WriteTo.Console(LogEventLevel.Verbose,
                        standardErrorFromLevel: LogEventLevel.Error,
                        outputTemplate: "{Timestamp:o}|{Level:u3}|{Message:lj}{NewLine}{Exception}"
                        )
                        .CreateLogger();
                }

                await RebuildMyself.BuildIfSourcesChanged(commandLineArguments);

                var source = SourceCodeLayout.Get(targetsType);

                object CreateOnceProxy(Type type)
                {
                    try
                    {
                        return Once.Create(type);
                    }
                    catch (Exception ex)
                    {
                        throw new OnceException($"Error in using the [Once] attribute in {type}.", ex);
                    }
                }

                var onceProxy = CreateOnceProxy(targetsType);
                var combinedOptions = new CombinedOptions(onceProxy);
                if (source != null)
                {
                    combinedOptions.SourceOptions = new SourceOptions();
                }

                rest = new ArraySegment<string>(commandLineArguments);
                
                GetOptParser.Parse(ref rest, combinedOptions);

                if (combinedOptions.Options.Help)
                {
                    HelpText.Print(Console.Out, combinedOptions);
                    return ExitCode.HelpDisplayed;
                }

                if (combinedOptions.SourceOptions != null && source != null)
                {
                    var sourceOptions = combinedOptions.SourceOptions;
                    if (sourceOptions.Edit)
                    {
                        await Tools.Cmd.Run("start", source.CsprojFile);
                        return ExitCode.Success;
                    }

                    if (sourceOptions.Debug)
                    {
                        System.Diagnostics.Debugger.Launch();
                    }

                    if (sourceOptions.Watch)
                    {
                        await Watch();
                    }
                }

                var amgBuildAssembly = Assembly.GetExecutingAssembly();
                Logger.Debug("{name} {version}", amgBuildAssembly.GetName().Name, amgBuildAssembly.NugetVersion());

                for (var args = new ArraySegment<string>(combinedOptions.Options.TargetAndArguments); args.Any();)
                {
                    var (method, parameters) = ParseCommands(ref args, combinedOptions.OnceProxy);

                    object? result = null;

                    try
                    {
                        result = await RunCommand(onceProxy, method, parameters);
                        if (result != null)
                        {
                            result.Dump().Write(Console.Out);
                        }
                    }
                    catch (InvocationFailedException)
                    {
                        // can be ignored here, because failures are recorded in invocations
                    }
                }

                var invocations = new[] { GetStartupInvocation() }
                    .Concat(((IInvocationSource)onceProxy).Invocations);

                if (combinedOptions.Options.Summary)
                {
                    Summary.PrintTimeline(invocations).Write(Console.Out);
                }

                if (combinedOptions.Options.AsciiArt)
                {
                    Summary.PrintAsciiArt(invocations);
                }

                return invocations.Failed()
                    ? ExitCode.TargetFailed
                    : ExitCode.Success;
            }
            catch (OnceException ex)
            {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine();
                Console.Error.WriteLine("See https://github.com/sidiandi/Amg.Build/ for instructions.");
                return ExitCode.TargetFailed;
            }
            catch (CommandLineArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Run with --help to get help.");
                return ExitCode.CommandLineError;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($@"An unknown error has occured.

This is a bug in Amg.Build.

Submit here: https://github.com/sidiandi/Amg.Build/issues

Details:
{ex}
");
                return ExitCode.UnknownError;
            }
        }

        string StartupFile => BuildScriptDll + ".startup";

        void RecordStartupTime()
        {
            if (!StartupFile.IsFile())
            {
                Json.Write(StartupFile, DateTime.UtcNow);
            }
        }

        DateTime GetStartupTime()
        {
            if (StartupFile.IsFile())
            {
                try
                {
                    return Json.Read<DateTime>(StartupFile).Result;
                }
                catch
                {
                    // ignore read errors
                }
                finally
                {
                    StartupFile.EnsureFileNotExists();
                }
            }
            return Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }

        InvocationInfo GetStartupInvocation()
        {
            var begin = GetStartupTime();
            var end = DateTime.UtcNow;
            var startupDuration = end - begin;
            Logger.Debug("Startup duration: {startupDuration}", startupDuration);
            var startupInvocation = new InvocationInfo("startup", begin, end);
            return startupInvocation;
        }

        string BuildScriptDll => Assembly.GetEntryAssembly().Location;

        internal static (MethodInfo method, object?[] parameters, ArraySegment<string> rest)
            ParseCommands(string[] arguments, object targets)
        {
            var rest = new ArraySegment<string>(arguments);
            var (method, parameters) = ParseCommands(ref rest, targets);
            return (method, parameters, rest);
        }

        /// <summary>
        /// Extract the method to be called on targets and its arguments from command line arguments
        /// </summary>
        /// <param name="arguments">all command line arguments (required for error display)</param>
        /// <param name="options">parsed options</param>
        /// <returns></returns>
        internal static (MethodInfo method, object?[] parameters) ParseCommands(
        ref ArraySegment<string> arguments,
        object targets)
        {
            var r = arguments;

            if (r.Count == 0)
            {
                try
                {
                    var defaultTarget = CommandObject.GetDefaultTarget(targets);
                    if (defaultTarget == null)
                    {
                        throw new CommandLineArgumentException(r, "no default command");
                    }
                    else
                    {
                        arguments = r;
                        return (defaultTarget, new string[] { });
                    }
                }
                catch (Exception e)
                {
                    throw new CommandLineArgumentException(r, e);
                }
            }

            var candidates = CommandObject.Commands(targets.GetType());

            var commandName = GetOptParser.GetFirst(ref r);
            try
            {
                var command = candidates.FindByName(
                    _ => GetOptParser.GetLongOptionNameForMember(_.Name),
                    commandName,
                    "commands"
                    );
                var parameterValues = ParseParameters(ref r, command);
                arguments = r;
                return (command, parameterValues);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new CommandLineArgumentException(arguments, e);
            }
        }

        static object ReadArray(ref ArraySegment<string> args, Type arrayType)
        {
            var r = args;
            var elementType = arrayType.GetElementType();

            var items = new List<object?>();
            while (r.Count > 0)
            {
                var i = ReadScalar(ref r, elementType);
                items.Add(i);
            }
            try
            {
                var a = ToArray(items, arrayType);
                args = r;
                return a;
            }
            catch (Exception e)
            {
                throw new CommandLineArgumentException(args, $"Cannot read {arrayType}", e);
            }
        }

        static object ToArray(IList<object?> items, Type arrayType)
        {
            var a = Array.CreateInstance(arrayType.GetElementType(), items.Count);
            for (int i = 0; i < items.Count; ++i)
            {
                a.SetValue(items[i], i);
            }
            return a;
        }

        static object ReadScalar(ref ArraySegment<string> args, Type valueType)
        {
            if (valueType.IsArray)
            {
                return ReadArray(ref args, valueType);
            }

            var r = args;
            var p = GetOptParser.GetFirst(ref r);
            try
            {
                var parameterValue = GetOptOption.Parse(valueType, p);
                args = r;
                return parameterValue;
            }
            catch (ArgumentException e)
            {
                throw new CommandLineArgumentException(args, e);
            }
        }

        static object? ReadParameter(ref ArraySegment<string> args, ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                if (args.Count > 0)
                {
                    return ReadScalar(ref args, parameter.ParameterType);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (args.Count == 0)
                {
                    throw new CommandLineArgumentException(args, $"Parameter {parameter.Name} is missing.");
                }
                return ReadScalar(ref args, parameter.ParameterType);
            }
        }

        internal static object?[] ParseParameters(ref ArraySegment<string> args, MethodInfo command)
        {
            var rest = args;
            var p = command.GetParameters().Select(_ => ReadParameter(ref rest, _)).ToArray();
            args = rest;
            return p;
        }

        private static async Task<object?> RunCommand(object instance, MethodInfo method, object?[] parameters)
        {
            Logger.Information("Run {target}({arguments})", method.Name, parameters.Join(", "));
            return await AsTask(method.Invoke(instance, parameters));
        }

        /// <summary>
        /// Waits if returnValue is Task
        /// </summary>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        static Task<object?> AsTask(object returnValue)
        {
            if (returnValue is Task task)
            {
                var type = returnValue.GetType();
                var resultProperty = type.GetProperty("Result");
                return task.ContinueWith((_) =>
                {
                    try
                    {
                        var result = resultProperty == null
                            ? null
                            : resultProperty.GetValue(task);
                        return result;
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        throw targetInvocationException.InnerException.InnerException;
                    }
                });
            }
            else
            {
                return Task.FromResult<object?>(returnValue);
            }
        }

        private static LogEventLevel SerilogLogEventLevel(Verbosity verbosity)
        {
            switch (verbosity)
            {
                case Verbosity.Detailed:
                    return LogEventLevel.Debug;
                case Verbosity.Normal:
                    return LogEventLevel.Information;
                case Verbosity.Minimal:
                    return LogEventLevel.Error;
                case Verbosity.Quiet:
                    return LogEventLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, "no enum value");
            }
        }
    }
}
