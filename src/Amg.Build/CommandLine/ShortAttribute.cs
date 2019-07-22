using System;

namespace Amg.CommandLine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ShortAttribute : System.Attribute
    {
        public ShortAttribute(char name)
        {
            Name = name;
        }

        public char Name { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class OperandsAttribute : System.Attribute
    {
        public OperandsAttribute()
        {
        }
    }
}
