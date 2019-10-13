using System;
using System.IO;
using System.Text;

namespace Amg.Extensions
{
    /// <summary>
    /// Calls an <![CDATA[ Action<string> ]]> for every written line.
    /// </summary>
    class ActionTextWriter : TextWriter
    {
        private readonly Action<string> output;
        StringBuilder? startedLine = null;

        /// <summary />
        public ActionTextWriter(Action<string> output)
        {
            this.output = output;
        }

        /// <summary />
        public override void Write(char value)
        {
            if (value == '\r')
            {
                // do nothing
            }
            else if (value == '\n')
            {
                WriteLine(string.Empty);
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
        public override void WriteLine(string value)
        {
            if (startedLine != null)
            {
                value = startedLine.ToString() + value;
                startedLine = null;
            }
            output(value);
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