using System.IO;

namespace Amg.Build
{
    internal class TeeStream : TextReader
    {
        private TextReader input;
        private TextWriter output;

        public TeeStream(TextReader input, TextWriter output)
        {
            this.input = input;
            this.output = output;
        }

        public override int Read()
        {
            var c = input.Read();
            if (c >= 0)
            {
                output.Write((char)c);
            }
            return c;
        }

        public override string ReadLine()
        {
            var line = input.ReadLine();
            if (line != null)
            {
                output.WriteLine(line);
            }
            return line;
        }

        public override int Peek()
        {
            return input.Peek();
        }
    }
}