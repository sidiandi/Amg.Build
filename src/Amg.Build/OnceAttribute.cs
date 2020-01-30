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

    /// <summary>
    /// Mark a method with this attribute to cache the result in the file system.
    /// </summary>
    /// Can only be applied to virtual methods.
    public class CachedAttribute : Attribute
    {
    }
}