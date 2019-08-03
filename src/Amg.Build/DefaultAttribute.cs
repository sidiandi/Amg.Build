using System;

namespace Amg.Build
{
    /// <summary>
    /// Marks the default build target.
    /// </summary>
    /// This target is called when build.cmd is started without parameters.
    public class DefaultAttribute : Attribute
    {
    }
}