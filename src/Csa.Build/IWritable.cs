using System.IO;

namespace Csa.Build
{
    /// <summary>
    /// Something that can be written to a TextWriter
    /// </summary>
    public interface IWritable
    {
        void Write(TextWriter textWriter);
    }
}