using System;

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