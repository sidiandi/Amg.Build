using System;

namespace Amg.CommandLine
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class OperandsAttribute : System.Attribute
    {
        public OperandsAttribute()
        {
        }
    }
}
