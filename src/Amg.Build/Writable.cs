using System;
using System.IO;

namespace Amg.Build
{
    class Writable : IWritable
    {
        private readonly Action<TextWriter> writer;

        public Writable(Action<TextWriter> writer)
        {
            this.writer = writer;
        }

        public override string ToString()
        {
            using (var w = new StringWriter())
            {
                Write(w);
                return w.ToString();
            }
        }

        public void Write(TextWriter textWriter)
        {
            writer(textWriter);
        }
    }

}
