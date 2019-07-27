using System;
using System.IO;
using System.Text;

namespace Amg.Build
{
    /// <summary>
    /// Calls an <![CDATA[ Action<string> ]]> for every written line.
    /// </summary>
    public class ActionStream : TextWriter
    {
        private Action<string> output;
        StringBuilder startedLine = null;

        /// <summary />
        public ActionStream(Action<string> output)
        {
            this.output = output;
        }

        /// <summary />
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

        /// <summary />
        public override void WriteLine(string line)
        {
            if (startedLine != null)
            {
                line = startedLine.ToString() + line;
                startedLine = null;
            }
            output(line);
        }

        /// <summary />
        public override void Flush()
        {
            WriteLine();
        }

        /// <summary />
        public override Encoding Encoding => Encoding.UTF8;
    }
}