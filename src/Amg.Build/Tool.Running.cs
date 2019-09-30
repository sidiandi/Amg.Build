using System.Diagnostics;

namespace Amg.Build
{
    public partial class Tool
    {
        class Running : IRunning
        {
            private Process process;

            public Running(Process p)
            {
                this.process = p;
                lock (RunningProcesses)
                {
                    RunningProcesses.Add(this);
                }
            }

            public Process Process => process;

            public override string ToString() => Process.Id.ToString();

            public void WaitForExit()
            {
                Process.WaitForExit();
                lock (RunningProcesses)
                {
                    RunningProcesses.Remove(this);
                }
            }
        }
    }
}