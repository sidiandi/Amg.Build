using System.IO;

namespace Amg.Extensions
{
    internal class TeeTextReader : TextReader
    {
        readonly private TextReader input;
        readonly private TextWriter output;

        public TeeTextReader(TextReader input, TextWriter output)
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

        public override string? ReadLine()
        {
            string? line = input.ReadLine();
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