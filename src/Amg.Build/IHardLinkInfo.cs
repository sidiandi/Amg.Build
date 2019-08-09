using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
{
    /// <summary>
    /// Information about a file system hard link
    /// </summary>
    public interface IHardLinkInfo
    {
        /// <summary>
        /// How many files are linked to this index
        /// </summary>
        int FileLinkCount { get; }
        /// <summary>
        /// The file index
        /// </summary>
        long FileIndex { get; }
        /// <summary>
        /// linked files
        /// </summary>
        IEnumerable<string> HardLinks { get; }
    }
}
