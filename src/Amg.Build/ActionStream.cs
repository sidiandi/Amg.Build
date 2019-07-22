using System;
using System.IO;
using System.Text;

namespace Amg.Build
{
    /// <summary>
    /// Calls an Action<string> for every written line
    /// </summary>
    public class ActionStream : TextWriter
    {
        private Action<string> output;
        StringBuilder startedLine = null;

        public ActionStream(Action<string> output)
        {
            this.output = output;
        }

        public override void Write(char value)
        {
            if (value == '\r')
            {
            }
            else if (value == '\n')
            {
                WriteLine(String.Empty);
            }
            else
            {
                if (startedLine == null)
                {
                    startedLine = new StringBuilder();
                }
                startedLine.Append(value);
            }
        }

        public override void WriteLine(string line)
        {
            if (startedLine != null)
            {
                line = startedLine.ToString() + line;
                startedLine = null;
            }
            output(line);
        }

        public override void Flush()
        {
            WriteLine();
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}