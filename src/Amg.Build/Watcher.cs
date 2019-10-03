using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Amg.Build
{
    class Watcher
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Assembly entryAssembly;
        private readonly string[] commandLineArguments;
        private readonly string directoryToMonitor;

        public Watcher(Assembly entryAssembly, string[] commandLineArguments, string directoryToMonitor)
        {
            this.entryAssembly = entryAssembly;
            this.commandLineArguments = commandLineArguments;
            this.directoryToMonitor = directoryToMonitor;
        }

        public bool IsWatching()
        {
            var value = System.Environment.GetEnvironmentVariable(EnvironentVariable);
            return !String.IsNullOrEmpty(value);
        }

        string EnvironentVariable => "AmgBuildWatching";

        public async Task Watch()
        {
            await Task.CompletedTask;

            System.Environment.SetEnvironmentVariable(EnvironentVariable, this.entryAssembly.Location);

            using (var fsw = new FileSystemWatcher(directoryToMonitor))
            {
                fsw.BeginInit();
                fsw.Filter = "*";
                fsw.IncludeSubdirectories = true;
                fsw.Changed += Fsw_Changed;
                fsw.Deleted += Fsw_Changed;
                fsw.Created += Fsw_Changed;
                fsw.EnableRaisingEvents = true;
                fsw.EndInit();

                Console.ReadLine();

                // wait
                fsw.Changed -= Fsw_Changed;
                fsw.Deleted -= Fsw_Changed;
                fsw.Created -= Fsw_Changed;
            }
        }

        readonly object mutex = new object();

        HashSet<string> changedFiles = new HashSet<string>();

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            lock (mutex)
            {
                changedFiles.Add(e.FullPath);
                Monitor.PulseAll(mutex);
            }
            Run();
        }

        IEnumerable<string> GetChangedFiles()
        {
            lock (mutex)
            {
                Monitor.Wait(mutex);
                var c = changedFiles;
                changedFiles = new HashSet<string>();
                return c;
            }
        }

        Task? runner = null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3241:Methods should not return values that are never used", Justification = "<Pending>")]
        void Run()
        {
            lock (mutex)
            {
                if (runner == null)
                {
                    runner = Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            Logger.Information("Watching file system at {root}. Press Ctrl+C to stop.", directoryToMonitor);
                            var files = GetChangedFiles();
                            if (files.Any())
                            {
                                Logger.Information("Changed files:\r\n{files}", files.Join());

                                var tool = new Tool("dotnet.exe")
                                    .WithEnvironment(EnvironentVariable, "1")
                                    .WithArguments(entryAssembly.Location);

                                tool.Run(this.commandLineArguments).Wait();
                            }
                        }
                    }, TaskCreationOptions.LongRunning);
                }
            }
        }
    }
}
