using System;
using System.Linq;
using System.Reflection;

namespace Amg.Build
{
    /// <summary>
    /// Mark method to be executed only once during the lifetime of its class instance.
    /// </summary>
    /// Can only be applied to virtual methods.
    public class OnceAttribute : Attribute
    {
    }
}